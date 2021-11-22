using Kryxivia.AuthLoaderAPI.Services.LoginQueue.Models;
using Kryxivia.AuthLoaderAPI.Settings;
using Kryxivia.AuthLoaderAPI.Utilities;
using Kryxivia.Domain.MongoDB.Models.Game;
using Kryxivia.Domain.MongoDB.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Services.LoginQueue
{
    public class LoginQueueService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly LoginQueueSettings _loginQueueSettings;

        private readonly ConcurrentDictionary<string, bool> _inQueue = new ConcurrentDictionary<string, bool>();
        private ChainQueue<string, LoginRequest> _chainQueue = new ChainQueue<string, LoginRequest>();

        public LoginQueueService(IServiceProvider serviceProvider, IOptions<LoginQueueSettings> loginQueueSettings)
        {
            _serviceProvider = serviceProvider;
            _loginQueueSettings = loginQueueSettings?.Value;

            Start();
        }

        #region Startup

        public void Start()
        {
            try
            {
                // Queue GC
                Task.Run(async () =>
                {
                    for (; ; )
                    {
                        try
                        {
                            _chainQueue.RemoveTimeOut(10000, (removedKey) =>
                            {
                                _inQueue.TryRemove(removedKey, out _);
                                Log.Information($"[QueueGarbageCollection] Key '{removedKey}' removed from queue");
                            });
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[QueueGarbageCollection] An error has occurred: {ex?.ToString()}");
                        }

                        await Task.Delay(1000);
                    }
                });

                // Login
                Task.Run(async () =>
                {
                    for (; ; )
                    {
                        for (var i = 0; i < _loginQueueSettings.Prefetch; i++)
                        {
                            try
                            {
                                if (!_chainQueue.IsEmpty
                                    && _chainQueue.Dequeue(out var loginRequest)
                                    && _inQueue.TryRemove(loginRequest.Id, out _))
                                {
                                    using var scope = _serviceProvider.CreateScope();
                                    var accountRepository = scope.ServiceProvider.GetService<AccountRepository>();

                                    var account = await accountRepository.GetByPublicKey(loginRequest.PublicKey);
                                    if (account == null)
                                    {
                                        account = new Account()
                                        {
                                            PublicKey = loginRequest.PublicKey,
                                            Signature = loginRequest.Signature,
                                            IsLogged = true
                                        };

                                        await accountRepository.Create(account);
                                    }
                                    else
                                    {
                                        account.IsLogged = true;
                                        await accountRepository.Update(account.IdAsString, account);
                                    }

                                    Log.Information($"[Login] Account '{account.Id}' logged");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[Login] An error has occurred: {ex?.ToString()}");
                            }
                        }

                        await Task.Delay(_loginQueueSettings.TTL);
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error($"An error has occurred: {ex?.ToString()}");
            }
        }

        #endregion

        public string PushLogin(LoginRequest loginRequest)
        {
            try
            {
                _chainQueue.Enqueue(loginRequest.Id, loginRequest);
                _inQueue.TryAdd(loginRequest.Id, true);

                return loginRequest.Id;
            }
            catch (Exception error)
            {
                Log.Error(error.ToString());
                return string.Empty;
            }
        }

        public bool IsInQueue(string id)
        {
            return _inQueue.ContainsKey(id);
        }

        public LoginStatus GetLoginStatus(string id)
        {
            try
            {
                var position = _chainQueue.GetPosition(id);

                return new LoginStatus()
                {
                    State = position == -1 ? "Logged" : "Waiting",
                    Position = position,
                    Total = _chainQueue.Count
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }
    }
}
