using System;
using System.Collections.Generic;
using System.Linq;

namespace BacNetApi.AccessControl
{
    public class UserIndexer
    {
        private readonly List<User> _userList = new List<User>();
        private readonly BacNetDevice _device;

        public UserIndexer(BacNetDevice device)
        {
            _device = device;
        }

        public List<uint> Get()
        {
            return _device.ObjectList.Where(o => o.Contains("CU")).Select(s => Convert.ToUInt32(s.Replace("CU", ""))).ToList();
        }

        public User this[uint i]
        {
            get
            {
                int index = _userList.FindIndex(d => d.Id == "CU" + i);
                if (index < 0)
                {
                    var user = new User(_device, i);
                    _userList.Add(user);
                    index = _userList.FindIndex(d => d.Id == "CU" + i);
                }
                return _userList[index];
            }
            set
            {
                int index = _userList.FindIndex(d => d.Id == "CU" + i);
                if (index < 0)
                    _userList.Add(value);
                else
                    _userList[index] = value;
            }
        }
    }
}
