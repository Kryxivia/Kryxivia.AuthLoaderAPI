using Kryxivia.Domain.MongoDB.Models.Game;
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
        public AccountRights AccountRights { get; set; }
        public string BanPeriod { get; set; }
    }
}
