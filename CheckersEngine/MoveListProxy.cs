using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    public class MoveListProxy<IMove> : List<IMove>
    {
        public MoveListProxy()
        {
                
        }

        public MoveListProxy(MoveListProxy<IMove> moveList) : base(moveList)
        {

        }

        public override string ToString()
        {
            string moveChain = string.Empty;

            foreach (IMove move in this) 
            {
                if (move is not null)
                {
                    moveChain += $" {move?.ToString()}";
                }
            }

            return moveChain;
        }
    }
}
