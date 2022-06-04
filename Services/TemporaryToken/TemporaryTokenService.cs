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
using Kryxivia.AuthLoaderAPI.Services.TemporaryToken.Models;

namespace Kryxivia.AuthLoaderAPI.Services.TemporaryToken
{
    public class TemporaryTokenService
    {
        private readonly IServiceProvider _serviceProvider;

        private ConcurrentDictionary<string, TemporaryAuthToken> _temporaryAuthTokens = new ConcurrentDictionary<string, TemporaryAuthToken>();
        private List<string> _inQueueHash = new List<string>();
        private object mutex = new object();

        private Random random = new Random();

        public TemporaryTokenService(IServiceProvider serviceProvider, IOptions<LoginQueueSettings> loginQueueSettings)
        {
            _serviceProvider = serviceProvider;

            Start();
        }

        #region Startup

        public void Start()
        {
            try
            {
                Task.Run(async () =>
                {
                    for (; ; )
                    {
                        try
                        {
                            DateTime now = DateTime.UtcNow;
                            List<string> _hashToRemove = new List<string>();
                            _inQueueHash.ForEach(x =>
                            {
                                try
                                {
                                    TemporaryAuthToken token;
                                    _temporaryAuthTokens.TryGetValue(x, out token);
                                    TimeSpan timeSpent = now - token.Date;
                                    // after 5 mins past we clear unnecessary token
                                    if (timeSpent.Minutes >= 1)
                                    {
                                        _temporaryAuthTokens.TryRemove(x, out _);
                                        _hashToRemove.Add(x);
                                    }

                                }
                                catch (Exception error)
                                {
                                    Log.Error(error.ToString());
                                }
                            });
                            if (_hashToRemove.Count > 0)
                                Log.Information(string.Format("{0} temporary tokens cleared", _hashToRemove.Count));
                            lock (mutex)
                                _inQueueHash = _inQueueHash.Where(x => !_hashToRemove.Contains(x)).ToList();
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[QueueGarbageCollection] An error has occurred: {ex?.ToString()}");
                        }

                        await Task.Delay(10000);
                    }
                });

      
            } 
            catch (Exception ex)
            {
                Log.Error($"An error has occurred: {ex?.ToString()}");
            }
        }

        #endregion

       public bool ValidateTemporaryAuthToken(string hash, string jwt)
       {
            TemporaryAuthToken token = null;
            _temporaryAuthTokens.TryGetValue(hash, out token);
            if (token != null)
            {
                token.jwtAttached = jwt;
                token.validated = true;
                token.Date = DateTime.UtcNow;
            }
            return false;
        }

       public TemporaryAuthToken GenerateTemporaryAuthToken()
        {
            var hash = string.Empty;
            while (hash == string.Empty && !_temporaryAuthTokens.ContainsKey(hash))
                hash = RandomAlphaNumeric(16);

            var token = new TemporaryAuthToken { TokenHash = hash, Date = DateTime.UtcNow, validated = false, jwtAttached = null };
            _temporaryAuthTokens.TryAdd(token.TokenHash, token);
             
            Log.Information(string.Format("New auth temporary token for uplauncher initiated: {0}", hash));
            // add in queue to remove later on
            lock (mutex)
                _inQueueHash.Add(hash);
            return token;
        }

        private string RandomAlphaNumeric(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public TemporaryAuthToken VerifyTemporaryAuthToken(string hash)
        {
            TemporaryAuthToken token;
            _temporaryAuthTokens.TryGetValue(hash, out token);
            if (token != null && token.validated && token.jwtAttached != null)
            {
                // remove from temporary token wait list as the client had the JWT response
                _temporaryAuthTokens.TryRemove(hash, out _);
                lock (mutex)
                    _inQueueHash = _inQueueHash.Where(x => (x != hash)).ToList();
            }

            return token;
        }
    }
}
