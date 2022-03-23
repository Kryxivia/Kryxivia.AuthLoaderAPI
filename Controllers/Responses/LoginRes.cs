using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Responses
{
    public class LoginRes
    {
        public string Token { get; set; }
        public string Ticket { get; set; }
    }
}
