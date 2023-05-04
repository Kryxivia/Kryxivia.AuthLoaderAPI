﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Kryxivia.AuthLoaderAPI.Controllers.Requests;
using Kryxivia.AuthLoaderAPI.Controllers.Responses;
using Kryxivia.AuthLoaderAPI.Middlewares.Attributes;
using Kryxivia.Domain.MongoDB.Models.Game;
using Kryxivia.Domain.MongoDB.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kryxivia.AuthLoaderAPI.Controllers
{
    [ApiController]
    [JwtAuthorize]
    [Route("api/v1/sessions")]
    public class CharacterSessionController : _ControllerBase
    {
        private readonly CharacterSessionRepository _characterSessionRepository;

        public CharacterSessionController(CharacterSessionRepository characterSessionRepository)
        {
            _characterSessionRepository = characterSessionRepository;
        }

        /// <summary>
        /// Retrieves a list of character sessions based on a list of character names and remove ServerIp and ServerPort
        /// </summary>
        /// <param name="req">The list of character names to search for.</param>
        /// <returns>A list of character sessions corresponding to the specified names.</returns>
        [HttpPost]
        [Route("get-by-names")]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetSessionsRes))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorRes))]
        public async Task<IActionResult> GetCharacterSessionByNames([FromBody] GetSessionsReq req)
        {
            if (req.SessionToFind == null || req.SessionToFind.Count == 0)
                return NotFound(ErrorRes.Get("No names found"));

            List<CharacterSession> sessions = await _characterSessionRepository.GetAllByNames(req.SessionToFind);

            if (sessions == null || sessions.Count == 0)
                return NotFound(ErrorRes.Get("No sessions found"));

            sessions.ForEach(x => { x.ServerIp = null; x.ServerPort = -1; });

            GetSessionsRes sessionsRes = new GetSessionsRes();
            sessionsRes.CharactersSessions = sessions;

            return Ok(sessionsRes);
        }
    }
}
