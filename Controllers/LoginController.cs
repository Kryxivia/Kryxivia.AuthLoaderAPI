using System;
using System.Threading.Tasks;
using Kryxivia.Domain.MongoDB.Models.Game;
using Kryxivia.Domain.MongoDB.Repositories;
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
using Kryxivia.AuthLoaderAPI.Controllers.Responses;
using Kryxivia.AuthLoaderAPI.Services.TemporaryToken;
using Kryxivia.AuthLoaderAPI.Services.TemporaryToken.Models;
using Newtonsoft.Json;
using Kryxivia.AuthLoaderAPI.Middlewares.Attributes;
using Kryxivia.AuthLoaderAPI.Utilities;

namespace Kryxivia.AuthLoaderAPI.Controllers
{
    [ApiController]
    [AllowAnonymous]
    [Route("api/v1/login")]
    public class LoginController : _ControllerBase
    {
        private readonly JwtSettings _jwtSettings;

        private readonly AlphaAccessRepository _alphaAccessRepository;
        private readonly AccountRepository _accountRepository;

        private readonly LoginQueueService _loginQueueService;

        private readonly TemporaryTokenService _temporaryTokenService;

        private readonly EthereumMessageSigner _ethereumMessageSigner;

        private readonly PlayerStateService _playerStateService;

        private readonly AlphaRewardRepository _alphaRewardRepository;

        public LoginController(IOptions<JwtSettings> jwtSettings,
            AlphaAccessRepository alphaAccessRepository, AccountRepository accountRepository, AlphaRewardRepository alphaRewardRepository,
                LoginQueueService loginQueueService, TemporaryTokenService temporaryTokenService,
                PlayerStateService playerStateService)
        {
            _jwtSettings = jwtSettings?.Value;

            _alphaRewardRepository = alphaRewardRepository;
            _alphaAccessRepository = alphaAccessRepository;
            _accountRepository = accountRepository;

            _loginQueueService = loginQueueService;
            _temporaryTokenService = temporaryTokenService;
            _playerStateService = playerStateService;

            _ethereumMessageSigner = new EthereumMessageSigner();
        }

