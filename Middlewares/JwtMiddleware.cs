using Kryxivia.AuthLoaderAPI.Settings;
using Kryxivia.Shared.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nethereum.Signer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kryxivia.AuthLoaderAPI.Middlewares
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly JwtSettings _jwtSettings;

        private readonly EthereumMessageSigner _ethereumMessageSigner;

        public JwtMiddleware(RequestDelegate next, IOptions<JwtSettings> jwtSettings)
        {
            _next = next;
            _jwtSettings = jwtSettings?.Value;

            _ethereumMessageSigner = new EthereumMessageSigner();
        }

        public async Task Invoke(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token != null) attachUserToContext(context, token);

            await _next(context);
        }

        private void attachUserToContext(HttpContext context, string authorizationValue)
        {
            try
            {
                var jwtHandler = new JwtSecurityTokenHandler();

                jwtHandler.ValidateToken(authorizationValue, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _jwtSettings.SymmetricSecurityKey,

                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,

                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,

                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwt = (JwtSecurityToken)validatedToken;

                var jwtPublicKey = jwt.Claims.FirstOrDefault(x => x.Type == CustomClaimTypes.PublicKey)?.Value ?? string.Empty;
                var jwtSignature = jwt.Claims.FirstOrDefault(x => x.Type == CustomClaimTypes.Signature)?.Value ?? string.Empty;

                var addressRec = _ethereumMessageSigner.EncodeUTF8AndEcRecover(_jwtSettings.SignatureMessage, jwtSignature);

                if (addressRec == jwtPublicKey)
                {
                    context.Items["PublicKey"] = addressRec;
                    context.Items["Signature"] = jwtSignature;
                }
            }
            catch
            {
                // Do nothing if jwt validation fails
                // User is not attached to context so request won't have access to secure routes
            }
        }
    }
}
