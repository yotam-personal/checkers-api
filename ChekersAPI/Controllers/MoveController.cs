using Microsoft.AspNetCore.Mvc;
using CheckersEngine;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChekersAPI.Controllers
{
    [ApiController]
    public class CheckersController : ControllerBase
    {
        [HttpPost("getbestmove")]
        public async Task<ActionResult<string>> GetBestMove([FromBody] CheckersMoveRequest moveRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("format error - please send a request of the format: GameID: , Color:, State: ");
            }

            try
            {
                MoveResponseAgent agent = MoveResponseAgent.GetInstance(moveRequest);
                CheckersMoveReply computerMove = await agent.GenerateMoveResponse();
                Console.WriteLine(computerMove);
                return Ok(computerMove);
            }
            catch (Exception ex)
            {
                return BadRequest("Error: " + ex.Message);
            }
        }
    }
}
