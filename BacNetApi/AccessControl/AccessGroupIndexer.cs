using System;
using System.Collections.Generic;
using System.Linq;

namespace BacNetApi.AccessControl
{
    public class AccessGroupIndexer
    {
        private readonly List<AccessGroup> _accessGroupList = new List<AccessGroup>();
        private readonly BacNetDevice _device;

        public AccessGroupIndexer(BacNetDevice device)
        {
            _device = device;
        }

        public List<uint> Get()
        {
            return _device.ObjectList.Where(o => o.Contains("AG")).Select(s => Convert.ToUInt32(s.Replace("AG", ""))).ToList();
        }

        public AccessGroup this[uint i]
        {
            get
            {
                int index = _accessGroupList.FindIndex(d => d.Id == "AG" + i);
                if (index < 0)
                {
                    var user = new AccessGroup(_device, i);
                    _accessGroupList.Add(user);
                    index = _accessGroupList.FindIndex(d => d.Id == "AG" + i);
                }
                return _accessGroupList[index];
            }
            set
            {
                int index = _accessGroupList.FindIndex(d => d.Id == "AG" + i);
                if (index < 0)
                    _accessGroupList.Add(value);
                else
                    _accessGroupList[index] = value;
            }
        }
    }
}
