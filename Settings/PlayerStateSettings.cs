using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Settings
{
    public class PlayerStateSettings
    {
        public int TTL { get; set; }
        public int MaxPlayersOnline { get; set; }
        public int SecondsTimeout { get; set; }
    }
}