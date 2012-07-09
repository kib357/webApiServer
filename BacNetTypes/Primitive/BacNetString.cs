using System.Collections;
using System.Text;

namespace BacNetTypes.Primitive
{
    public class BacNetString
    {
        private string Value { get; set; }

        private BacnetCharacterStringEncoding TextEncoding { get; set; }

        public BacNetString()
        {}

        public BacNetString(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }

        public BacNetString(byte[] apdu, int startIndex, int length, ref int len)
        {
            if (apdu[startIndex] == 0)
            {
                Value = Encoding.UTF8.GetString(apdu, startIndex + 1, length - 1);
                TextEncoding = BacnetCharacterStringEncoding.AnsiX34;
            }
            len += length;
        }

        public byte[] GetBytes()
        {
            ArrayList res = new ArrayList();
            if (TextEncoding == BacnetCharacterStringEncoding.AnsiX34)
            {
                res.Add((byte) 0);
                res.AddRange(Encoding.UTF8.GetBytes(Value));
            }
            return (byte[])res.ToArray(typeof(byte));
        }
    }
}
