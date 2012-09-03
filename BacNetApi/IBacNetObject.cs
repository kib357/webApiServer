using System.Collections.Generic;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types.Constructed;

namespace BacNetApi
{
    public interface IBacNetObject
    {
        event ValueChangedEventHandler ValueChangedEvent;

        bool IsExist();
        object Get(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue, int arrayIndex = -1);
        bool Set(object value, BacnetPropertyId propertyId = BacnetPropertyId.PresentValue);
        bool Create(List<BACnetPropertyValue> data);
        bool Delete();
    }
}