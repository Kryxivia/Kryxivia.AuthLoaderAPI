using Kryxivia.Domain.MongoDB.Models.Game;
using System.Collections.Generic;

namespace Kryxivia.AuthLoaderAPI.Controllers.Responses
{
    public class GetSessionsRes
    {
        public List<CharacterSession> CharactersSessions { get; set; } = new List<CharacterSession>();
    }
}
