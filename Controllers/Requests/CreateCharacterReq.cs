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

        // Old
        public int HairColor { get; set; }
        public int SkinColor { get; set; }
        public int EyesColor { get; set; }


        // New
        public int EyesColorType { get; set; }
        public int HairColorType { get; set; }
        public int HairStyle { get; set; }
        public int BodyColorType { get; set; }

        public int FactionId { get; set; }

        public List<KeyValuePair<string, float>> Blendshapes { get; set; } = new List<KeyValuePair<string, float>>();

        public bool WelcomeStuff { get; set; } = true;
    }
}
