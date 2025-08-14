using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    public class GameObject
    {
        private MoveGenerator moveGenerator;
        public uint[] BoardState { get; set; }
        private List<ComputerMove> moveList;
        private List<IMove> legalMoves;
        public uint[] StartingPosition { get; init; }
        internal TranspositionTable TranspositionTable { get; set; }
        public eColor Turn { get; set; }
        private eColor PlayingAs { get; init; }

        // constructor starting the game from custom position
        public GameObject(GameObjectRequest i_Request)
        {
            this.Turn = i_Request.TurnColor;
            this.PlayingAs = i_Request.ComputerColor;
            this.BoardState = i_Request.BoardState;
            this.StartingPosition = new uint[i_Request.BoardState.Length];
            // make a deep copy of the BitBoards so it has a different memory address
            Array.Copy(i_Request.BoardState, this.StartingPosition, i_Request.BoardState.Length); 
            this.moveList = new List<ComputerMove>();
            this.moveGenerator = new MoveGenerator();
            this.legalMoves = this.moveGenerator.GiveLegalMoves(this.BoardState, this.Turn);
            this.TranspositionTable = new TranspositionTable(64);
        }

        public void swapTurn()
        {
            this.Turn = this.Turn == eColor.Black ? eColor.White : eColor.Black;
        }

        public void ValidateAndChangeBoardState(uint[] newBoardState)
        {
            if (moveList.Count == 0 && this.equalStates(newBoardState))
            {
                return;
            }

            bool validResult = false;
            foreach (IMove move in legalMoves) 
            {
                CheckersBitBoardHelper.MakeMove(move, this.BoardState);
                foreach (IMove _move in CheckersBitBoardHelper.GeneratePositionAfterDoubleCaptures(move, this.BoardState, true))
                {
                    if (equalStates(newBoardState))
                    {
                        validResult = true;
                        // encapsulate the move sequence
                        move.DoubleCapturesList = [_move];
                        ComputerMove madeMove = new ComputerMove(move);
                        moveList.Add(madeMove);
                        swapTurn();
                        break;
                    }
                }

                if (!validResult)
                {
                    CheckersBitBoardHelper.unMakeMove(move, this.BoardState);
                }
                else
                {
                    break;
                }
            }

            validResult = validResult && this.Turn == PlayingAs;
            if (!validResult)
            {
                throw new ArgumentException("invalid request to the server");
            }
            else if (moveList.Count >= 2)
            {
                EvaluationNode prevMoveEvalNode = moveList[moveList.Count - 2].EvaluationNode;
                moveList[moveList.Count - 1].EvaluationNode.evaluation = prevMoveEvalNode.evaluation;
                moveList[moveList.Count - 1].EvaluationNode.PathLength = prevMoveEvalNode.PathLength - 1;

            }
        }

        public void PlayMove(ComputerMove move)
        {
            if (moveList.Count > 0)
            {
                this.moveList.Last().EvaluationNode.evaluation = move.EvaluationNode.evaluation;
                this.moveList.Last().EvaluationNode.PathLength = move.EvaluationNode.PathLength + 1;
            }

            this.moveList.Add(move);
            CheckersBitBoardHelper.MakeMoveSequence(move.Move, this.BoardState);
            swapTurn();
            this.legalMoves = this.moveGenerator.GiveLegalMoves(this.BoardState, this.Turn);
        }

        private bool equalStates(uint[] i_BitBoards)
        {
            bool blackSoliderMatch = this.BoardState[(int)ePiece.sBlack - 1] == i_BitBoards[(int)ePiece.sBlack - 1];
            bool whiteSoliderMatch = this.BoardState[(int)ePiece.sWhite - 1] == i_BitBoards[(int)ePiece.sWhite - 1];
            bool blackQueenMatch = this.BoardState[(int)ePiece.qBlack - 1] == i_BitBoards[(int)ePiece.qBlack - 1];
            bool whiteQueenMatch = this.BoardState[(int)ePiece.qWhite - 1] == i_BitBoards[(int)ePiece.qWhite - 1];

            return blackSoliderMatch && whiteSoliderMatch && blackQueenMatch && whiteQueenMatch;
        }

        public bool IsComputerLoss()
        {
            this.legalMoves = this.moveGenerator.GiveLegalMoves(this.BoardState, this.Turn);
            bool computerLoss = this.Turn == this.PlayingAs && this.legalMoves.Count == 0;

            return computerLoss;
        }

        public string[] GetGameSequence()
        {
            List<string> gameSequence = [CheckersBitBoardHelper.GeneratePositionFromBitBoards(this.StartingPosition)];
            foreach (ComputerMove move in moveList)
            {
                gameSequence.Add(move.ToString());
            }

            return gameSequence.ToArray();
        }
    }
}
