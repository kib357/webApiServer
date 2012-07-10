using System.Threading;
using BACsharp;

namespace BacNetApi
{
    public class BacNetRequest
    {
        public int InvokeId { get; set; }
        public BacnetConfirmedServices ServiceChoise { get; set; }
        public AutoResetEvent ResetEvent { get; set; }
        public object Ack { get; set; }
    }
}