        /// <summary>
        /// Authenticate a user and add it to the login queue
        /// </summary>
        [HttpPost]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginRes))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorRes))]
        public async Task<IActionResult> Login([FromBody] LoginReq req)
        {
            var addressRec = _ethereumMessageSigner.EncodeUTF8AndEcRecover(_jwtSettings.SignatureMessage, req.Signature);
            if (addressRec != req.PublicKey)
                return Error(ErrorRes.Get("Invalid signature"));

            if (_loginQueueService.IsInQueue(addressRec))
                return Error(ErrorRes.Get("Already in queue"));
            /*
                var alphaAccessEntry = await _alphaAccessRepository.GetByPublicKey(req.PublicKey);
                if (alphaAccessEntry == null)
                    return Error(ErrorRes.Get("No Alpha access found"));
            */

            /*
            var consumedAlphaRewards = await _alphaRewardRepository.GetByConsumer(req.PublicKey);
            if (consumedAlphaRewards.Count > 0)
            {
                var result = consumedAlphaRewards.FindAll(x => x.Spell.TemplateId == -1);
                if (result.Count == 0)
                    return Error(ErrorRes.Get("You need the NFT 'Kryxivia Alpha Book Access' to access the public alpha! Go claim it on https://app.kryxivia.io/public-alpha."));
            }
            */

            // Creating an account if not existing...
            var account = await _accountRepository.GetByPublicKey(req.PublicKey);
            if (account == null)
            {
                account = new Account()
                {
                    PublicKey = req.PublicKey,
                    Signature = req.Signature,
                };

                if (string.IsNullOrEmpty(await _accountRepository.Create(account))) return Error(ErrorRes.Get("Error creating account"));
            }

            // Requesting login...
            var loginRequest = new LoginRequest()
            {
                Id = addressRec,
                PublicKey = addressRec,
                Signature = req.Signature
            };

            var ticket = _loginQueueService.PushLogin(loginRequest);
            if (string.IsNullOrWhiteSpace(ticket))
                return Error(ErrorRes.Get("Ticket is empty"));

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

            if (req.TemporaryAuthToken != null)
                _temporaryTokenService.ValidateTemporaryAuthToken(req.TemporaryAuthToken, jwtAsString);

            return Ok(new LoginRes() { Token = jwtAsString, Ticket = ticket });
        }

        /// <summary>
        /// 
        /// </summary>
        [HttpGet]
        [Route("disconnect")]
        [JwtAuthorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStatus))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public IActionResult PingDisconnected()
        {
            var senderPubKey = HttpContext.PublicKey();
            _playerStateService.DisconnectPlayer(senderPubKey);
            return Ok(new { result = true, date = DateTime.UtcNow });
        }

        /// <summary>
        /// 
        /// </summary>
        [HttpGet]
        [Route("ping")]
        [JwtAuthorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStatus))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public IActionResult PingAliveness()
        {
            var senderPubKey = HttpContext.PublicKey();
            _playerStateService.UpdateAlivePlayer(senderPubKey);
            return Ok(new { result = true, date = DateTime.UtcNow });
        }

        /// <summary>
        /// 
        /// </summary>
        [HttpGet]
        [Route("join-queue")]
        [JwtAuthorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStatus))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public async Task<IActionResult> JoinQueue()
        {
            var senderPubKey = HttpContext.PublicKey();

            // Requesting joining queue...
            var loginRequest = new LoginRequest()
            {
                Id = senderPubKey,
                PublicKey = senderPubKey,
                Signature = string.Empty
            };

            var ticket = _loginQueueService.PushLogin(loginRequest);
            if (string.IsNullOrWhiteSpace(ticket))
                return Error(ErrorRes.Get("Ticket is empty"));

            return Ok(new { ticket = ticket, date = DateTime.UtcNow });
        }


        /// <summary>
        /// 
        /// </summary>
        [HttpGet]
        [Route("online-players")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStatus))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public IActionResult OnlinePlayers()
        {
            return Ok(new { count = _playerStateService.CountAlivePlayers() });
        }

        /// <summary>
        /// Returns queue position for a given ticket
        /// </summary>
        [HttpGet]
        [Route("{ticket}")]
        [JwtAuthorize]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(LoginStatus))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public IActionResult GetTicketPosition(string ticket)
        {
            var senderPubKey = HttpContext.PublicKey();
            var loginStatus = _loginQueueService.GetLoginStatus(ticket);
            if (loginStatus != null)
            {
                if (_playerStateService.isAlreadyConnected(senderPubKey))
                {
                    return Error(new LoginStatus()
                    {
                        State = "Account already connected!",
                        Position = -1,
                        Total = -1,
                        Waiting = true
                    });
                }

                int connectedPlayers = _playerStateService.GetConnectedCount();
                if (connectedPlayers > _playerStateService.GetMaxPlayers())
                {
                    return Error(new LoginStatus()
                    {
                        State = "Too many players connected!",
                        Position = -1,
                        Total = -1,
                        Waiting = true
                    });
                }
                if (loginStatus.State == "Logged")
                {
                    _playerStateService.UpdateAlivePlayer(senderPubKey);
                }
                return Ok(loginStatus);
            }
            else return NotFound(ErrorRes.Get("No ticket found"));
        }

        /// <summary>
        /// Generate a random temporary token attached to an uplauncher session
        /// </summary>
        [HttpGet]
        [Route("token_auth")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemporaryAuthToken))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public IActionResult GetTemporaryToken()
        {
            TemporaryAuthToken tmpToken = _temporaryTokenService.GenerateTemporaryAuthToken();
            return Ok(tmpToken);
        }

        /// <summary>
        /// Generate a random temporary token attached to an uplauncher session
        /// </summary>
        [HttpPost]
        [Route("token_auth_check")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemporaryAuthToken))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public IActionResult CheckTemporaryToken(TemporaryAuthToken req)
        {
            TemporaryAuthToken tmpToken = _temporaryTokenService.VerifyTemporaryAuthToken(req.TokenHash);
            return Ok(tmpToken);
        }
    }
}
