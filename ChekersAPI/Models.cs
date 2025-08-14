using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;


namespace ChekersAPI
{
    public record StartGameRequest 
    {
        [Required(ErrorMessage = "Color is required")]
        [RegularExpression("^(black|white)$", ErrorMessage = "Color must be either 'black' or 'white'")]
        public required string TurnColor { get; set; }

        [Required(ErrorMessage = "Color is required")]
        [RegularExpression("^(black|white)$", ErrorMessage = "Color must be either 'black' or 'white'")]
        public required string ComputerColor { get; set; }

        [Required(ErrorMessage = "Position is required")]
        [StringLength(32, ErrorMessage = "Position must be exactly 32 characters long")]
        public required string Position { get; set; }
    }


    // Data model for receiving checkers positions
    public record CheckersMoveRequest
    {
        [Required(ErrorMessage = "GameId is required")]
        [Length(8, 8, ErrorMessage = "Length must be 8")]
        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
        public required string GameId { get; set; }

        [Required(ErrorMessage = "Position is required")]
        [StringLength(32, ErrorMessage = "Position must be exactly 32 characters long")]
        public required string Position { get; set; }
    }

    public record CheckersMoveReply
    {
        public required string Move { get; set; }
        public required float Evaluation { get; set; }
        public required int PathLength { get; set; }
    }

    public record GameId
    { 
        public required string ID { get; set; }
    }

    public record WinnerSubmission
    {
        public required CheckersMoveRequest MoveRequest { get; init; }
        public required Winner Winner { get; init; }
    }

    public record Winner
    {
        public required string Name { get; set; }
    }

    public record WinnerResponse
    {
        public required string Name { get; set; }
        public required string[] PositionSequence { get; set; }
        public required string[] MoveSequence { get; set; }

    }

    [JsonSerializable(typeof(List<WinnerResponse>))]
    [JsonSerializable(typeof(WinnerResponse))]
    [JsonSerializable(typeof(WinnerSubmission))]
    [JsonSerializable(typeof(StartGameRequest))]
    [JsonSerializable(typeof(GameId))]
    [JsonSerializable(typeof(List<Winner>))]
    [JsonSerializable(typeof(Winner))]
    [JsonSerializable(typeof(CheckersMoveRequest))]
    [JsonSerializable(typeof(CheckersMoveReply))]
    [JsonSerializable(typeof(ValidationProblemDetails))]
    public partial class MyJsonSerializerContext : JsonSerializerContext
    {
        // The source generator will fill in the necessary details.
    }
}
