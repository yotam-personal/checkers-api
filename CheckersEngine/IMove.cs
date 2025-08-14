using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    public interface IMove
    {
        // change in the future to properties
        MoveListProxy<IMove> DoubleCapturesList { get; set; }
        uint MoveValue { get; }
        void addDoubleCapture(IMove i_Move);
        int MovingPieceValue { get; }
        int CapturedPieceValue { get; }
        uint TargetSquare { get; }
        uint StartSquare { get; }
        uint CaptureSquare { get; }
        bool IsCapture();
        bool IsPromotion();
        bool IsDoubleCapture();
    }
}
