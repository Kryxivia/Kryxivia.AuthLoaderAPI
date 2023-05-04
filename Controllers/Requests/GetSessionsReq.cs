using System.Collections.Generic;

namespace Kryxivia.AuthLoaderAPI.Controllers.Requests
{
    public class GetSessionsReq
    {
        public List<string> SessionToFind { get; set; } = new List<string>();
    }
}
