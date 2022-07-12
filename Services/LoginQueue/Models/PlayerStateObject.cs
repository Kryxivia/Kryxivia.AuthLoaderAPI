using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Services.LoginQueue.Models
{
    public class PlayerStateObject
    {
        public bool Alive;

        public DateTime? LastPing;

        public string PublicKey;
    }
}
