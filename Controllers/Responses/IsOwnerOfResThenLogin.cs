using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Responses
{
    public class IsOwnerOfResThenLogin
    {
        public bool Owner { get; set; }
        public bool Success { get; set; }
        public bool IsBanned { get; set; }
        public string BanPeriod { get; set; }
    }
}
