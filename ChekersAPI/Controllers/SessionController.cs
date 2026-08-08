using ChekersAPI;
using CheckersEngine;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Text.Json.Serialization;

namespace ChekersAPI.Controllers
{
    [ApiController]
    public class SessionController : Controller
    {
        private readonly ILogger<SessionController> _logger;

        public SessionController(ILogger<SessionController> logger)
        {
            _logger = logger;
        }

        [HttpPost("startgame")]
        public async Task<ActionResult<GameId>> InitializeSession([FromBody] StartGameRequest i_Request)
        {
            try
            {
                _logger.LogInformation("StartGame called");

                // Validate the request model
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid");
                    return BadRequest(ModelState);
                }

                _logger.LogInformation("Creating game object request");
                GameObjectRequest gameRequest = await getGameObjectRequest(i_Request);

                _logger.LogInformation("Adding game to tracker with ID: {GameId}", gameRequest.Id);
                GameTracker.Instance.AddGame(gameRequest);

                _logger.LogInformation("Game Started with GameID: {GameId}", gameRequest.Id);
                GameMetrics.GamesStarted.Inc();
                await recordGameStarted();

                return new GameId { ID = gameRequest.Id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in startgame: {Message}", ex.Message);
                return BadRequest(new { error = "invalid request to the server", details = ex.Message });
            }
        }

        // Best effort on purpose. The lifetime counter lives in Postgres so it survives a
        // deploy, but a game does not need Postgres to be played — so an unreachable
        // database costs a tick on a dashboard and nothing else. The alternative, letting
        // this throw, would make the leaderboard's availability the game's availability.
        private async Task recordGameStarted()
        {
            if (!Db.Ready)
            {
                return;
            }

            try
            {
                long total = await Db.IncrementCounterAsync(Db.GamesStartedCounter);
                GameMetrics.GamesAllTime.Set(total);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "could not record the game in the lifetime counter");
            }
        }

        private async Task<GameObjectRequest> getGameObjectRequest(StartGameRequest i_Request)
        {
            _logger.LogInformation("Generating unique game ID");
            string ID = await GameTracker.Instance.GenerateUniqueGameIdAsync();

            _logger.LogInformation("Validating and getting board");
            uint[] boardState = ValidationUtils.ValidateAndGetBoard(i_Request.Position);

            eColor requestTurnColor = i_Request.TurnColor.Equals("white") ? eColor.White : eColor.Black;
            eColor requestComputerColor = i_Request.ComputerColor.Equals("white") ? eColor.White : eColor.Black;

            return new GameObjectRequest
            {
                Id = ID,
                BoardState = boardState,
                TurnColor = requestTurnColor,
                ComputerColor = requestComputerColor
            };
        }
    }
}