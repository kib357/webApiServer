using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;

namespace BacNetApi.Data
{
    public class PrimitiveObject : BacNetObject
    {
        public PrimitivePropertyIndexer Properties { get; set; } 

        public PrimitiveObject(BacNetDevice device, string id)
        {
            _device = device;
            Id = id;
            Properties = new PrimitivePropertyIndexer(this);
            SynchronizationContext = SynchronizationContext.Current;
        }
        

        //#region Events

        public event ValueChangedEventHandler ValueChangedEvent
        {
            add
            {
                Properties[(int)BacnetPropertyId.PresentValue].ValueChangedEvent += value;
            }
            remove
            {
                Properties[(int)BacnetPropertyId.PresentValue].ValueChangedEvent -= value;
            }
        }

        //private void OnValueChangedEvent(object state)
        //{
        //    ValueChangedEventHandler handler = _valueChangedEvent;
        //    if (handler != null) handler(_device.Id + "." + Id, state.ToString());
        //}

        //#endregion

        #region Methods

        public bool IsExist()
        {
            throw new NotImplementedException();
        }

        public string Get(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue, int arrayIndex = -1)
        {
            var value = _device.ReadProperty(this, propertyId, arrayIndex);
            if (value != null && value.Count != 1)
                throw new Exception("Constructed property - cannot read via standard get method");
            return value != null ? value[0].ToString() : null;
        }

        public async Task<object> GetAsync(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return await Task.Run(() => Get(propertyId));
        }

        public bool Set(object value, BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return _device.WriteProperty(this, propertyId, value);
        }

        public void BeginSet(object value, BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            _device.BeginWriteProperty(this, propertyId, value);
        }

        #endregion
    }
}
