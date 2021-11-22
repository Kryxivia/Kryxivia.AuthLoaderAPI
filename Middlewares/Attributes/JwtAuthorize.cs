using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Middlewares.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class JwtAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                var httpContext = (context.HttpContext as HttpContext);
                if (httpContext == null)
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                if (!httpContext.Items.ContainsKey("PublicKey") || !httpContext.Items.ContainsKey("Signature"))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error has occurred while validating the JWT");
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
