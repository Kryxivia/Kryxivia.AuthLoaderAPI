using Kryxivia.Domain.MongoDB.Models.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Requests.Base
{
    public class EquipmentItemReq : ItemReq
    {
        public List<GemItemReq> AttachedGems { get; set; }
    }
}
