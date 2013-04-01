using System.Collections.Generic;
using System.Linq;

namespace BacNetApi.Data
{
    public class PrimitivePropertyIndexer
    {
        private readonly List<PrimitiveProperty> _propertyList = new List<PrimitiveProperty>();
        private readonly PrimitiveObject _primitiveObject;

        public PrimitivePropertyIndexer(PrimitiveObject primitiveObject)
        {
            _primitiveObject = primitiveObject;
        }

        public bool Contains(uint objId)
        {
            return _propertyList.FindIndex(o => o.Id == objId) >= 0;
        }

        public List<PrimitiveProperty> ToList()
        {
            return _propertyList;
        }

        public PrimitiveProperty this[int i]
        {
            get
            {
                var property = _propertyList.FirstOrDefault(d => d.Id == i);
                if (property != null) return property;
                property = new PrimitiveProperty(_primitiveObject, i);
                _propertyList.Add(property);
                return property;
            }
            set
            {
                int index = _propertyList.FindIndex(d => d.Id == i);
                if (index < 0)
                    _propertyList.Add(value);
                else
                    _propertyList[index] = value;
            }
        }
    }
}