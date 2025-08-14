using System.Collections.Generic;

namespace CheckersEngine
{
    internal class MoveAdapter : IMove
    {
        public MoveListProxy<IMove>? DoubleCapturesList { get; set; }
        private CheckersMove m_Move;
        public MoveAdapter(CheckersMove i_Move) 
        {
            m_Move = i_Move;
        }

        public MoveAdapter(MoveAdapter moveToWrap, IMove doubleCapture)
        {
            m_Move = moveToWrap.m_Move;
            DoubleCapturesList = new MoveListProxy<IMove> { doubleCapture };
        }

        public uint StartSquare 
        {
            get
            {
               return BitUtils.BitPositionToUInt((int)(m_Move.MoveValue & 0b11111));
            }
        }

        public uint TargetSquare
        {
            get
            {
                return BitUtils.BitPositionToUInt((int)(m_Move.MoveValue >> 5) & 0b11111);
            }
        }

        public uint CaptureSquare
        {
            get
            {
                return BitUtils.BitPositionToUInt((int)(m_Move.MoveValue >> 10) & 0b11111);
            }
        }

        public bool IsCapture()
        {
            bool isCapture = (m_Move.MoveValue & 0b0111110000000000) != 0;
            return isCapture;
        }

        public bool IsPromotion()
        {
            return m_Move.IsPromotion();
        }

        public bool IsDoubleCapture()
        {
            return this.DoubleCapturesList?.Count > 0 && this.DoubleCapturesList.First() is not null;
        }

        public int MovingPieceValue
        {
            get
            {
                return (int)m_Move.GetMovingPieceType() - 1;
            }
        }

        public int CapturedPieceValue
        {
            get
            {
                return (int)m_Move.GetCapturedPieceType() - 1;
            }
        }

        public void addDoubleCapture(IMove i_Move)
        {
            if (DoubleCapturesList is null)
            {
                DoubleCapturesList = new MoveListProxy<IMove>();
            }

            DoubleCapturesList.Add(i_Move);
        }

        public uint MoveValue
        {
            get
            {
                return m_Move.MoveValue;
            }
        }

        public override string ToString()
        {
            return m_Move.ToString() + DoubleCapturesList?.ToString();
        }
    }
}