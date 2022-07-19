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
    public class PlayerStateService
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly PlayerStateSettings _playerStateSettings;

        private readonly AccountRepository _accountRepository;

        private List<PlayerStateObject> _players = new List<PlayerStateObject>();

        private object _mutex = new object();

        public PlayerStateService(IServiceProvider serviceProvider, IOptions<PlayerStateSettings> playerStateSettings)
        {
            _playerStateSettings = playerStateSettings?.Value;
            _serviceProvider = serviceProvider;

            using var scope = _serviceProvider.CreateScope();
            _accountRepository = scope.ServiceProvider.GetService<AccountRepository>();
            Start();
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                var alives = await FetchAlivesPlayers();
                Log.Information(string.Format("{0} loaded players currently online", alives.Count));

                SetAlivePlayers(alives);
                AliveAnalyzer();
            });
        }

        public async Task<List<PlayerStateObject>> FetchAlivesPlayers()
        {
            List<Account> accounts = await _accountRepository.GetAccountsByLoggedState(true);
            var alives = accounts.Select(x => new PlayerStateObject { Alive = true, LastPing = DateTime.UtcNow, PublicKey = x.PublicKey });
            return alives.ToList();
        }

        public void UpdateAlivePlayer(string publicKey)
        {
            lock (_mutex)
            {
                var player = _players.Find(x => x.PublicKey == publicKey);
                if (player == null) {
                    _players.Add(new PlayerStateObject { Alive = true, LastPing = DateTime.UtcNow, PublicKey = publicKey });
                }
                else
                {
                    player.LastPing = DateTime.UtcNow;
                }
            }
        }

        public void DisconnectPlayer(string publicKey)
        {
            if (!isAlreadyConnected(publicKey))
                return;
            lock (_mutex)
            {
                int index =  _players.FindIndex(x => x.PublicKey == publicKey);
                _players.RemoveAt(index);
            }
        }

        public int GetConnectedCount()
        {
            int connectedPlayers = 0;
            lock (_mutex)
            {
                connectedPlayers = _players.Count;
            }
            return connectedPlayers;
        }

        public bool isAlreadyConnected(string publicKey)
        {
            bool isConnected = false;
            lock (_mutex)
            {
                isConnected = _players.Select(x => x.PublicKey).Contains(publicKey);
            }
            return isConnected;
        }

        public int GetMaxPlayers()
        {
            return _playerStateSettings.MaxPlayersOnline;
        }

        public int CountAlivePlayers()
        {
            int count = 0;
            lock (_mutex)
            {
                count = _players.Count;
            }
            return count;
        }

        private List<PlayerStateObject> GetAlivePlayers()
        {
            List<PlayerStateObject> alives = new List<PlayerStateObject>();
            lock (_mutex)
            {
                _players.ForEach(x => alives.Add(x));
            }
            return alives;
        }

        private void SetAlivePlayers(List<PlayerStateObject> players)
        {
            lock (_mutex)
            {
                _players = players;
            }
        }

        private void MergeAlivePlayers(List<PlayerStateObject> newPlayers)
        {
            lock (_mutex)
            {
                var keys = _players.Select(s => s.PublicKey);
                var toAdd = newPlayers.FindAll(x => !keys.Contains(x.PublicKey));
                _players.AddRange(toAdd);
            }
        }

        public async void AliveAnalyzer()
        {
            for (;;)
            {
                try
                {
                    // add new alive players in case something updated the database
                    MergeAlivePlayers(await FetchAlivesPlayers());

                    var currentDate = DateTime.UtcNow;
                    List<PlayerStateObject> stillAlives = new List<PlayerStateObject>();
                    var livePlayers = GetAlivePlayers();
                    Log.Information(string.Format("{0} players connected in-game", livePlayers.Count));
                    List<string> toDisconnects = new List<string>();

                    livePlayers.ForEach(x =>
                    {
                        bool alive = (currentDate - x.LastPing).Value.TotalSeconds <= _playerStateSettings.SecondsTimeout;
                        if (alive)
                        {
                            stillAlives.Add(x);
                        }
                        else
                        {
                            toDisconnects.Add(x.PublicKey);
                        }
                    });

                    if (toDisconnects.Count > 0)
                    {
                        var result = await _accountRepository.UpdateAccountsLoggedState(toDisconnects, false);
                        SetAlivePlayers(stillAlives);
                        Log.Information(string.Format("{0} players disconnected in the last {1} seconds.",
                            toDisconnects.Count, _playerStateSettings.SecondsTimeout));
                    }
                }
                catch (Exception error)
                {
                    Log.Error(error.ToString());
                }
                await Task.Delay(_playerStateSettings.TTL);
            }
        }
    }
}
