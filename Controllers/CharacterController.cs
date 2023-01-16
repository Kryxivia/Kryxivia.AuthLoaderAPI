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
using Kryxivia.AuthLoaderAPI.Controllers.Responses;

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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreateCharacterRes))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorRes))]
        public async Task<IActionResult> CreateCharacter([FromBody] CreateCharacterReq req)
        {
            var senderPubKey = HttpContext.PublicKey();

            var account = await _accountRepository.GetByPublicKey(senderPubKey);
            if (account != null && !account.IsAdmin)
            {
                var characterCount = await _characterRepository.GetAllActiveByPublicKey(senderPubKey);
                if (characterCount?.Count >= Constants.MAX_CHARACTER_PER_ACCOUNT)
                    return Error(ErrorRes.Get($"Characters count limited to {Constants.MAX_CHARACTER_PER_ACCOUNT} per account"));
            }

            var isNameAvailable = await IsNameAvailable(req.Name);
            if (!isNameAvailable) return Error(ErrorRes.Get("This character name is already used"));

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
                var now = DateTime.Now;

                var welcomeInventoryStuff = await _gameParametersRepository.GetByName<List<PositionedItem>>("WelcomeInventoryStuff");
                welcomeInventoryStuff.ForEach(x =>
                {
                    x.Item.Id = ObjectId.GenerateNewId();
                    x.Item.CreatedAt = now;
                    x.Item.UpdatedAt = now;

                    character.InventoryItems.Add(x);
                });

                var welcomeEquippedStuff = await _gameParametersRepository.GetByName<List<EquipmentItem>>("WelcomeEquippedStuff");
                welcomeEquippedStuff.ForEach(x =>
                {
                    x.Id = ObjectId.GenerateNewId();
                    x.CreatedAt = now;
                    x.UpdatedAt = now;

                    character.EquippedItems.Add(x);
                });
            }

            var characterId = await _characterRepository.Create(character);
            if (!string.IsNullOrWhiteSpace(characterId))
                return Ok(new CreateCharacterRes() { CharacterId = characterId });
            else
                return Error(ErrorRes.Get("Error creating character"));
        }

        /// <summary>
        /// Archive a character from a given character id for the authenticated address
        /// </summary>
        [HttpDelete]
        [Route("{characterId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ErrorRes))]
        public async Task<IActionResult> ArchiveCharacter(string characterId)
        {
            var senderPubKey = HttpContext.PublicKey();

            var targetCharacter = await _characterRepository.GetActive(characterId);
            if (!string.IsNullOrWhiteSpace(targetCharacter?.IdAsString) && targetCharacter?.PublicKey == senderPubKey)
            {
                if (await _characterRepository.UpdatePropertyAsync(characterId, x => x.IsArchived, true) && await _characterRepository.UpdatePropertyAsync(characterId, x => x.ArchivedAt, DateTime.Now))
                {
                    return Ok();
                }
                else
                {
                    return Error(ErrorRes.Get("Error updating character"));
                }
            }
            else
            {
                return Error(ErrorRes.Get("Unauthorized sender"));
            }
        }

        /// <summary>
        /// Returns characters list for the authenticated address
        /// </summary>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Character>))]
        public async Task<IActionResult> GetCharactersList()
        {
            var senderPubKey = HttpContext.PublicKey();

            var characters = await _characterRepository.GetAllActiveByPublicKey(senderPubKey) ?? new List<Character>();

            // Removing InventoryItems from json result
            characters.ForEach(x => x.InventoryItems = null);

            // Removing Spells from json result
            characters.ForEach(x => x.Spells = null);

            // Removing ServerIp and ServerPort from json result
            characters.ForEach(x => { x.ServerIp = null; x.ServerPort = -1; });

            return Ok(characters);
        }

        /// <summary>
        /// Returns a flag indicating if a character exists with the given name
        /// </summary>
        [HttpGet]
        [Route("names/{name}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CharacterNameExistsRes))]
        public async Task<IActionResult> CharacterNameExists(string name)
        {
            return Ok(new CharacterNameExistsRes() { Exists = !(await IsNameAvailable(name)) });
        }

        /// <summary>
        /// Returns a flag indicating if a character is owned by the authenticated address
        /// </summary>
        [HttpGet]
        [Route("ownership/{characterId}")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IsOwnerOfRes))]
        public async Task<IActionResult> IsOwnerOf(string characterId)
        {
            bool isOwner = false;

            var senderPubKey = HttpContext.PublicKey();

            var character = await _characterRepository.GetActive(characterId);
            if (character != null && character.PublicKey == senderPubKey) isOwner = true;

            return Ok(new IsOwnerOfRes() { Owner = isOwner });
        }

        /// <summary>
        /// Returns a flag indicating if a character is owned by the authenticated address
        /// </summary>
        [HttpGet]
        [Route("ownership/{characterId}/login")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IsOwnerOfResThenLogin))]
        public async Task<IActionResult> IsOwnerOfThenLogin(string characterId)
        {
            bool isOwner = false;
            bool success = false;
            bool isBanned = false;
            string banPeriod = "";

            var senderPubKey = HttpContext.PublicKey();

            Character character = await _characterRepository.GetActive(characterId);
            Account account = await _accountRepository.GetByPublicKey(senderPubKey);

            if (character != null && character.PublicKey == senderPubKey)
            {
                await UpdateVersion(character);
                await CleanInventory(character, account);
                isOwner = true;

                if (!character.IsLogged)
                {
                    if (await _characterRepository.UpdatePropertyAsync(character.IdAsString, x => x.IsLogged, true)) success = true;
                }

                if (account.BanPeriod.HasValue && account.BanPeriod > DateTime.Now)
                {
                    isBanned = true;
                    banPeriod = account.BanPeriod?.ToString("MM/dd/yyyy HH:mm");
                }
            }

            return Ok(new IsOwnerOfResThenLogin() { Owner = isOwner, Success = success, IsBanned = isBanned, BanPeriod = banPeriod, IsAdmin = account.IsAdmin });
        }

        #region Utilities

        private async Task<bool> IsNameAvailable(string name)
        {
            var characters = await _characterRepository.GetAllByName(name);
            return characters == null || characters?.Count == 0;
        }

        private async Task CleanInventory(Character character, Account account)
        {
            var distinctAndCleanedBank = account.Bank.Items.GroupBy(x => x.Position).Select(y => y.First()).Where(i => i.Position >= 0).ToList();

            if (distinctAndCleanedBank.Count != account.Bank.Items.Count)
            {
                account.Bank.Items = distinctAndCleanedBank;
                await _accountRepository.Update(account.PublicKey, account);
            }

            var distinctAndCleanedInventory = character.InventoryItems.GroupBy(x => x.Position).Select(y => y.First()).Where(i => i.Position >= 0 && i.Position <= 50).ToList();

            if (distinctAndCleanedInventory.Count > 0 && distinctAndCleanedInventory.Count != character.InventoryItems.Count)
            {
                character.InventoryItems = distinctAndCleanedInventory;
                await _characterRepository.UpdatePropertyAsync(character.IdAsString, x => x.InventoryItems, character.InventoryItems);
            }
        }

        private async Task UpdateVersion(Character character)
        {
            await _characterRepository.UpdatePropertyAsync(character.IdAsString, x => x.QuestVersion, 0);
        }

        #endregion
    }
}
