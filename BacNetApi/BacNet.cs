using System.Collections.Generic;

namespace BacNetApi
{
    public class BacNet : IBacNetservices
    {
        private readonly List<BacNetDevice> _deviceList = new List<BacNetDevice>(); 

        public BacNetDevice this[int i]
        {
            get
            {
                int index = _deviceList.FindIndex(d => d.Id == i);
                if (index < 0)
                {
                    var device = new BacNetDevice(i, this);
                    _deviceList.Add(device);
                    index = _deviceList.FindIndex(d => d.Id == i);
                }
                return _deviceList[index];
            }
            set
            {
                int index = _deviceList.FindIndex(d => d.Id == i);
                if (index < 0)
                    _deviceList.Add(value);
                else
                    _deviceList[index] = value;
            }
        }
    }

    public delegate void ValueChangedEventHandler(string address, string value);
}