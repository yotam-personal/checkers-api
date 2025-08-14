using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    public record GameObjectRequest
    {
        public string Id { get; init; }
        public uint[] BoardState { get; init; }
        public eColor TurnColor { get; init; }
        public eColor ComputerColor { get; init; }
    }

}
