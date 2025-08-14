using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    internal class ComputerMoveComparer : IComparer<ComputerMove>
    {
        private readonly EvaluationNodeComparer m_Comparer;
        public ComputerMoveComparer(eColor evaluatingColor)
        {
            m_Comparer = new EvaluationNodeComparer() { EvaluatingColor = evaluatingColor };
        }

        public int Compare(ComputerMove? x, ComputerMove? y)
        {
            return m_Comparer.Compare(x.EvaluationNode, y.EvaluationNode);
        }
    }
}
