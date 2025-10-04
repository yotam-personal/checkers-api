using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    internal class TranspositionTable
    {
        public Entry[] whiteEntries;
        public Entry[] blackEntries;

        public readonly ulong count;
        public bool enabled = true;

        public uint[] BitBoards { get; set; }
        public TranspositionTable(int sizeMB)
        {
            int ttEntrySizeBytes = System.Runtime.CompilerServices.Unsafe.SizeOf<TranspositionTable.Entry>();
            int desiredTableSizeInBytes = sizeMB * 1024 * 1024;
            int numEntries = desiredTableSizeInBytes / ttEntrySizeBytes;

            count = (ulong)(numEntries);
            whiteEntries = new Entry[numEntries];
            blackEntries = new Entry[numEntries];
        }

        public ulong getIndex(out ulong hash)
        {
            hash = ZobristHashing.YotamHash(BitBoards);
            return  hash % count;       
        }


        public bool LookupEvaluation(int depth, ref EvaluationNode o_Node, eColor color)
        {
            if (!enabled)
            {
                return false;
            }

            ulong hash;
            Entry entry = color == eColor.White ? whiteEntries[getIndex(out hash)] : blackEntries[getIndex(out hash)];       
            if (entry.key == hash)
            {
                // Only use stored evaluation if it has been searched to at least the same depth as would be searched now or win
                if (entry.depth >= depth || entry.value.evaluation == float.MaxValue || entry.value.evaluation == float.MinValue)
                {
                    o_Node = entry.value;
                    return true;
                }
            }

            return false;
        }

        public void StoreEvaluation(int depth, EvaluationNode node, eColor colorTurn, EvaluationNodeComparer i_Comparer)
        {
            if (!enabled)
            {
                return;
            }

            bool insertFlag = false;
            ulong hash;
            ulong index = getIndex(out hash);
            Entry[] entries = colorTurn == eColor.White ? whiteEntries : blackEntries;
            Entry entry = entries[index];
            if (depth >= entry.depth && node.evaluation != float.MinValue && node.evaluation != float.MinValue)
            {
                insertFlag = true;
            }
            else
            {
                i_Comparer.EvaluatingColor = colorTurn;
                insertFlag = i_Comparer.Compare(node, entry.value) == 1 ? true : false;
            }

            if (insertFlag)
            {
                entries[index] = new Entry(hash ,node, (byte)depth);
            }
        }

        public struct Entry
        {

            public readonly ulong key;
            public readonly EvaluationNode value;
            public readonly byte depth;

            public Entry(ulong key, EvaluationNode value, byte depth)
            {
                this.key = key;
                this.value = value;
                this.depth = depth; // depth is how many ply were searched ahead from this position
            }
        }
    }
}
