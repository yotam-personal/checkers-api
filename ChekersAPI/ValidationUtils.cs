using Azure.Core;
using CheckersEngine;
using Microsoft.Identity.Client;

namespace ChekersAPI
{
    public static class ValidationUtils
    {
        public static bool ValidateBoard(string board)
        {
            // this method validates the string passed from the client is a valid representetion of a board
            // meaning it may only contain #0123, each signaling one of the possible 5 options of a square state
            // this method doesn't validate if the position passed is an illegal position in the sense of it
            // being a legal position representation but the position itself not being valid
            if (board.Length != 32)
            {
                return false;
            }

            foreach (char c in board)
            {
                if (c != '0' && c != '1' && c != '2' && c != '3' && c != '#')
                {
                    return false;
                }
            }


            return true;
        }

        public static uint[] ValidateAndGetBoard(string board) 
        {
            bool validBoard = ValidateBoard(board);
            if (!validBoard)
            {
                throw new ArgumentException("invalid board state");
            }
            // generates bitboard representation of the position
            return CheckersBitBoardHelper.GenerateBitBoards(board);
        }

        // checks a move request against the associated game object, makes the changes in game object
        // to be prepared for the incoming move generation
        public static GameObject ValidateAndParseRequest(CheckersMoveRequest i_Request)
        {
            GameObject gameObject;
            bool isValidGameId = GameTracker.Instance.TryAccessGame(i_Request.GameId, out gameObject);
            if (!isValidGameId)
            {
                throw new ArgumentException("Invalid GameID");
            }
            // validates if the request's board state is valid, if valid, it is parsed from it's string format to bitboard format
            // if not valid - ArgumentException is thrown
            uint[] requestBoardState = ValidateAndGetBoard(i_Request.Position);
            gameObject.ValidateAndChangeBoardState(requestBoardState);

            return gameObject;
        }

        public static bool IsComputerLoss(GameObject i_GameObject)
        {
            return isStartingPosition(i_GameObject.StartingPosition) && i_GameObject.IsComputerLoss();
        }

        private static bool isStartingPosition(uint[] i_BoardState)
        {
            /*
            bool blackSolidersStarting = i_BoardState[(int)ePiece.sBlack - 1] == 0xFFF;
            bool whiteSolidersStarting = i_BoardState[(int)ePiece.sWhite - 1] == 0xFFF00000;
            bool blackQueensStarting = i_BoardState[(int)ePiece.qBlack - 1] == 0;
            bool whiteQueensStarting = i_BoardState[(int)ePiece.qWhite - 1] == 0;

            return blackSolidersStarting && whiteSolidersStarting && blackQueensStarting && whiteQueensStarting;
            */
            return true;
        }
    }
}
