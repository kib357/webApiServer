using System;

namespace BacNetTypes.Primitive
{
    public class BacNetDouble
    {
        public double Value { get; set; }

        public BacNetDouble()
        {}

        public BacNetDouble(byte[] apdu, int startIndex, int length, ref int len)
        {
            byte[] value = new byte[length];
            for (int i = 0; i < length; i++)
            {
                value[i] = apdu[startIndex + length - 1 - i];
            }
            Value = BitConverter.ToDouble(value, 0);
            len += 8;
        }

        public byte[] GetBytes()
        {
            byte[] res = BitConverter.GetBytes(Value);
            for (int i = 0; i < res.Length / 2; i++)
            {
                byte tmp = res[i];
                res[i] = res[res.Length - 1 - i];
                res[res.Length - 1 - i] = tmp;
            }
            return res;
        }
    }
}
