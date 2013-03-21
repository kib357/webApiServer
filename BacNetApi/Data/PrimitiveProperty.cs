using System;
using System.Collections.Generic;
using System.Threading;
using BACsharp;

namespace BacNetApi.Data
{
    public class PrimitiveProperty
    {
        internal readonly PrimitiveObject _primitiveObject;
        protected SynchronizationContext SynchronizationContext;
        public DateTime LastUpdated { get; protected set; }
        public int Id { get; private set; }
        public float COVIncrement { get; set; }

        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                _primitiveObject._device.LastUpdated = LastUpdated = DateTime.Now;
                if (CheckValueChanges(value))
                {
                    _value = value;
                    if (SynchronizationContext != null)
                        SynchronizationContext.Post(OnValueChangedEvent, _value);
                    else
                        OnValueChangedEvent(_value);
                }
            }
        }

        public PrimitiveProperty(PrimitiveObject primitiveObject, int id)
        {
            COVIncrement = (float)0.1;
            _primitiveObject = primitiveObject;
            Id = id;
            SynchronizationContext = SynchronizationContext.Current;
        }

        public bool SetValue(string newValue)
        {
            return false;
        }

        private bool CheckValueChanges(string value)
        {
            double oldValue, newValue;
            if (double.TryParse(value.Replace(',', '.'), out newValue) &&
                double.TryParse(_value, out oldValue) &&
                Math.Abs(newValue - oldValue) >= 0.1)
                return true;
            return _value != value;
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
                _primitiveObject._device.AddSubscriptionObject(this);
            }
            remove
            {
                _valueChangedEvent -= value;
                _valueChangedSubscribers.Remove(value);
                if (_valueChangedSubscribers.Count == 0)
                    _primitiveObject._device.RemoveSubscriptionObject(this);
            }
        }

        private void OnValueChangedEvent(object state)
        {
            ValueChangedEventHandler handler = _valueChangedEvent;
            if (handler != null) handler(_primitiveObject._device.Id + "." + _primitiveObject.Id + 
                (Id != (uint)BacnetPropertyId.PresentValue ? "." + Id : ""), state.ToString());
        }

        #endregion
    }
}
