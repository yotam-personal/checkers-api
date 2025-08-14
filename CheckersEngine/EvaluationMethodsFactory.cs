using System;
using System.Diagnostics;
using System.Runtime;
namespace CheckersEngine
{
    public enum EvaluationType
    {
        Basic,
        AdvancedWithMoveCount,
        QueenWorthUp,
        Advanced
    }

    public static class EvaluationMethodsFactory
    {
        private static readonly IMoveGenerator s_MoveGenerator = new MoveGenerator();

        // Piece-square tables for the opening game
        private static readonly float[,] openingSolidersTable = {
            {0f, 0f, 0f, 0f},
            {0.45f, 0.45f, 0.45f, 0.45f},
            {0.6f, 0.6f, 0.6f, 0.6f},
            {0.65f, 0.65f, 0.65f, 0.5f},
            {0.5f, 0.65f, 0.65f, 0.6f},
            {0.5f, 0.4f, 0.4f, 0.3f},
            {0.3f, 0.4f, 0.4f, 0.3f},
            {0.4f, 0.5f, 0.45f, 0.5f}
        };

        private static readonly float[,] endgameSolidersTable = {
            {0.0f, 0.0f, 0.0f, 0.0f},
            {0.68f, 0.68f, 0.68f, 0.68f},
            {0.57f, 0.57f, 0.57f, 0.57f},
            {0.45f, 0.45f, 0.45f, 0.5f},
            {0.4f, 0.4f, 0.4f, 0.4f},
            {0.4f, 0.4f, 0.4f, 0.35f},
            {0.4f, 0.4f, 0.4f, 0.4f},
            {0.4f, 0.4f, 0.4f, 0.4f}
        };


        private static readonly float[,] openingQueensTable = {
            {0.2f, 0.2f, 0.2f, 0.1f},
            {0.2f, 0.2f, 0.2f, 0.2f},
            {0.4f, 0.4f, 0.4f, 0.2f},
            {0.2f, 0.6f, 0.6f, 0.5f},
            {0.5f, 0.6f, 0.6f, 0.2f},
            {0.2f, 0.4f, 0.4f, 0.4f},
            {0.2f, 0.2f, 0.2f, 0.2f},
            {0.1f, 0.2f, 0.2f, 0.2f}
        };

        private static readonly float[,] endgameQueensTable = {
            {0.2f, 0.2f, 0.2f, 0.1f},
            {0.2f, 0.3f, 0.3f, 0.2f},
            {0.2f, 0.4f, 0.4f, 0.2f},
            {0.2f, 0.6f, 0.6f, 0.2f},
            {0.2f, 0.6f, 0.6f, 0.2f},
            {0.2f, 0.4f, 0.4f, 0.2f},
            {0.2f, 0.3f, 0.3f, 0.2f},
            {0.1f, 0.2f, 0.2f, 0.2f}
        };

