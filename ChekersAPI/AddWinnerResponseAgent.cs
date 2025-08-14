using CheckersEngine;
namespace ChekersAPI
{
    public class AddWinnerResponseAgent
    {
        private readonly GameObject m_GameObject;
        private AddWinnerResponseAgent(GameObject gameObject)
        {
            m_GameObject = gameObject;
        }

        public static AddWinnerResponseAgent Create(WinnerSubmission i_Submission)
        {
            return new AddWinnerResponseAgent(ValidationUtils.ValidateAndParseRequest(i_Submission.MoveRequest));
        }
    }
}
