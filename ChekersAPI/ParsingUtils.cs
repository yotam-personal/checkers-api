using System.Runtime.CompilerServices;

namespace ChekersAPI
{
    public class ParsingUtils
    {
        internal static string[] GetPositionSequence(string[] winnerGame)
        {
            List<string> positionSequence = [winnerGame[0]];
            foreach (string s in winnerGame.Skip(1))
            {
                foreach(string move in getAllMoves(s))
                {
                    positionSequence.Add(getBoardStateAfterMove(move, positionSequence.Last()));
                }
            }

            return positionSequence.ToArray();  
        }

        private static IEnumerable<string> getAllMoves(string i_DBMove)
        {
            // DBMove is of the form Move: moves | Evaluation: eval | Depth: depth
            string moves = i_DBMove.Split('|')[0].Split(':')[1];
            foreach(string _move in moves.Split(' '))
            {
                if (!string.IsNullOrEmpty(_move))
                {
                    yield return _move;
                }
            }
        }

        // the moves are of the form StartSquare-TargetSquare
        private static string getBoardStateAfterMove(string i_Move, string i_BoardState)
        {
            int[] squares = i_Move.Split("-").Select(getSquare).ToArray();
            char[] boardState = i_BoardState.ToCharArray();
            boardState[squares[1]] = boardState[squares[0]];
            boardState[squares[0]] = '#';
            int squareDifference = Math.Abs(squares[1] - squares[0]);
            checkForCapture(squares, squareDifference, boardState);
            checkForPromotion(boardState);

            return new string(boardState);
        }

        private static void checkForPromotion(char[] i_BoardState)
        {
            // Check the first four indices for '0'
            for (int i = 0; i < 4; i++)
            {
                if (i_BoardState[i] == '0')
                {
                    i_BoardState[i] = '2';
                }
            }

            // Check the last four indices for '1'
            for (int i = 28; i < 32; i++)
            {
                if (i_BoardState[i] == '1')
                {
                    i_BoardState[i] = '3';
                }
            }
        }

        private static void checkForCapture(int[] i_Squares, int i_SquareDifference, char[] i_BoardState)
        {
            if (i_SquareDifference == 7 || i_SquareDifference == 9)
            {
                int capturedPieceSquare = Math.Min(i_Squares[1], i_Squares[0]);
                if (i_SquareDifference == 7)
                {
                    if (capturedPieceSquare / 4 % 2 == 0)
                    {
                        capturedPieceSquare += 4;
                    }
                    else
                    {
                        capturedPieceSquare += 3;
                    }
                }
                else if (i_SquareDifference == 9)
                {
                    if (capturedPieceSquare / 4 % 2 == 0)
                    {
                        capturedPieceSquare += 5;
                    }
                    else
                    {
                        capturedPieceSquare += 4;
                    }
                }

                i_BoardState[capturedPieceSquare] = '#';
            }
        }

        private static int getSquare(string i_Square)
        {
            return 31 - int.Parse(i_Square);
        }
    }
}
