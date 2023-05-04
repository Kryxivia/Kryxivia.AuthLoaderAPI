using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Kryxivia.AuthLoaderAPI.Middlewares.Attributes;
using Kryxivia.Domain.MongoDB.Models.Game;
using Kryxivia.Domain.MongoDB.Repositories;
using Kryxivia.AuthLoaderAPI.Controllers.Requests;
using Kryxivia.AuthLoaderAPI.Controllers.Responses;
using Kryxivia.Shared.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Kryxivia.AuthLoaderAPI.Controllers;

namespace Kryxivia.AuthLoaderAPI.Controllers
{
    [ApiController]
    [JwtAuthorize]
    [Route("api/v1/status")]
    public class StatusController : _ControllerBase
    {
        private readonly CharacterRepository _characterRepository;

        public StatusController(CharacterRepository characterRepository)
        {
            _characterRepository = characterRepository;
        }

        /// <summary>
        /// Returns status for a given character id
        /// </summary>
        [HttpGet]
        [Route("{characterId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StatusRes))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public async Task<IActionResult> GetStatus(string characterId)
        {
            var targetCharacter = await _characterRepository.GetActive(characterId);
            if (targetCharacter == null)
                return NotFound(ErrorRes.Get("No character found"));

            return Ok(new StatusRes() 
            {
                IsOnline = targetCharacter.IsLogged
            });
        }
    }
}
