using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    // this class is used as an object for the mini-max algorithm, hence the dictionaries.
    internal class MoveGenerator : IMoveGenerator
    {
        
        public List<IMove> GiveLegalMoves(uint[] i_BitBoards, eColor i_ColorToMove)
        {
            List<IMove> legalMoves;           
            legalMoves = generateMoves(i_BitBoards, i_ColorToMove);                      
            return legalMoves!;
        }

        private List<IMove> generateMoves(uint[] i_BitBoards, eColor i_Color)
        {
            List<IMove> movesList = new List<IMove>();
            bool isCaptureTurn = false; // Force Captures, if capture found list is cleared and isCaptureTurn becomes true
            Func<uint, uint> moveLeft, moveRight, captureLeft, captureRight;
            uint friendlyPieces, opposingPieces, soliders;
            allocateBitBoards(i_Color, i_BitBoards, out friendlyPieces, out opposingPieces, out soliders, out _);
            ePiece movingSolider = getSoliderPiece(i_Color);
            foreach (uint soliderStartSquare in BitUtils.GetSetBits(soliders))
            {
                int piecePosition = BitUtils.FindBitPosition(soliderStartSquare) % 8;
                ShiftingFunctionsFactory.GetShiftingFuncs(piecePosition, i_Color, out moveLeft, out moveRight,
                    out captureLeft, out captureRight);
                // left capture check
                if (captureLeft != null && captureCheck(soliderStartSquare, opposingPieces, friendlyPieces, moveLeft, captureLeft))
                {
                    if (!isCaptureTurn)
                    {
                        isCaptureTurn = doCaptureSeenDuringTurn(movesList);
                    }

                    IMove moveToAdd = makeMoveAfterCapture(i_Color, i_BitBoards, soliderStartSquare, moveLeft(soliderStartSquare),
                                                           captureLeft(soliderStartSquare), movingSolider, i_Color);
                    movesList.Add(moveToAdd);
                }
                // right capture check
                if (captureRight != null && captureCheck(soliderStartSquare, opposingPieces, friendlyPieces, moveRight, captureRight))
                {
                    if (!isCaptureTurn)
                    {
                        isCaptureTurn = doCaptureSeenDuringTurn(movesList);
                    }

                    IMove moveToAdd = makeMoveAfterCapture(i_Color, i_BitBoards, soliderStartSquare, moveRight(soliderStartSquare),
                                                           captureRight(soliderStartSquare), movingSolider, i_Color);
                    movesList.Add(moveToAdd);
                }

                // normal move left
                if (!isCaptureTurn && moveLeft != null && normalMoveCheck(soliderStartSquare, friendlyPieces | opposingPieces, moveLeft))
                {
                    movesList.Add(PackMove(soliderStartSquare, moveLeft(soliderStartSquare), movingSolider));
                }
                // normal move right
                if (!isCaptureTurn && moveRight != null && normalMoveCheck(soliderStartSquare, friendlyPieces | opposingPieces, moveRight))
                {
                    movesList.Add(PackMove(soliderStartSquare, moveRight(soliderStartSquare), movingSolider));
                }
            }

            queenMoves(i_BitBoards, isCaptureTurn, movesList, i_Color);
            return movesList;
        }

        private void queenMoves(uint[] i_BitBoards, bool i_IsCaptureTurn, List<IMove> i_Moves, eColor i_ColorTurn)
        {
            uint queens, friendly, opposing;
            allocateBitBoards(i_ColorTurn, i_BitBoards, out friendly, out opposing, out _, out queens);
            ePiece movingQueen = getQueenPiece(i_ColorTurn);
            foreach (uint queenStartSquare in BitUtils.GetSetBits(queens))
            {
                foreach (IEnumerable<uint> queenMovesIterator in ShiftingFunctionsFactory.GetQueenIterators(queenStartSquare, opposing))
                {
                    uint captureablePiece = 0;
                    foreach (uint queenTargetSquare in queenMovesIterator)
                    {
                        if (queenTargetSquare == 0)
                        {
                            break;
                        }

                        if (((friendly | opposing) & queenTargetSquare) == 0) // empty square check
                        {
                            if (captureablePiece != 0)
                            {
                                if (!i_IsCaptureTurn) // clear all none capture moves
                                {
                                    i_IsCaptureTurn = doCaptureSeenDuringTurn(i_Moves);
                                }

                                IMove moveToAdd = makeMoveAfterCapture(i_ColorTurn, i_BitBoards, queenStartSquare, captureablePiece, queenTargetSquare, movingQueen, i_ColorTurn);
                                i_Moves.Add(moveToAdd);
                                break;
                            }
                            else if (!i_IsCaptureTurn)
                            {
                                i_Moves.Add(PackMove(queenStartSquare, queenTargetSquare, movingQueen));
                            }
                        }
                        else if ((opposing & queenTargetSquare) != 0) //enemy piece seen
                        {
                            if (captureablePiece != 0) // 2 black pieces in a row seen
                            {
                                break;
                            }

                            captureablePiece = queenTargetSquare;
                        }
                        else //reached a friendly piece
                        {
                            break;
                        }
                    }
                }
            }
        }

        private bool normalMoveCheck(uint i_Piece, uint i_Board, Func<uint, uint> i_ShiftingFunc)
        {
            bool isLegalMove = false;
            bool outOfBounds = i_ShiftingFunc(i_Piece) == 0;
            if (!outOfBounds && !isPieceInTheWay(i_Piece, i_Board, i_ShiftingFunc))
            {
                isLegalMove = true;
            }

            return isLegalMove;
        }

        private bool captureCheck(uint i_Piece, uint i_OpposingPieces, uint i_SamePieces, Func<uint, uint> i_ShiftingFunc,
            Func<uint, uint> i_2ndShiftingFunc)
        {
            bool isLegalMove = false;
            bool outOfBounds = i_ShiftingFunc(i_Piece) == 0 || i_2ndShiftingFunc(i_Piece) == 0;
            if (!outOfBounds && isPieceInTheWay(i_Piece, i_OpposingPieces, i_ShiftingFunc) &&
                !isPieceInTheWay(i_Piece, i_OpposingPieces | i_SamePieces, i_2ndShiftingFunc))
            {
                isLegalMove = true;
            }

            return isLegalMove;
        }

        private IMove makeMoveAfterCapture(eColor i_Color, uint[] i_BitBoards, uint startSquare, uint capturedSquare, uint targetSquare, ePiece movingPiece, eColor movingPieceColor)
        {
            ePiece capturedPiece = getCapturedPieceType(i_Color, i_BitBoards, capturedSquare);
            IMove moveToAdd = PackMove(startSquare, targetSquare, movingPiece, capturedSquare, capturedPiece);
            CheckersBitBoardHelper.MakeMove(moveToAdd, i_BitBoards);
            doubleCaptureCheck(moveToAdd, targetSquare, i_BitBoards, movingPieceColor);
            CheckersBitBoardHelper.unMakeMove(moveToAdd, i_BitBoards);

            return moveToAdd;
        }

        private void doubleCaptureCheck(IMove move, uint pieceLocation, uint[] i_BitBoards, eColor i_MovingPieceColor)
        {
            // checks front and back captures
            ePiece movingPiece = getPieceType(pieceLocation, i_BitBoards);
            uint friendly, opposing;
            allocateBitBoards(i_MovingPieceColor, i_BitBoards, out friendly, out opposing, out _, out _);
            Func<uint, uint> moveLeft, moveRight, captureLeft, captureRight;
            int bitPosition = BitUtils.FindBitPosition(pieceLocation) % 8;
            ShiftingFunctionsFactory.GetShiftingFuncs(bitPosition, eColor.White, out moveLeft, out moveRight, out captureLeft, out captureRight);
            if (captureLeft != null && captureCheck(pieceLocation, opposing, friendly, moveLeft, captureLeft))
            {
                IMove moveToAdd = makeMoveAfterCapture(i_MovingPieceColor, i_BitBoards, pieceLocation, moveLeft(pieceLocation), captureLeft(pieceLocation), movingPiece, i_MovingPieceColor);
                move.addDoubleCapture(moveToAdd);
            }

            if (captureRight != null && captureCheck(pieceLocation, opposing, friendly, moveRight, captureRight))
            {
                IMove moveToAdd = makeMoveAfterCapture(i_MovingPieceColor, i_BitBoards, pieceLocation, moveRight(pieceLocation), captureRight(pieceLocation), movingPiece, i_MovingPieceColor);
                move.addDoubleCapture(moveToAdd);
            }

            ShiftingFunctionsFactory.GetShiftingFuncs(bitPosition, eColor.Black, out moveLeft, out moveRight, out captureLeft, out captureRight);
            if (captureLeft != null && captureCheck(pieceLocation, opposing, friendly, moveLeft, captureLeft))
            {
                IMove moveToAdd = makeMoveAfterCapture(i_MovingPieceColor, i_BitBoards, pieceLocation, moveLeft(pieceLocation), captureLeft(pieceLocation), movingPiece, i_MovingPieceColor);
                move.addDoubleCapture(moveToAdd);
            }

            if (captureRight != null && captureCheck(pieceLocation, opposing, friendly, moveRight, captureRight))
            {
                IMove moveToAdd = makeMoveAfterCapture(i_MovingPieceColor, i_BitBoards, pieceLocation, moveRight(pieceLocation), captureRight(pieceLocation), movingPiece, i_MovingPieceColor);
                move.addDoubleCapture(moveToAdd);
            }
        }

        private ePiece getPieceType(uint pieceLocation, uint[] i_BitBoards)
        {
            ePiece movingPiece;
            if ((pieceLocation & i_BitBoards[0]) != 0)
            {
                movingPiece = ePiece.sBlack;
            }
            else if ((pieceLocation & i_BitBoards[1]) != 0)
            {
                movingPiece = ePiece.sWhite;
            }
            else if ((pieceLocation & i_BitBoards[2]) != 0)
            {
                movingPiece = ePiece.qBlack;
            }
            else
            {
                movingPiece = ePiece.qWhite;
            }

            return movingPiece;
        }

        private bool isPieceInTheWay(uint piece, uint allColorPieces, Func<uint, uint> i_Shift)
        {
            return (i_Shift(piece) & allColorPieces) != 0;
        }

        private ePiece getSoliderPiece(eColor i_Color)
        {
            if (i_Color == eColor.White)
            {
                return ePiece.sWhite;
            }
            else
            {
                return ePiece.sBlack;
            }
        }

        private ePiece getQueenPiece(eColor i_Color)
        {
            if (i_Color == eColor.White)
            {
                return ePiece.qWhite;
            }
            else
            {
                return ePiece.qBlack;
            }
        }

        private bool doCaptureSeenDuringTurn(List<IMove> movesList)
        {
            movesList.Clear();
            return true;
        }

        private IMove PackMove(uint i_StartSquare, uint i_TargetSquare, ePiece i_MovingPiece, uint i_CaptureSquare = 0b00000, ePiece i_CapturedPiece = 0b00)
        {
            int PieceValue = (int)i_MovingPiece - 1;
            int CapturedPieceValue = i_CapturedPiece == ePiece.None ? 0 : (int)i_CapturedPiece - 1;
            return new MoveAdapter(new CheckersMove(i_StartSquare, i_TargetSquare, PieceValue, i_CaptureSquare, CapturedPieceValue));
        }

        private void allocateBitBoards(eColor i_Color, uint[] i_BitBoards, out uint o_Friendly,
            out uint o_Opposing, out uint o_Soliders, out uint o_Queens)
        {
            if (i_Color == eColor.Black)
            {
                o_Friendly = i_BitBoards[0] | i_BitBoards[2];
                o_Opposing = i_BitBoards[1] | i_BitBoards[3];
                o_Soliders = i_BitBoards[0];
                o_Queens = i_BitBoards[2];
            }
            else
            {
                o_Friendly = i_BitBoards[1] | i_BitBoards[3];
                o_Opposing = i_BitBoards[0] | i_BitBoards[2];
                o_Soliders = i_BitBoards[1];
                o_Queens = i_BitBoards[3];
            }
        }

        private ePiece getCapturedPieceType(eColor i_ColorTurn, uint[] i_BitBoards, uint captureablePiece)
        {
            ePiece capturedPieceType;
            if (i_ColorTurn == eColor.White)
            {
                capturedPieceType = (i_BitBoards[(int)ePiece.sBlack - 1] & captureablePiece) != 0 ? ePiece.sBlack : ePiece.qBlack;
            }
            else
            {
                capturedPieceType = (i_BitBoards[(int)ePiece.sWhite - 1] & captureablePiece) != 0 ? ePiece.sWhite : ePiece.qWhite;
            }

            return capturedPieceType;
        }
    }
}
