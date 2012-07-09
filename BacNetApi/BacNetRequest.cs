using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BacNetTypes;

namespace BacNetApi
{
    public class BacNetRequest
    {
        public byte InvokeId { get; set; }
        public BacnetConfirmedServices ServiceChoise { get; set; }
        public AutoResetEvent ResetEvent { get; set; }
        public object Ack { get; set; }
    }
}
