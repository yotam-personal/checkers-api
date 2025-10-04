using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using CheckersEngine;

namespace ChekersAPI.Controllers
{
    [ApiController]
    public class WinnersController : ControllerBase
    {
        private readonly FirestoreDb db;
        private readonly ILogger<WinnersController> logger;

        public WinnersController(IConfiguration configuration, ILogger<WinnersController> logger)
        {
            this.logger = logger;

            string projectId = configuration["Firebase:ProjectId"];

            // Try to get credentials path from configuration
            string? credentialsPath = configuration["Firebase:CredentialPath"];

            if (!string.IsNullOrEmpty(credentialsPath))
            {
                // Use file-based credentials
                string fullPath = Path.IsPathRooted(credentialsPath)
                    ? credentialsPath
                    : Path.Combine(AppContext.BaseDirectory, credentialsPath);

                if (System.IO.File.Exists(fullPath))
                {
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", fullPath);
                    this.db = FirestoreDb.Create(projectId);
                    logger.LogInformation("Firebase initialized with credentials from: {Path}", fullPath);
                }
                else
                {
                    logger.LogError("Credential file not found at: {Path}", fullPath);
                    throw new FileNotFoundException($"Firebase credential file not found at: {fullPath}");
                }
            }
            else
            {
                // Use default credentials (works in GCP environments like Cloud Run, GKE, etc.)
                logger.LogInformation("Using default application credentials for Firebase");
                this.db = FirestoreDb.Create(projectId);
            }
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

                GameObject winningGameObject = ValidationUtils.ValidateAndParseRequest(i_Submission.MoveRequest);
                if (!ValidationUtils.IsComputerLoss(winningGameObject))
                {
                    return BadRequest("Position is not winning");
                }

                string[] gameSequence = winningGameObject.GetGameSequence();
                CollectionReference winnersCollection = db.Collection("winners");

                // Get the top 5 winners (ordered by timestamp)
                Query topWinnersQuery = winnersCollection
                    .OrderBy("timestamp")
                    .LimitToLast(5);

                QuerySnapshot winnersSnapshot = await topWinnersQuery.GetSnapshotAsync();

                // Remove oldest winner if we already have 5
                if (winnersSnapshot.Documents.Count >= 5)
                {
                    DocumentReference oldestWinnerRef = winnersSnapshot.Documents[0].Reference;
                    await oldestWinnerRef.DeleteAsync();
                    logger.LogInformation("Removed oldest winner to make room for new winner");
                }

                // Add the new winner
                await winnersCollection.AddAsync(new
                {
                    name = i_Submission.Winner.Name,
                    timestamp = Timestamp.GetCurrentTimestamp(),
                    gameSequence = gameSequence
                });

                logger.LogInformation("Added new winner: {Name}", i_Submission.Winner.Name);
                return Ok(new { message = "Winner added successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding winner");
                return StatusCode(500, new { error = "An error occurred while adding the winner" });
            }
        }

        [HttpGet("getwinners")]
        public async Task<IActionResult> GetWinners()
        {
            try
            {
                Query winnersQuery = db.Collection("winners")
                    .OrderByDescending("timestamp")
                    .Limit(5);

                QuerySnapshot winnersSnapshot = await winnersQuery.GetSnapshotAsync();

                if (winnersSnapshot.Documents.Count == 0)
                {
                    return Ok(new List<WinnerResponse>()); // Return empty list instead of 404
                }

                List<WinnerResponse> winners = winnersSnapshot.Documents
                    .Select(winnerDoc => new WinnerResponse
                    {
                        Name = winnerDoc.GetValue<string>("name"),
                        MoveSequence = winnerDoc.GetValue<string[]>("gameSequence").Skip(1).ToArray(),
                        PositionSequence = ParsingUtils.GetPositionSequence(winnerDoc.GetValue<string[]>("gameSequence"))
                    })
                    .ToList();

                return Ok(winners);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving winners");
                return StatusCode(500, new { error = "An error occurred while retrieving winners" });
            }
        }
    }
}