using System.Threading.Tasks;
using BACsharp;

namespace BacNetApi
{
    public interface IBacNetObject
    {
        event ValueChangedEventHandler ValueChangedEvent;

        bool IsExist();
        object Get(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue);
        bool Set(object value, BacnetPropertyId propertyId = BacnetPropertyId.PresentValue);
        bool Create();
        bool Delete();
    }
}