using System.Diagnostics;
using System.Runtime.Intrinsics.X86;

namespace CheckersEngine
{
    public class CheckersComputer : ICheckersComputer
    {
        private Func<uint[], float> m_EvaluationStrategyMethod;
        private uint[] m_BitBoards;
        private IMoveGenerator m_MoveGenerator = new MoveGenerator();
        private TranspositionTable m_TT;
        private eColor m_MovingColor;
        private ComputerMoveComparer m_MoveComparer;
        private EvaluationNodeComparer m_Comparer = new EvaluationNodeComparer();
        private CheckersComputer()
        {
        }

        public static CheckersComputer GetInstance(GameObject i_GameObject)
        {
            CheckersComputer computer = new CheckersComputer();
            computer.m_MovingColor = i_GameObject.Turn;
            computer.m_TT = i_GameObject.TranspositionTable;
            computer.m_BitBoards = i_GameObject.BoardState;
            computer.m_TT.BitBoards = computer.m_BitBoards; 
            computer.m_EvaluationStrategyMethod = EvaluationMethodsFactory.GetEvaluationStrategy(EvaluationType.Advanced);
            computer.m_MoveComparer = new ComputerMoveComparer(i_GameObject.Turn);

            return computer;
        }

        ComputerMove ICheckersComputer.GenerateMove()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<ComputerMove> moveList = new List<ComputerMove>();
            List<IMove> legalMoves = m_MoveGenerator.GiveLegalMoves(m_BitBoards, m_MovingColor);
            ComputerMove topMove = null;
            bool stopFlag = false;
   
            for (int depth = 1; depth <= 100; depth++)
            {
                moveList.Clear();
                foreach (IMove move in legalMoves)
                {
                    if (stopwatch.ElapsedMilliseconds > 500 && topMove is not null)
                    {
                        stopFlag = true;
                        break;
                    }

                    CheckersBitBoardHelper.MakeMove(move, m_BitBoards);
                    foreach (IMove _move in CheckersBitBoardHelper.GeneratePositionAfterDoubleCaptures(move, m_BitBoards, true))
                    {
                        EvaluationNode currentMoveEvalNode = MiniMax(depth - 1, swapTurn(m_MovingColor));
                        move.DoubleCapturesList = [_move];
                        moveList.Add(new ComputerMove(move, currentMoveEvalNode));
                    }

                    CheckersBitBoardHelper.unMakeMove(move, m_BitBoards);
                }
                
                // sort based on the move comparer
                moveList = moveList.OrderByDescending(item => item, m_MoveComparer).ToList();
                legalMoves = moveList.Select(move => move.Move).ToList();
                // conditional in case of timelapse break
                topMove = moveList.Count > 0 ? moveList[0] : topMove;
                stopFlag = stopFlag || topMove.EvaluationNode.evaluation == float.MaxValue || topMove.EvaluationNode.evaluation == float.MinValue;
                if (stopFlag)
                {
                    break;
                }
            }

            return topMove;
        }

        private EvaluationNode MiniMax(int i_Depth, eColor colorTurn, int numExtensions = 0, float alpha = float.MinValue, float beta = float.MaxValue, int pathLength = 0)
        {
            pathLength++;
            m_Comparer.EvaluatingColor = colorTurn;
            EvaluationNode bestEvaluation = new EvaluationNode() { evaluation = m_Comparer.GetLostPositionEvaluation(), PathLength = -1 };
            if (checkForTTExit(ref bestEvaluation, i_Depth, colorTurn))
            {
                return bestEvaluation!;
            }    

            List<IMove> moves = m_MoveGenerator.GiveLegalMoves(m_BitBoards, colorTurn);
            if (moves.Count == 0)
            {
                bestEvaluation.PathLength = 0;
                return bestEvaluation!;
            }

            if (i_Depth == 0)
            {
                // quiecent search i.e. don't stop search until a quiet position
                if (moves.Exists(move => move.IsCapture()))
                {
                    i_Depth++;
                }
                else
                {
                    float evaluation = m_EvaluationStrategyMethod(m_BitBoards);
                    bestEvaluation = new EvaluationNode() { evaluation = evaluation, depth = 0, PathLength = pathLength };
                    m_TT.StoreEvaluation(i_Depth, bestEvaluation, colorTurn, m_Comparer);
                    return bestEvaluation;
                }
            }

            for (int i = 0; i < moves.Count; i++)
            {
                int extension = isExtensionMove(moves[i]) && numExtensions <= 4 ? 1 : 0;
                CheckersBitBoardHelper.MakeMove(moves[i], m_BitBoards);
                foreach (IMove _ in CheckersBitBoardHelper.GeneratePositionAfterDoubleCaptures(moves[i], this.m_BitBoards))
                {
                    EvaluationNode currentEvaluation = MiniMax(i_Depth - 1 + extension, this.swapTurn(colorTurn), numExtensions + extension, alpha, beta, pathLength);
                    if (i == 0 && bestEvaluation.PathLength == -1)
                    {
                        bestEvaluation = currentEvaluation;
                    }
                    else
                    {
                        m_Comparer.EvaluatingColor = colorTurn;
                        bestEvaluation = m_Comparer.Compare(bestEvaluation, currentEvaluation) == 1 ? bestEvaluation : currentEvaluation;
                    }

                    if (colorTurn == eColor.White)
                    {
                        alpha = Math.Max(alpha, bestEvaluation.evaluation);
                    }
                    else
                    {
                        beta = Math.Min(beta, bestEvaluation.evaluation);
                    }
                }

                CheckersBitBoardHelper.unMakeMove(moves[i], m_BitBoards);
                if (beta <= alpha && bestEvaluation.evaluation != float.MaxValue && bestEvaluation.evaluation != float.MinValue)
                {
                    break; // Beta cut-off
                }
            }

            if (bestEvaluation.evaluation == float.MaxValue || bestEvaluation.evaluation == float.MinValue)
            {
                bestEvaluation = bestEvaluation.Clone() as EvaluationNode;
            }

            m_TT.StoreEvaluation(i_Depth, bestEvaluation!, colorTurn, m_Comparer);
            return bestEvaluation!;
        }

        private bool isExtensionMove(IMove move)
        {
            bool extension = false;
            if (move.IsCapture() || move.IsPromotion())
            {
                extension = true;
            }

            return extension;
        }

        private bool checkForTTExit(ref EvaluationNode node, int depth, eColor colorTurn)
        {
            return m_TT.LookupEvaluation(depth, ref node, colorTurn);
        }

        private eColor swapTurn(eColor i_Color)
        {
            return (i_Color == eColor.Black) ? eColor.White : eColor.Black;
        }
    }

    public class EvaluationNode : ICloneable
    {
        public float evaluation { get; set; }
        public int depth { get; set; }
        public int PathLength { get; set; }

        // clone on win to calc path length
        public object Clone()
        {
            return new EvaluationNode
            {
                evaluation = this.evaluation,
                depth = this.depth,
                PathLength = this.PathLength + 1
            };
        }
    }
}
