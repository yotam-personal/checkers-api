using CheckersEngine;

namespace ChekersAPI
{
    public class MoveResponseAgent
    {
        private readonly GameObject m_GameObject;
        private MoveResponseAgent(GameObject gameObject)
        {
            m_GameObject = gameObject;
        }
        public static MoveResponseAgent GetInstance(CheckersMoveRequest i_Request)
        {
            return new MoveResponseAgent(ValidationUtils.ValidateAndParseRequest(i_Request));
        }
        public async Task<CheckersMoveReply> GenerateMoveResponse()
        {
            return await Task.Run(() =>
            {
                ICheckersComputer computer = CheckersComputer.GetInstance(this.m_GameObject);
                ComputerMove engineMove = computer.GenerateMove();
                this.m_GameObject.PlayMove(engineMove);
                return new CheckersMoveReply { Move = engineMove.Move.ToString(), Evaluation = engineMove.EvaluationNode.evaluation, PathLength = engineMove.EvaluationNode.PathLength };
            });
        }
    }
}
