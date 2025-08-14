using CheckersEngine;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Text.Json.Serialization;

namespace ChekersAPI.Controllers
{
    [ApiController]
    public class SessionController : Controller
    {
        [HttpPost("startgame")]
        public async Task<ActionResult<GameId>> InitializeSession([FromBody] StartGameRequest i_Request)
        {
            try
            {
                // Validate the request model
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                GameObjectRequest gameRequest = await getGameObjectRequest(i_Request);
                GameTracker.Instance.AddGame(gameRequest);

                Console.WriteLine($"Game Started with GameID: {gameRequest.Id}");
                return new GameId { ID = gameRequest.Id };
            }
            catch (Exception ex)
            {
                return BadRequest("invalid request to the server");
            }
        }

        private async Task<GameObjectRequest> getGameObjectRequest(StartGameRequest i_Request)
        {
            string ID = await GameTracker.Instance.GenerateUniqueGameIdAsync();
            uint[] boardState = ValidationUtils.ValidateAndGetBoard(i_Request.Position);
            eColor requestTurnColor = i_Request.TurnColor.Equals("white") ? eColor.White : eColor.Black;
            eColor requestComputerColor = i_Request.ComputerColor.Equals("white") ? eColor.White : eColor.Black;

            return new GameObjectRequest { Id = ID, BoardState = boardState, TurnColor = requestTurnColor, 
                ComputerColor = requestComputerColor };
        }
    }
}
