using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Controllers.Responses
{
    public class ErrorRes
    {
        public string ErrorMessage { get; set; }

        public static ErrorRes Get(string message) => new ErrorRes() { ErrorMessage = message };
    }
}
