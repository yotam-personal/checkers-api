using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CheckersEngine
{
    public class ComputerMove
    {
        public IMove Move { get; set; }

        public EvaluationNode EvaluationNode { get; set; }
        public ComputerMove(IMove i_Move, EvaluationNode node) 
        {
            Move = i_Move;
            EvaluationNode = node;
        }

        public ComputerMove(IMove i_Move)
        {
            Move = i_Move;
            EvaluationNode = new EvaluationNode();
        }

        public ComputerMove()
        {
                
        }

        public override string ToString()
        {
            return $"Move:{Move} | Evaluation:{EvaluationNode.evaluation} | Depth:{EvaluationNode.PathLength}";
        }
    }
}
