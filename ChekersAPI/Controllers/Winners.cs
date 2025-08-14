using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Microsoft.AspNetCore.Http.HttpResults;
using CheckersEngine;

namespace ChekersAPI.Controllers
{
    [ApiController]
    public class WinnersController : ControllerBase // Consider renaming class to follow convention
    {
        private string projectId = "checkers-198a5";                      
        string jsonPath = Path.Combine("D:\\ChekersAPI\\ChekersAPI\\Properties\\key.json");
        private readonly FirestoreDb db;
        public WinnersController(IConfiguration configuration)
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", jsonPath);
            this.db = FirestoreDb.Create(projectId);
        }

        [HttpPost("addwinner")]
        public async Task<IActionResult> AddWinner([FromBody] WinnerSubmission i_Submission)
        {
            try
            {
                if(!ModelState.IsValid)
                {
                    return BadRequest("invalid json format");
                }


                GameObject winningGameObject = ValidationUtils.ValidateAndParseRequest(i_Submission.MoveRequest);
                if (!ValidationUtils.IsComputerLoss(winningGameObject))
                {
                    return BadRequest("position is not winning");
                }

                string[] gameSequence = winningGameObject.GetGameSequence();
                // Get a reference to the winners collection
                CollectionReference winnersCollection = db.Collection("winners");

                // Create a query to get the top 5 winners (ordered by timestamp descending)
                Query topWinnersQuery = winnersCollection.OrderBy(fieldPath: "timestamp").LimitToLast(5);

                // Get the snapshot of the top 5 winners
                QuerySnapshot winnersSnapshot = await topWinnersQuery.GetSnapshotAsync();

                // Check if there are already 5 winners
                if (winnersSnapshot.Documents.Count == 5)
                {
                    // Need to remove the oldest winner (first document in the snapshot)
                    DocumentReference oldestWinnerRef = winnersSnapshot.Documents[0].Reference;
                    await oldestWinnerRef.DeleteAsync();
                }

                // Add the new winner document with name and timestamp
                await winnersCollection.AddAsync(new
                {
                    name = i_Submission.Winner.Name,
                    timestamp = Timestamp.GetCurrentTimestamp(),
                    gameSequence = gameSequence
                });

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("getwinners")]
        public async Task<IActionResult> GetWinners()
        {
            // Get a query to the winners collection, ordered by timestamp (descending for latest)
            Query winnersQuery = db.Collection("winners").OrderByDescending(fieldPath: "timestamp");

            // Limit the query to top 5 documents
            Query limitedQuery = winnersQuery.LimitToLast(5);

            // Get the winners snapshot
            QuerySnapshot winnersSnapshot = await limitedQuery.GetSnapshotAsync();

            // Check if there are any winners
            if (winnersSnapshot.Documents.Count == 0)
            {
                return NotFound("No winners found.");
            }

            // List to store winner objects
            List<WinnerResponse> winners = new List<WinnerResponse>();

            // Loop through each winner document and extract data
            foreach (DocumentSnapshot winnerDoc in winnersSnapshot)
            {
                string winnerName = winnerDoc.GetValue<string>("name");
                string[] winnerGame = winnerDoc.GetValue<string[]>("gameSequence");
                winners.Add(new WinnerResponse { Name  = winnerName, MoveSequence = winnerGame.Skip(1).ToArray(), PositionSequence = ParsingUtils.GetPositionSequence(winnerGame) });
            }

            // Return the list of winners
            return Ok(winners);
        }

    }
}
