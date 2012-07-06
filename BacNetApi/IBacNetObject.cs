using System.Threading.Tasks;

namespace BacNetApi
{
    public interface IBacNetObject
    {
        event ValueChangedEventHandler ValueChangedEvent;

        Task<bool> IsExist();
        Task<string> Get(int propertyId = 85);
        Task<bool> Set(object value, int propertyId = 85);        
        Task<bool> Create();
        Task<bool> Delete();
    }
}