using Microsoft.Extensions.DependencyInjection;
using Kryxivia.AuthLoaderAPI.Middlewares.Attributes;
using Kryxivia.Contracts;
using Kryxivia.Shared.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Kryxivia.AuthLoaderAPI.Controllers.Responses;
using Serilog;
using Kryxivia.AuthLoaderAPI.Utilities;
using Microsoft.Extensions.Options;

namespace Kryxivia.AuthLoaderAPI.Controllers
{
    [ApiController]
    [JwtAuthorize]
    [Route("api/v1/nft")]
    public class NftController : _ControllerBase
    {
        private readonly Web3Settings _web3Settings;

        private IServiceProvider _serviceProvider;

        private readonly _KryxiviaNftServiceBase _kryxiviaNftService;

        public NftController(IOptions<Web3Settings> web3Settings, IServiceProvider serviceProvider)
        {
            _web3Settings = web3Settings?.Value;

            _serviceProvider = serviceProvider;

            if (_web3Settings.IsTargetTestnet)
            {
                _kryxiviaNftService = _serviceProvider.GetService<TestnetKryxiviaNftService>();
            }
            else if (_web3Settings.IsTargetMainnet)
            {
                _kryxiviaNftService = _serviceProvider.GetService<MainnetKryxiviaNftService>();
            }
        }

        /// <summary>
        /// Retrieves on-chain the NFTs count of a given address
        /// </summary>
        [HttpGet]
        [Route("wallet/count")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetNftsCountRes))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorRes))]
        public async Task<IActionResult> GetNftsCount()
        {
            try
            {
                var senderPubKey = HttpContext.PublicKey();

                var balanceOfAddr = await _kryxiviaNftService.BalanceOfQueryAsync(senderPubKey);
                return Ok(new GetNftsCountRes() { Count = ((int)balanceOfAddr % int.MaxValue) });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error has occurred while retrieving NFTs count");
                return Error(ErrorRes.Get("Error retrieving NFTs count"));
            }
        }
    }
}