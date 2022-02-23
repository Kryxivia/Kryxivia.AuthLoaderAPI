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
using Kryxivia.AuthLoaderAPI.Controllers.Requests;
using System.Net.Mime;
using MongoDB.Bson;
using Kryxivia.Domain.MongoDB.Enums;
using Newtonsoft.Json;

namespace Kryxivia.AuthLoaderAPI.Controllers
{
    [ApiController]
    [JwtAuthorize]
    [Route("api/v1/characters")]
    public class CharacterController : _ControllerBase
    {
        private readonly AccountRepository _accountRepository;
        private readonly CharacterRepository _characterRepository;
        private readonly GameParametersRepository _gameParametersRepository;

        public CharacterController(AccountRepository accountRepository, CharacterRepository characterRepository, GameParametersRepository gameParametersRepository)
        {
            _accountRepository = accountRepository;
            _characterRepository = characterRepository;
            _gameParametersRepository = gameParametersRepository;
        }

        /// <summary>
        /// Create a character for the authenticated address
        /// </summary>
        [HttpPut]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterReq req)
        {
            var senderPubKey = HttpContext.PublicKey();

            var account = await _accountRepository.GetByPublicKey(senderPubKey);
            if (account != null && !account.IsAdmin)
            {
                var characterCount = await _characterRepository.GetAllActiveByPublicKey(senderPubKey);
                if (characterCount?.Count >= Constants.MAX_CHARACTER_PER_ACCOUNT) return Error($"Characters count limited to {Constants.MAX_CHARACTER_PER_ACCOUNT} per account");
            }

            var character = new Character()
            {
                PublicKey = senderPubKey,
                Name = req.Name,
                Gender = req.Gender,
                HairColor = req.HairColor,
                HairStyle = req.HairStyle,
                SkinColor = req.SkinColor,
                EyesColor = req.EyesColor,
                Blendshapes = req.Blendshapes
            };

            if (req.WelcomeStuff)
            {
                var welcomeInventoryStuff = await _gameParametersRepository.GetByName<List<PositionedItem>>("WelcomeInventoryStuff");
                if (welcomeInventoryStuff != null && welcomeInventoryStuff.Count > 0)
                    character.InventoryItems.AddRange(welcomeInventoryStuff);

                var welcomeEquippedStuff = await _gameParametersRepository.GetByName<List<EquipmentItem>>("WelcomeEquippedStuff");
                if (welcomeEquippedStuff != null && welcomeEquippedStuff.Count > 0)
                    character.EquippedItems.AddRange(welcomeEquippedStuff);
            }

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
        [Produces(MediaTypeNames.Application.Json)]
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
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Character>))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCharactersList()
        {
            var senderPubKey = HttpContext.PublicKey();

            var characters = await _characterRepository.GetAllActiveByPublicKey(senderPubKey);

            if (characters?.Count == 0)
                return NotFound();

            // Removing InventoryItems from json result
            characters.ForEach(x => x.InventoryItems = null);

            // Removing Spelss from json result
            characters.ForEach(x => x.Spells = null);

            return Ok(characters);
        }

        /// <summary>
        /// Returns a flag indicating if a character exists with the given name
        /// </summary>
        [HttpGet]
        [Route("names/{name}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> CharacterNameExists(string name)
        {
            var characters = await _characterRepository.GetAllByName(name);
            return Ok(new { Exists = characters?.Count >= 1 });
        }
    }
}
