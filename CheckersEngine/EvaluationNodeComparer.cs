using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    internal class EvaluationNodeComparer : IComparer<EvaluationNode>
    {
        public eColor EvaluatingColor { get; set; }      
        public int Compare(EvaluationNode? x, EvaluationNode? y)
        {
            if (y is null)
            {
                return 1;
            }

            int result = 0;
            // Check if one of the evaluations is a definitive win or loss
            if (x.evaluation == float.MaxValue && y.evaluation == float.MaxValue)
            {
                // winning player chooses the quick win, losing player chooses slow loss
                result = (EvaluatingColor == eColor.White)
                    ? (x.PathLength <= y.PathLength ? 1 : -1)
                    : (x.PathLength > y.PathLength ? 1 : -1);
            }
            else if (x.evaluation == float.MinValue && y.evaluation == float.MinValue)
            {
                // If black is evaluating choose the faster win, else choose the slower loss
                result = (EvaluatingColor == eColor.Black)
                    ? (x.PathLength <= y.PathLength ? 1 : -1)
                    : (x.PathLength > y.PathLength ? 1 : -1);
            }
            else if (EvaluatingColor == eColor.White)
            {
                result = x.evaluation >= y.evaluation ? 1 : -1;
            }
            else if (EvaluatingColor == eColor.Black)
            {
                result = x.evaluation <= y.evaluation ? 1 : -1;
            }

            return result;
        }

        public float GetLostPositionEvaluation()
        {
            return (EvaluatingColor == eColor.White) ? float.MinValue : float.MaxValue;
        }
    }
}
