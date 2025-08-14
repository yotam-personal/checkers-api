using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    public static class ZobristHashing
    {
        private static readonly ulong[,] bitValues = new ulong[4, 32]; // 4 uint values, 32 bits each
        private static readonly ulong prime1 = (uint)(Math.Pow(2, 11) + 3);
        private static readonly ulong prime2 = (uint)(Math.Pow(2, 13) + 7);

        static ZobristHashing()
        {
            const int seed = 29426028; // Seed for reproducibility
            Random rng = new Random(seed);

            for (int valueIndex = 0; valueIndex < 4; valueIndex++)
            {
                for (int bitIndex = 0; bitIndex < 32; bitIndex++)
                {
                    bitValues[valueIndex, bitIndex] = RandomUnsigned64BitNumber(rng);
                }
            }
        }

        public static ulong YotamHash(uint[] i_BitBoards)
        {
            ulong hash = 0;

            // Combine bitboards using XOR with random values for better distribution
            for (int valueIndex = 0; valueIndex < 4; valueIndex++)
            {
                hash ^= i_BitBoards[valueIndex] * bitValues[valueIndex, 0]; // Use a random value for each bitboard
            }

            return hash;
        }


        public static ulong HashCode(uint[] i_BitBoards)
        {
            ulong hash = 0;

            for (int valueIndex = 0; valueIndex < 4; valueIndex++)
            {
                for (int bitIndex = 0; bitIndex < 32; bitIndex++)
                {
                    if ((i_BitBoards[valueIndex] & (1u << bitIndex)) != 0)
                    {
                        hash ^= bitValues[valueIndex, bitIndex];
                    }
                }
            }

            return hash;
        }

        private static ulong RandomUnsigned64BitNumber(Random rng)
        {
            byte[] buffer = new byte[8];
            rng.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }
    }

}
