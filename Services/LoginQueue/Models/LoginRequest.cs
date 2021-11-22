using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Services.LoginQueue.Models
{
    public class LoginRequest
    {
        public string Id { get; set; }
        public string PublicKey { get; set; }
        public string Signature { get; set; }
    }
}
