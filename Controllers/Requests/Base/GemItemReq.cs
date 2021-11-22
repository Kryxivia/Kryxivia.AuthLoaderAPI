using Kryxivia.Domain.MongoDB.Models.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Requests.Base
{
    public class GemItemReq : ItemReq
    {
        public List<Spell> Spells { get; set; }
    }
}
