using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kryxivia.Domain.MongoDB.Models.Game;
using Kryxivia.Domain.MongoDB.Repositories;
using Kryxivia.AuthLoaderAPI.Middlewares.Attributes;
using Kryxivia.AuthLoaderAPI.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Signer;
using Kryxivia.AuthLoaderAPI.Settings;
using Microsoft.Extensions.Options;
using Kryxivia.AuthLoaderAPI.Services.LoginQueue;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Kryxivia.Shared.Claims;
using Microsoft.AspNetCore.Authorization;
using Kryxivia.AuthLoaderAPI.Controllers.Requests;
using Kryxivia.AuthLoaderAPI.Services.LoginQueue.Models;
using System.Net.Mime;

namespace Kryxivia.AuthLoaderAPI.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/v1/login")]
    public class LoginController : _ControllerBase
    {
        private readonly JwtSettings _jwtSettings;

        private readonly LoginQueueService _loginQueueService;

        private readonly EthereumMessageSigner _ethereumMessageSigner;

        public LoginController(IOptions<JwtSettings> jwtSettings,
            LoginQueueService loginQueueService)
        {
            _jwtSettings = jwtSettings?.Value;

            _loginQueueService = loginQueueService;

            _ethereumMessageSigner = new EthereumMessageSigner();
        }

        /// <summary>
        /// Authenticate a user and add it to the login queue
        /// </summary>
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Login([FromBody] LoginReq req)
        {
            var addressRec = _ethereumMessageSigner.EncodeUTF8AndEcRecover(_jwtSettings.SignatureMessage, req.Signature);
            if (addressRec != req.PublicKey)
                return Unauthorized("Invalid signature");

            if (_loginQueueService.IsInQueue(addressRec))
                return Unauthorized("Already in queue");

            var loginRequest = new Services.LoginQueue.Models.LoginRequest()
            {
                Id = addressRec,
                PublicKey = addressRec,
                Signature = req.Signature
            };

            var ticket = _loginQueueService.PushLogin(loginRequest);
            if (string.IsNullOrWhiteSpace(ticket))
                return Error("Ticket is empty");

            // Generating Jwt
            var expDate = DateTime.Now.AddMinutes(_jwtSettings.ExpirationInMinutes);
            var issuer = _jwtSettings.Issuer;
            var audience = _jwtSettings.Audience;
            var signingCredentials = _jwtSettings.SigningCredentials;

            var jwtHandler = new JwtSecurityTokenHandler();
            var jwtDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(CustomClaimTypes.PublicKey, addressRec),
                    new Claim(CustomClaimTypes.Signature, req.Signature)
                }),
                Expires = expDate,
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = signingCredentials
            };

            var jwt = jwtHandler.CreateToken(jwtDescriptor);
            var jwtAsString = jwtHandler.WriteToken(jwt);

            return Ok(new { Token = jwtAsString, Ticket = ticket });
        }

        /// <summary>
        /// Returns queue position for a given ticket
        /// </summary>
        [HttpGet]
        [Route("{ticket}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStatus))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetTicketPosition(string ticket)
        {
            var loginStatus = _loginQueueService.GetLoginStatus(ticket);
            if (loginStatus != null) return Ok(loginStatus);
            else return NotFound("Unable to find this ticket");
        }
    }
}
