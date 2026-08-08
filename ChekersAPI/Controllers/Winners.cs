using Microsoft.AspNetCore.Mvc;
using CheckersEngine;
using Npgsql;

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

                // Postgres TEXT cannot hold a NUL byte, and a lone surrogate cannot be
                // encoded as UTF-8 at all. Both reached the driver and came back as a 500 —
                // which is not merely the wrong code: it also marked the database down and
                // stopped the durable game counter, so one crafted name from any client
                // suppressed counting for everybody.
                if (hasUnstorableText(i_Submission.Winner.Name))
                {
                    return BadRequest("Name may not contain control characters");
                }

                string[] gameSequence = winningGameObject.GetGameSequence();
                await Db.AddWinnerAsync(i_Submission.Winner.Name, gameSequence);
                GameMetrics.WinsRecorded.Inc();

                logger.LogInformation("Added new winner: {Name}", i_Submission.Winner.Name);
                return Ok("Winner added successfully");
            }
            catch (ArgumentException ex)
            {
                // An expired or replayed game id, or a position that does not follow from
                // the tracked game. The caller's problem, not the server's — and reporting
                // it as a 500 made a routine replay indistinguishable in the logs from a
                // real outage.
                logger.LogInformation("Rejected a winner submission: {Reason}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (PostgresException ex) when (ex.SqlState.StartsWith("22") || ex.SqlState.StartsWith("23"))
            {
                logger.LogInformation(ex, "Rejected a winner submission the database refused");
                return BadRequest("Winner could not be stored as submitted");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding winner");
                return StatusCode(500, "An error occurred while adding the winner");
            }
        }


        /// <summary>
        /// Text Postgres cannot store or UTF-8 cannot encode: control characters (a NUL byte
        /// is rejected by the server as an invalid UTF-8 sequence) and unpaired surrogates.
        ///
        /// Pairs, not surrogates. Every emoji outside the basic plane is encoded in UTF-16 as
        /// a surrogate PAIR, so rejecting any surrogate rejected a trophy emoji — a perfectly
        /// storable name that the Firestore version accepted and that round-trips byte for
        /// byte through this one. Only a high surrogate with no low surrogate after it, or a
        /// low surrogate with nothing before it, cannot be encoded.
        /// </summary>
        private static bool hasUnstorableText(string name)
        {
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsControl(c))
                {
                    return true;
                }
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 >= name.Length || !char.IsLowSurrogate(name[i + 1]))
                    {
                        return true;
                    }
                    i++;
                }
                else if (char.IsLowSurrogate(c))
                {
                    return true;
                }
            }
            return false;
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
                // Row by row, each in its own try. The projection used to run as one LINQ
                // statement over all five rows, so a single unreplayable sequence returned
                // 500 for the entire leaderboard instead of costing one entry.
                var winners = new List<WinnerResponse>();
                foreach (var row in rows)
                {
                    try
                    {
                        winners.Add(new WinnerResponse
                        {
                            Name = row.Name,
                            MoveSequence = row.GameSequence.Skip(1).ToArray(),
                            PositionSequence = ParsingUtils.GetPositionSequence(row.GameSequence)
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Skipping a leaderboard entry whose game cannot be replayed");
                    }
                }

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
