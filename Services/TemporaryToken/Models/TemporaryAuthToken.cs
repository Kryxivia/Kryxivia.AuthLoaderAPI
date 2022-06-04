using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Services.TemporaryToken.Models
{
    
    public class TemporaryAuthToken
    {
        public string TokenHash { get; set; }
        public DateTime Date { get; set; }

        public bool validated { get; set; }

        public string jwtAttached { get; set; }
    }
}
