using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Requests
{
    public class LoginReq
    {
        public string PublicKey { get; set; }
        public string Signature { get; set; }

        public string TemporaryAuthToken { get; set; }
    }
}
