using System.Collections.Generic;

namespace BacNetApi
{
    public class BacNetObjectIndexer
    {
        private readonly List<BacNetObject> _objectList = new List<BacNetObject>();
        private readonly BacNetDevice _device;
 
        public BacNetObjectIndexer(BacNetDevice device)
        {
            _device = device;
        }

        public bool Contains(string objId)
        {
            return _objectList.FindIndex(o => o.Id == objId) >= 0;
        }

        public BacNetObject this[string i]
        {
            get
            {
                int index = _objectList.FindIndex(d => d.Id == i);
                if (index < 0)
                {
                    var device = new BacNetObject(_device, i);
                    _objectList.Add(device);
                    index = _objectList.FindIndex(d => d.Id == i);
                }
                return _objectList[index];
            }
            set
            {
                int index = _objectList.FindIndex(d => d.Id == i);
                if (index < 0)
                    _objectList.Add(value);
                else
                    _objectList[index] = value;
            }
        }
    }
}