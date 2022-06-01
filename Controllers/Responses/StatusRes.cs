using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Responses
{
    public class StatusRes
    {
        public bool IsOnline { get; set; }
        public string Activity { get; set; }
    }
}
