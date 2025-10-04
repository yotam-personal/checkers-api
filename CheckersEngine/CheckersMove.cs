using System;
using System.Runtime.CompilerServices;
using System.Xml;

namespace CheckersEngine
{
    internal readonly struct CheckersMove
    {
        // Move data packed into a 32 bit value
        // the format is as follow FFFFFFFFFFFFFFFFPPCCCCCTTTTTSSSSS
        // F = unused flag, P = PieceValue, C = Capture, T = Target, S = Start
        private readonly uint m_MoveValue;
        public uint MoveValue { get { return m_MoveValue; } }
        //flags
        public const byte NoCapture = 0b0000000;
        public const byte BlackSolider = 0b00000000;
        public const byte WhiteSolider = 0b00000001;
        public const byte BlackQueen = 0b00000010;
        public const byte WhiteQueen = 0b00000011;
        //Create Move
        public CheckersMove(uint i_StartSquare, uint i_TargetSquare, int i_PieceFlag, uint i_CaptureSquare = NoCapture, int i_CapturedPieceFlag = NoCapture)
        {
            //32 legal playing squares => all these int values are 5 bit values representing a valid playing square
            int startSquare = BitUtils.FindBitPosition(i_StartSquare); // 5 bit value
            int targetSquare = BitUtils.FindBitPosition(i_TargetSquare); // 5 bit value
            int captureSquare = i_CaptureSquare == NoCapture ? 0 : BitUtils.FindBitPosition(i_CaptureSquare); // 5 bit value

            m_MoveValue = (uint)(startSquare | targetSquare << 5 | captureSquare << 10 | i_PieceFlag << 15 | i_CapturedPieceFlag << 17);
        }

        private uint targetSquare => BitUtils.BitPositionToUInt((int)((m_MoveValue >> 5) & 0b11111));

        internal ePiece GetMovingPieceType()
        {
            int pieceValue = (int)((m_MoveValue >> 15) & 0b0011); //2 bit value
            return (ePiece)Enum.ToObject(typeof(ePiece), pieceValue + 1);
        }

        internal ePiece GetCapturedPieceType()
        {
            int pieceValue = (int)m_MoveValue >> 17; //2 bit value
            return (ePiece)Enum.ToObject(typeof(ePiece), pieceValue + 1);
        }

        public bool IsPromotion()
        {
            // moved to last row of piece type and moved a solider
            bool blackPromotion = (targetSquare & 0xF0000000) != 0 && GetMovingPieceType() == ePiece.sBlack;
            bool whitePromotion = (targetSquare & 0x0000000F) != 0 && GetMovingPieceType() == ePiece.sWhite;

            return whitePromotion || blackPromotion;
        }

        private int StartSquare => (int)(m_MoveValue & 0b11111);
        private int TargetSquare => (int)((m_MoveValue >> 5) & 0b11111);

        public override string ToString()
        {
            string move = $"{StartSquare}-{TargetSquare}";
            return move;
        }
    }
}