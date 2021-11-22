using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Requests
{
    public class CreateCharacterReq
    {
        public string Name { get; set; }
        public int Gender { get; set; }

        // TODO: Options....
    }
}