        public static Func<uint[], float> GetEvaluationStrategy(EvaluationType i_Type)
        {
            Func<uint[], float> strategy = null;
            switch (i_Type)
            {
                case EvaluationType.Basic:
                    strategy = basic;
                    break;
                case EvaluationType.AdvancedWithMoveCount:
                    strategy = advancedWithMoveCount;
                    break;
                case EvaluationType.QueenWorthUp:
                    strategy = queenWorthUp;
                    break;
                case EvaluationType.Advanced:
                    strategy = advanced;
                    break;
            }

            return strategy!;
        }
        private static float basic(uint[] i_BitBoards)
        {
            int blackSolidersAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.sBlack - 1]);
            int blackQueenAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.qBlack - 1]);
            int whiteSolidersAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.sWhite - 1]);
            int whiteQueensAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.qWhite - 1]);
            // amount of white pieces - amount of black pieces. queens count as 2.
            return whiteSolidersAmount + (whiteQueensAmount * 1.8f) - (blackSolidersAmount + (blackQueenAmount * 1.8f));
        }

        private static float advancedWithMoveCount(uint[] i_BitBoards)
        {
            float whiteLegalMoves = s_MoveGenerator.GiveLegalMoves(i_BitBoards, eColor.White).Count;
            float blackLegalMoves = s_MoveGenerator.GiveLegalMoves(i_BitBoards, eColor.Black).Count;
            float advancedEvaluation = advanced(i_BitBoards);

            return advancedEvaluation + 0.1f * (whiteLegalMoves - blackLegalMoves);
        }

        private static float queenWorthUp(uint[] i_BitBoards)
        {
            int blackSolidersAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.sBlack - 1]);
            int blackQueenAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.qBlack - 1]);
            int whiteSolidersAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.sWhite - 1]);
            int whiteQueensAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.qWhite - 1]);
            // amount of white pieces - amount of black pieces. queens count as 2.
            return whiteSolidersAmount + whiteQueensAmount * 10 - (blackSolidersAmount + (blackQueenAmount * 10));
        }

        private static float advanced(uint[] i_BitBoards) 
        {
            int blackSolidersAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.sBlack - 1]);
            int blackQueenAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.qBlack - 1]);
            int whiteSolidersAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.sWhite - 1]);
            int whiteQueensAmount = BitUtils.GetSetBitsAmount(i_BitBoards[(int)ePiece.qWhite - 1]);

            float blackScore = blackSolidersAmount + (blackQueenAmount * 1.8f);
            float whiteScore = whiteSolidersAmount + (whiteQueensAmount * 1.8f);

            int totalPieces = blackSolidersAmount + blackQueenAmount + whiteSolidersAmount + whiteQueensAmount;
            return whiteScore - blackScore + GetPieceSquareValue(i_BitBoards, totalPieces);
        }
        
        private static float GetPieceSquareValue(uint[] bitBoards, int totalPieces)
        {
            float value = 0;
            int rank;
            int file;
            float endgameWeight = MathF.Max(0f, 1f - (totalPieces / 24f)); // Adjust the threshold as needed
            // White soliders
            uint whiteSoliders = bitBoards[(int)ePiece.sWhite - 1];
            foreach (uint piece in BitUtils.GetSetBits(whiteSoliders))
            {
                int square = BitUtils.FindBitPosition(piece);
                getFileAndRank(square, out file, out rank);

                value += (endgameWeight * endgameSolidersTable[rank, file]) +
                 ((1 - endgameWeight) * openingSolidersTable[rank, file]);
            }

            // Black soliders
            uint blackSoliders = bitBoards[(int)ePiece.sBlack - 1];
            foreach (uint piece in BitUtils.GetSetBits(blackSoliders))
            {
                int square = BitUtils.FindBitPosition(piece);
                getFileAndRank(square, out file, out rank);
                value -= (endgameWeight * endgameSolidersTable[7 - rank, file]) +
                 ((1 - endgameWeight) * openingSolidersTable[7 - rank, file]);
            }

            // White queens
            uint whiteQueens = bitBoards[(int)ePiece.qWhite - 1];
            foreach (uint piece in BitUtils.GetSetBits(whiteQueens))
            {
                int square = BitUtils.FindBitPosition(piece);
                getFileAndRank(square, out file, out rank);
                value += (endgameWeight * endgameQueensTable[rank, file]) +
                 ((1 - endgameWeight) * openingQueensTable[rank, file]);

            }

            // Black queens
            uint blackQueens = bitBoards[(int)ePiece.qBlack - 1];
            foreach (uint piece in BitUtils.GetSetBits(blackQueens))
            {
                int square = BitUtils.FindBitPosition(piece);
                getFileAndRank(square, out file, out rank);
                value -= (endgameWeight * endgameQueensTable[rank, file]) +
                 ((1 - endgameWeight) * openingQueensTable[rank, file]);
            }

            return (float)Math.Round(value, 3);
        }

        private static void getFileAndRank(int square, out int file, out int rank)
        {
            rank = square / 4;
            file = square % 4;
        }
    }
}
