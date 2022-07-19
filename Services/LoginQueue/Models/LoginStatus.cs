using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Services.LoginQueue.Models
{
    public class LoginStatus
    {
        public string State { get; set; }
        public int Position { get; set; }
        public int Total { get; set; }

        public bool Waiting { get; set; }
    }
}
