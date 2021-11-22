using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Settings
{
    public class LoginQueueSettings
    {
        public int Prefetch { get; set; }
        public int TTL { get; set; }
    }
}
