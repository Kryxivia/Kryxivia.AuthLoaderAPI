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

        public int HairColor { get; set; }
        public int HairStyle { get; set; }
        public int SkinColor { get; set; }
        public int EyesColor { get; set; }

        public List<KeyValuePair<string, float>> Blendshapes { get; set; } = new List<KeyValuePair<string, float>>();

        public bool WelcomeStuff { get; set; } = true;
    }
}
