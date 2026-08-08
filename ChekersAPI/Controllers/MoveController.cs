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
                GameMetrics.MovesRequested.WithLabels("ok").Inc();

                return Ok(computerMove);
            }
            catch (Exception ex)
            {
                // Labelled rather than counted separately: a rise in rejected moves is how
                // a broken front end or an expired game shows up, and it is only legible
                // next to the successful ones.
                GameMetrics.MovesRequested.WithLabels("rejected").Inc();
                return BadRequest("Error: " + ex.Message);
            }
        }
    }
}
