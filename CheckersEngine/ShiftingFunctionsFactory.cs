using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    internal class ShiftingFunctionsFactory
    {
        //storing bit position and moves as bit shifts
        private static Dictionary<int, Func<uint, uint>> wFirstMoveDict;
        private static Dictionary<int, Func<uint, uint>> wSecondMoveDict;
        private static Dictionary<int, Func<uint, uint>> wFirstCaptureDict;
        private static Dictionary<int, Func<uint, uint>> wSecondCaptureDict;
        private static Dictionary<int, Func<uint, uint>> bFirstMoveDict;
        private static Dictionary<int, Func<uint, uint>> bSecondMoveDict;
        private static Dictionary<int, Func<uint, uint>> bFirstCaptureDict;
        private static Dictionary<int, Func<uint, uint>> bSecondCaptureDict;

        static ShiftingFunctionsFactory()
        {
            fillDicts();
        }

        public static void GetShiftingFuncs(int i_BitPosition, eColor i_PieceColor, 
            out Func<uint, uint> o_SingleLeft, out Func<uint, uint> o_SingleRight, 
            out Func<uint, uint> o_DoubleLeft, out Func<uint, uint> o_DoubleRight)
        {
            if (i_PieceColor == eColor.White)
            {
                o_SingleLeft = wFirstMoveDict[i_BitPosition];
                o_SingleRight = wSecondMoveDict[i_BitPosition];
                o_DoubleLeft = wFirstCaptureDict[i_BitPosition];
                o_DoubleRight = wSecondCaptureDict[i_BitPosition];
            }
            else
            {
                o_SingleLeft = bFirstMoveDict[i_BitPosition];
                o_SingleRight = bSecondMoveDict[i_BitPosition];
                o_DoubleLeft = bFirstCaptureDict[i_BitPosition];
                o_DoubleRight = bSecondCaptureDict[i_BitPosition];
            }
        }

        private static void fillDicts()
        {
            // fill white move dict
            fillwFirstMove();
            fillwSecondMove();
            fillwFirstCapture();
            fillwSecondCapture();

            // fill black move dict
            fillbFirstMove();
            fillbSecondMove();
            fillbFirstCapture();
            fillbSecondCapture();
        }
        //move left
        private static void fillbFirstMove()
        {
            bFirstMoveDict = new Dictionary<int, Func<uint, uint>>
            {
                { 0, shiftLeftBy(5) },
                { 1, shiftLeftBy(5) },
                { 2, shiftLeftBy(5) },
                { 3, null },
                { 4, shiftLeftBy(4) },
                { 5, shiftLeftBy(4) },
                { 6, shiftLeftBy(4) },
                { 7, shiftLeftBy(4) }
            };
        }
        //move right
        private static void fillbSecondMove()
        {
            bSecondMoveDict = new Dictionary<int, Func<uint, uint>>
            {
                { 0, shiftLeftBy(4) },
                { 1, shiftLeftBy(4) },
                { 2, shiftLeftBy(4) },
                { 3, shiftLeftBy(4) },
                { 4, null },
                { 5, shiftLeftBy(3) },
                { 6, shiftLeftBy(3) },
                { 7, shiftLeftBy(3) }
            };
        }
        //capture left
        private static void fillbFirstCapture()
        {
            bFirstCaptureDict = new Dictionary<int, Func<uint, uint>>
            {
                { 0, shiftLeftBy(9) },
                { 1, shiftLeftBy(9) },
                { 2, shiftLeftBy(9) },
                { 3, null },
                { 4, shiftLeftBy(9) },
                { 5, shiftLeftBy(9) },
                { 6, shiftLeftBy(9) },
                { 7, null }
            };
        }
        // capture right
        private static void fillbSecondCapture()
        {
            bSecondCaptureDict = new Dictionary<int, Func<uint, uint>>
            {
                { 0, null },
                { 1, shiftLeftBy(7) },
                { 2, shiftLeftBy(7) },
                { 3, shiftLeftBy(7) },
                { 4, null },
                { 5, shiftLeftBy(7) },
                { 6, shiftLeftBy(7) },
                { 7, shiftLeftBy(7) }
            };
        }
        // move left
        private static void fillwFirstMove()
        {
            wFirstMoveDict = new Dictionary<int, Func<uint, uint>>
            {
                { 0, shiftRightBy(3) },
                { 1, shiftRightBy(3) },
                { 2, shiftRightBy(3) },
                { 3, null },
                { 4, shiftRightBy(4) },
                { 5, shiftRightBy(4) },
                { 6, shiftRightBy(4) },
                { 7, shiftRightBy(4) }
            };
        }
        //move right
        private static void fillwSecondMove()
        {
            wSecondMoveDict = new Dictionary<int, Func<uint, uint>>
            {
                { 0, shiftRightBy(4) },
                { 1, shiftRightBy(4) },
                { 2, shiftRightBy(4) },
                { 3, shiftRightBy(4) },
                { 4, null },
                { 5, shiftRightBy(5) },
                { 6, shiftRightBy(5) },
                { 7, shiftRightBy(5) }
            };
        }
        //capture left
        private static void fillwFirstCapture()
        {
            wFirstCaptureDict = new Dictionary<int, Func<uint, uint>>
            {
                { 0, shiftRightBy(7) },
                { 1, shiftRightBy(7) },
                { 2, shiftRightBy(7) },
                { 3, null },
                { 4, shiftRightBy(7) },
                { 5, shiftRightBy(7) },
                { 6, shiftRightBy(7) },
                { 7, null }
            };
        }
        // capture right
        private static void fillwSecondCapture()
        {
            wSecondCaptureDict = new Dictionary<int, Func<uint, uint>>
            {
                { 0, null },
                { 1, shiftRightBy(9) },
                { 2, shiftRightBy(9) },
                { 3, shiftRightBy(9) },
                { 4, null },
                { 5, shiftRightBy(9) },
                { 6, shiftRightBy(9) },
                { 7, shiftRightBy(9) }
            };
        }

        private static Func<uint, uint> shiftLeftBy(int amount)
        {
            return (number) => { return number << amount; };
        }

        private static Func<uint, uint> shiftRightBy(int amount)
        {
            return (number) => { return number >> amount; };
        }

        public static IEnumerable<IEnumerable<uint>> GetQueenIterators(uint i_Piece, uint i_OpposingPieces)
        {
            yield return FirstLeftIterations(i_Piece, i_OpposingPieces);
            yield return SecondLeftIterations(i_Piece, i_OpposingPieces);
            yield return FirstRightIterations(i_Piece, i_OpposingPieces);
            yield return SecondRightIterations(i_Piece, i_OpposingPieces);
        }

        private static IEnumerable<uint> FirstLeftIterations(uint piece, uint i_OpposingPieces)
        {
            //int moveLimit = GameMasterSingleton.Instance.QueenMoveLimit;
            int moveLimit = 1;
            Func<uint, uint> shiftingFunc;
            while (moveLimit > 0)
            {  
                shiftingFunc = wFirstMoveDict[BitUtils.FindBitPosition(piece) % 8];
                if (shiftingFunc != null)
                {
                    piece = shiftingFunc(piece);
                    // check if reached a captureable piece, if so allow for an extra iteration
                    moveLimit -= (i_OpposingPieces & piece) == 0 ? 1 : 0;
                    yield return piece;
                }
                else
                {
                    break;
                }
            }
        }

        private static IEnumerable<uint> SecondLeftIterations(uint piece, uint i_OpposingPieces)
        {
            //int moveLimit = GameMasterSingleton.Instance.QueenMoveLimit;
            int moveLimit = 1;
            Func<uint, uint> shiftingFunc;
            while (moveLimit > 0)
            {
                shiftingFunc = bFirstMoveDict[BitUtils.FindBitPosition(piece) % 8];
                if (shiftingFunc != null)
                {
                    piece = shiftingFunc(piece);
                    // check if reached a captureable piece, if so allow for an extra iteration
                    moveLimit -= (i_OpposingPieces & piece) == 0 ? 1 : 0;
                    yield return piece;
                }
                else
                {
                    break;
                }
            }
        }
        private static IEnumerable<uint> FirstRightIterations(uint piece, uint i_OpposingPieces)
        {
            //int moveLimit = GameMasterSingleton.Instance.QueenMoveLimit;
            int moveLimit = 1;
            Func<uint, uint> shiftingFunc;
            while (moveLimit > 0)
            {
                shiftingFunc = wSecondMoveDict[BitUtils.FindBitPosition(piece) % 8];
                if (shiftingFunc != null)
                {
                    piece = shiftingFunc(piece);
                    // check if reached a captureable piece, if so allow for an extra iteration
                    moveLimit -= (i_OpposingPieces & piece) == 0 ? 1 : 0;
                    yield return piece;
                }
                else
                {
                    break;
                }
            }
        }
        private static IEnumerable<uint> SecondRightIterations(uint piece, uint i_OpposingPieces)
        {
            //int moveLimit = GameMasterSingleton.Instance.QueenMoveLimit;
            int moveLimit = 1;
            Func<uint, uint> shiftingFunc;
            while (moveLimit > 0)
            {
                shiftingFunc = bSecondMoveDict[BitUtils.FindBitPosition(piece) % 8];
                if (shiftingFunc != null)
                {
                    piece = shiftingFunc(piece);
                    // check if reached a captureable piece, if so allow for an extra iteration
                    moveLimit -= (i_OpposingPieces & piece) == 0 ? 1 : 0;
                    yield return piece;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
