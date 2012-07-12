using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types.Primitive;

namespace BacNetApi
{
    public delegate void ValueChangedEventHandler(string address, string value);
    public class BacNetObject : IBacNetObject
    {
        private BacNetDevice _device;
        public string Id { get; private set; }
        private SynchronizationContext _synchronizationContext;

        private string _stringValue;
        public string StringValue
        {
            get { return _stringValue; }
            set
            {
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
                Math.Abs(newValue - oldValue) > 0.5)
                return true;
            return _stringValue != value;
        }

        public BacNetObject(BacNetDevice device, string id)
        {
            _device = device;
            Id = id;
            _synchronizationContext = SynchronizationContext.Current;
        }

        #region Methods

        public Task<bool> IsExist()
        {
            throw new NotImplementedException();
        }

        public object Get(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {           
            return _device.ReadProperty(this, propertyId);
        }

        public T Get<T>(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            if (_device.ReadProperty(this, propertyId) is T)
                return (T) _device.ReadProperty(this, propertyId);
            return default(T);
        }

        public async Task<object> GetAsync(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return await Task.Run(() => Get(propertyId));
        }

        public async Task<T> GetAsync<T>(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return await Task.Run(() => Get<T>());
        }

        public Task<bool> Set(object value, int propertyId = 85)
        {
            throw new NotImplementedException();
        }

        public bool Create()
        {
            return _device.CreateObject(this);               
        }

        public async Task<bool> CreateAsync()
        {
            return await Task.Run(() => Create());
        }

        public Task<bool> Delete()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Events
        private List<ValueChangedEventHandler> _valueChangedSubscribers = new List<ValueChangedEventHandler>();
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

        public static BACnetObjectId GetObjectIdByString(string objectId)
        {
            objectId = objectId.ToUpper();

            int objId;
            if (!int.TryParse(new Regex(@"[0-9]+").Match(objectId).Value, out objId))
                throw new Exception("Not valid string id - wrong object number");

            var objType = new Regex(@"[a-z\-A-Z]+").Match(objectId).Value;
            if (objType == string.Empty)
                throw new Exception("Not valid string id - empty object type");


            return new BACnetObjectId((int)GetObjectType(objType), objId);
        }

        private static BacnetObjectType GetObjectType(string objType)
        {
            switch (objType)
            {
                case "AC":
                    return BacnetObjectType.Accumulator;
                case "AI":
                    return BacnetObjectType.AnalogInput;
                case "AO":
                    return BacnetObjectType.AnalogOutput;
                case "AV":
                    return BacnetObjectType.AnalogValue;
                case "AR":
                    return BacnetObjectType.Averaging;
                case "BI":
                    return BacnetObjectType.BinaryInput;
                case "BO":
                    return BacnetObjectType.BinaryOutput;
                case "BV":
                    return BacnetObjectType.BinaryValue;
                case "CAL":
                    return BacnetObjectType.Calendar;
                case "CMD":
                    return BacnetObjectType.Command;
                case "DEV":
                    return BacnetObjectType.Device;
                case "DC":
                    return BacnetObjectType.Door;
                case "EE":
                    return BacnetObjectType.EventEnrollment;
                case "EL":
                    return BacnetObjectType.EventLog;
                case "FILE":
                    return BacnetObjectType.File;
                case "GG":
                    return BacnetObjectType.GlobalGroup;
                case "GR":
                    return BacnetObjectType.Group;
                case "LSP":
                    return BacnetObjectType.LifeSafetyPoint;
                case "LSZ":
                    return BacnetObjectType.LifeSafetyZone;
                case "LO":
                    return BacnetObjectType.LightingOutput;
                case "LC":
                    return BacnetObjectType.LoadControl;
                case "LOOP":
                    return BacnetObjectType.Loop;
                case "MI":
                    return BacnetObjectType.MultiStateInput;
                case "MO":
                    return BacnetObjectType.MultiStateOutput;
                case "MV":
                    return BacnetObjectType.MultiStateValue;
                case "NC":
                    return BacnetObjectType.NotificationClass;
                case "PROG":
                    return BacnetObjectType.Program;
                case "PC":
                    return BacnetObjectType.PulseConverter;
                case "SCH":
                    return BacnetObjectType.Schedule;
                case "SV":
                    return BacnetObjectType.StructuredView;
                case "TL":
                    return BacnetObjectType.Trendlog;
                case "TLM":
                    return BacnetObjectType.TrendLogMultiple;
                default:
                    throw new Exception("Not valid string id - unknown object type");
            }
        }
    }
}