using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    public class CheckersBitBoardHelper
    {

        public static string GeneratePositionFromBitBoards(uint[] bitBoards)
        {
            StringBuilder position = new StringBuilder();
            uint mask = 0b1000_0000_0000_0000_0000_0000_0000_0000; // 32-bit mask

            for (int i = 0; i < 32; i++)
            {
                int pieceType = -1;
                for (int j = 0; j < bitBoards.Length; j++)
                {
                    if ((bitBoards[j] & mask) != 0)
                    {
                        pieceType = j;
                        break;
                    }
                }

                position.Append(pieceType == -1 ? '#' : (char)('0' + pieceType));
                mask >>= 1; // Shift the mask to the right by 1 bit
            }

            return position.ToString();
        }

        public static uint[] GenerateBitBoards(string i_Position)
        {
            uint[] bitBoards = new uint[4];
            for (int i = 0; i < 4; i++)
            {
                foreach (char c in i_Position)
                {
                    if (c - '0' == i)
                    {
                        bitBoards[i] = bitBoards[i] << 1 | 0b01;
                    }
                    else
                    {
                        bitBoards[i] <<= 1;
                    }
                }
            }

            return bitBoards;
        }

        internal static void MakeMove(IMove i_MoveToMake, uint[] i_BitBoards)
        {
            int movingPieceValue = i_MoveToMake.MovingPieceValue;
            i_BitBoards[movingPieceValue] ^= i_MoveToMake.StartSquare | i_MoveToMake.TargetSquare;
            if (i_MoveToMake.IsPromotion())
            {
                // 2 = queen offset
                i_BitBoards[movingPieceValue + 2] ^= i_MoveToMake.TargetSquare;
                i_BitBoards[movingPieceValue] ^= i_MoveToMake.TargetSquare;
            }

            if (i_MoveToMake.IsCapture())
            {
                int capturedPieceValue = i_MoveToMake.CapturedPieceValue;
                i_BitBoards[capturedPieceValue] ^= i_MoveToMake.CaptureSquare;
            }
        }

        internal static void MakeMoveSequence(IMove i_Move, uint[] i_BitBoards)
        {
            MakeMove(i_Move, i_BitBoards);
            if (i_Move.IsDoubleCapture())
            {
                MakeMoveSequence(i_Move.DoubleCapturesList[0], i_BitBoards);
            }
        }

        internal static void unMakeMove(IMove i_MoveToMake, uint[] i_BitBoards)
        {
            MakeMove(i_MoveToMake, i_BitBoards);
        }

        internal static IEnumerable<IMove> GeneratePositionAfterDoubleCaptures(IMove i_MoveToMake, uint[] i_BitBoards, bool backPropagation = false)
        {
            if (i_MoveToMake.IsDoubleCapture())
            {
                foreach (IMove moveDoubleCapture in i_MoveToMake.DoubleCapturesList)
                {
                    MakeMove(moveDoubleCapture, i_BitBoards);
                    foreach (IMove _move in GeneratePositionAfterDoubleCaptures(moveDoubleCapture, i_BitBoards, backPropagation))
                    {
                        if (backPropagation)
                        {
                            yield return new MoveAdapter(moveDoubleCapture as MoveAdapter, _move);
                        }
                        else
                        {
                            yield return null;
                        }
                    }

                    unMakeMove(moveDoubleCapture, i_BitBoards);
                }
            }
            else
            {
                yield return null;
            }
        }
    }
}
