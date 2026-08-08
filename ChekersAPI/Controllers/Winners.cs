using Microsoft.AspNetCore.Mvc;
using CheckersEngine;

namespace ChekersAPI.Controllers
{
    [ApiController]
    public class WinnersController : ControllerBase
    {
        private readonly ILogger<WinnersController> logger;

        public WinnersController(ILogger<WinnersController> logger)
        {
            this.logger = logger;
        }

        [HttpPost("addwinner")]
        public async Task<IActionResult> AddWinner([FromBody] WinnerSubmission i_Submission)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid json format");
                }

                // Unchanged and load-bearing: the client sends the winning position and the
                // server replays it against the live game before believing it. Without this,
                // "add me to the leaderboard" would be a request anyone could make about a
                // game they never played.
                GameObject winningGameObject = ValidationUtils.ValidateAndParseRequest(i_Submission.MoveRequest);
                if (!ValidationUtils.IsComputerLoss(winningGameObject))
                {
                    return BadRequest("Position is not winning");
                }

                string[] gameSequence = winningGameObject.GetGameSequence();
                await Db.AddWinnerAsync(i_Submission.Winner.Name, gameSequence);
                GameMetrics.WinsRecorded.Inc();

                logger.LogInformation("Added new winner: {Name}", i_Submission.Winner.Name);
                return Ok("Winner added successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding winner");
                return StatusCode(500, "An error occurred while adding the winner");
            }
        }

        [HttpGet("getwinners")]
        public async Task<IActionResult> GetWinners()
        {
            try
            {
                var rows = await Db.GetWinnersAsync();
                if (rows.Count == 0)
                {
                    return Ok(new List<WinnerResponse>());
                }

                // The stored sequence is [opening position, then one entry per move]. The
                // front end wants the moves without that first element, and the positions
                // derived by replaying them — exactly as the Firestore version returned it,
                // so the same front end reads either backend without noticing.
                List<WinnerResponse> winners = rows
                    .Select(row => new WinnerResponse
                    {
                        Name = row.Name,
                        MoveSequence = row.GameSequence.Skip(1).ToArray(),
                        PositionSequence = ParsingUtils.GetPositionSequence(row.GameSequence)
                    })
                    .ToList();

                return Ok(winners);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving winners");
                return StatusCode(500, "An error occurred while retrieving winners");
            }
        }
    }
}
