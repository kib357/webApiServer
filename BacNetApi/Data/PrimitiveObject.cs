using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;

namespace BacNetApi.Data
{
    public class PrimitiveObject : BacNetObject
    {
        public DateTime LastUpdated { get; protected set; }

        public PrimitiveObject(BacNetDevice device, string id)
        {
            _device = device;
            Id = id;
            _synchronizationContext = SynchronizationContext.Current;
        }

        private string _stringValue;
        public string StringValue
        {
            get { return _stringValue; }
            set
            {
                _device.LastUpdated = LastUpdated = DateTime.Now;
                if (CheckValueChanges(value))
                {
                    _stringValue = value;
                    if (_synchronizationContext != null)
                        _synchronizationContext.Post(OnValueChangedEvent, _stringValue);
                    else
                        OnValueChangedEvent(_stringValue);
                }
            }
        }

        private bool CheckValueChanges(string value)
        {
            double oldValue, newValue;
            if (double.TryParse(value.Replace(',', '.'), out newValue) &&
                double.TryParse(_stringValue, out oldValue) &&
                Math.Abs(newValue - oldValue) >= 0.1)
                return true;
            return _stringValue != value;
        }

        #region Events
        private readonly List<ValueChangedEventHandler> _valueChangedSubscribers = new List<ValueChangedEventHandler>();
        private event ValueChangedEventHandler _valueChangedEvent;
        public event ValueChangedEventHandler ValueChangedEvent
        {
            add
            {
                _valueChangedEvent += value;
                _valueChangedSubscribers.Add(value);
                _device.AddSubscriptionObject(this);
            }
            remove
            {
                _valueChangedEvent -= value;
                _valueChangedSubscribers.Remove(value);
                if (_valueChangedSubscribers.Count == 0)
                    _device.RemoveSubscriptionObject(this);
            }
        }

        private void OnValueChangedEvent(object state)
        {
            ValueChangedEventHandler handler = _valueChangedEvent;
            if (handler != null) handler(_device.Id + "." + Id, state.ToString());
        }

        #endregion

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
