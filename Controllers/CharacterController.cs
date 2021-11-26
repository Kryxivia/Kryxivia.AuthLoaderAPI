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
using Kryxivia.AuthLoaderAPI.Abstractions.Requests;

namespace Kryxivia.AuthLoaderAPI.Controllers
{
    [ApiController]
    [JwtAuthorize]
    [Route("api/v1/characters")]
    public class CharacterController : _ControllerBase
    {
        private readonly CharacterRepository _characterRepository;

        public CharacterController(CharacterRepository characterRepository)
        {
            _characterRepository = characterRepository;
        }

        /// <summary>
        /// Create a character for the authenticated address
        /// </summary>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterReq req)
        {
            var senderPubKey = HttpContext.PublicKey();

            var character = new Character()
            {
                PublicKey = senderPubKey,
                Name = req.Name,
                Gender = req.Gender,
            };

            var characterId = await _characterRepository.Create(character);
            if (!string.IsNullOrWhiteSpace(characterId))
                return Ok(characterId);
            else
                return Error();
        }

        /// <summary>
        /// Archive a character from a given character id for the authenticated address
        /// </summary>
        [HttpDelete]
        [Route("{characterId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ArchiveCharacter(string characterId)
        {
            var senderPubKey = HttpContext.PublicKey();

            var targetCharacter = await _characterRepository.GetActive(characterId);
            if (!string.IsNullOrWhiteSpace(targetCharacter?.IdAsString) && targetCharacter?.PublicKey == senderPubKey)
            {
                targetCharacter.IsArchived = true;
                targetCharacter.ArchivedAt = DateTime.Now;

                if (await _characterRepository.Update(characterId, targetCharacter))
                {
                    return Ok();
                }
                else
                {
                    return Error();
                }
            }
            else
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Returns characters list for the authenticated address
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCharactersList()
        {
            var senderPubKey = HttpContext.PublicKey();

            var characters = await _characterRepository.GetAllActiveByPublicKey(senderPubKey);

            if (characters?.Count == 0)
                return NotFound();

            // Removing InventoryItems from json result, we don't need it now
            characters.ForEach(x => x.InventoryItems = null);

            return Ok(characters);
        }

        /// <summary>
        /// Returns characters list for the authenticated address
        /// </summary>
        [HttpGet]
        [Route("names/{characterName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CharacterNameExists(string name)
        {
            var characters = await _characterRepository.GetAllByName(name);
            return Ok(new { Exists = characters?.Count >= 1 });
        }
    }
}
