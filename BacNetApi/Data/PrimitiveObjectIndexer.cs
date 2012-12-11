using System.Collections.Generic;

namespace BacNetApi.Data
{
    public class PrimitiveObjectIndexer
    {
        private readonly List<PrimitiveObject> _objectList = new List<PrimitiveObject>();
        private readonly BacNetDevice _device;
 
        public PrimitiveObjectIndexer(BacNetDevice device)
        {
            _device = device;
        }

        public bool Contains(string objId)
        {
            return _objectList.FindIndex(o => o.Id == objId) >= 0;
        }

        public List<PrimitiveObject> ToList()
        {
            return _objectList;
        }

        public PrimitiveObject this[string i]
        {
            get
            {
                int index = _objectList.FindIndex(d => d.Id == i);
                if (index < 0)
                {
                    var device = new PrimitiveObject(_device, i);
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