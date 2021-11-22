using Kryxivia.Domain.MongoDB.Enums;
using Kryxivia.Domain.MongoDB.Models.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Requests.Base
{
    public class ItemReq
    {
        public int TemplateId { get; set; }
        public ItemTypeEnum Type { get; set; }
        public List<Statistic> Stats { get; set; }
    }
}
