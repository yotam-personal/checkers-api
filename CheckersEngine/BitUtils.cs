using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CheckersEngine
{
    internal static class BitUtils
    {
        internal static List<uint> GetSetBits(uint number)
        {
            List<uint> bitList = new List<uint>();

            for (int i = 0; i < 32; i++)
            {
                if ((number & (1 << i)) != 0)
                {
                    bitList.Add((uint)(0b01 << i));
                }
            }

            return bitList;
        }

        internal static int GetSetBitsAmount(uint number)
        {
            int setBitsAmount = 0;

            for (int i = 0; i < 32; i++)
            {
                if ((number & (1 << i)) != 0)
                {
                    setBitsAmount++;
                }
            }

            return setBitsAmount;
        }

        internal static uint BitPositionToUInt(int i_BitPosition)
        {
            return (uint)(0b01 << i_BitPosition);
        }

        internal static string ToBitString(uint value)
        {
            return Convert.ToString(value, 2).PadLeft(32, '0');
        }

        internal static int FindBitPosition(uint number)
        {
            if (number == 0)
            {
                return -1; // Indicate no bits are set
            }

            // BitOperations.TrailingZeroCount returns the number of trailing zeros in the number's binary representation
            // The position of the LSB is the count of trailing zeros + 1
            return BitOperations.TrailingZeroCount(number);
        }
    }
}
