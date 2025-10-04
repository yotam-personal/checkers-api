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
                return new GameId { ID = gameRequest.Id };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in startgame: {Message}", ex.Message);
                return BadRequest(new { error = "invalid request to the server", details = ex.Message });
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