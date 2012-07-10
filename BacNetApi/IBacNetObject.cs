using System.Threading.Tasks;
using BACsharp;

namespace BacNetApi
{
    public interface IBacNetObject
    {
        event ValueChangedEventHandler ValueChangedEvent;

        Task<bool> IsExist();
        object Get(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue);
        Task<bool> Set(object value, int propertyId = 85);        
        bool Create();
        Task<bool> Delete();
    }
}