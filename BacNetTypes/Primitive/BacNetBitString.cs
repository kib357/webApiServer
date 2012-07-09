using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BacNetTypes.Primitive
{
    public class BacNetBitString
    {
        public string Value { get; set; }
        public byte UnusedBits { get; set; }

        public BacNetBitString()
        {}

        public BacNetBitString(byte[] apdu, int startIndex, int length, ref int len)
        {
            UnusedBits = apdu[startIndex];
            for (int i = 0; i < length - 1; i++)
            {
                Value += Convert.ToString(apdu[startIndex + 1 + i], 2);
            }
            len += length;
        }

        public byte[] GetBytes()
        {
            var res = new ArrayList();
            res.Add(UnusedBits);
            res.AddRange(GetValueBytes(Value));
            return (byte[])res.ToArray(typeof(byte));
        }

        private static byte[] GetValueBytes(string bitString)
        {
            return Enumerable.Range(0, bitString.Length / 8).
                Select(pos => Convert.ToByte(bitString.Substring(pos * 8, 8),2)).ToArray();
        }
    }
}
