using System;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;

namespace BacNetApi
{
    internal delegate void RequestTimeEndedEventHandler(BacNetRequest request);
    internal class BacNetRequest
    {
        public int InvokeId { get; set; }
        public BacnetConfirmedService ServiceChoise { get; set; }
        public AutoResetEvent ResetEvent { get; set; }
        public object State { get; set; }
        public event RequestTimeEndedEventHandler RequestTimeEndedEvent;

        private void OnRequestTimeEndedEvent()
        {
            var handler = RequestTimeEndedEvent;
            if (handler != null) handler(this);
        }

        public BacNetRequest()
        {
            RemoveInactive();   
        }

        private async void RemoveInactive()
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
            OnRequestTimeEndedEvent();
        }
    }
}
