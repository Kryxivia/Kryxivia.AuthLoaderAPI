using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Utilities
{
    public static class HttpContextExtensions
    {
        public static string PublicKey(this HttpContext httpContext)
        {
            return httpContext.Items["PublicKey"] as string;
        }

        public static string Signature(this HttpContext httpContext)
        {
            return httpContext.Items["Signature"] as string;
        }
    }
}
