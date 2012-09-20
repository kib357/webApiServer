using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;

namespace BacNetApi
{
    public delegate void ValueChangedEventHandler(string address, string value);
    public class BacNetObject : IBacNetObject
    {
        private readonly BacNetDevice _device;
        public string Id { get; private set; }
        private readonly SynchronizationContext _synchronizationContext;

        public DateTime LastUpdated { get; private set; }
        private DateTime _lastNewValue = DateTime.MinValue;

        private string _stringValue;
        public string StringValue
        {
            get { return _stringValue; }
            set
            {
                if (_lastNewValue == DateTime.MinValue) _lastNewValue = DateTime.Now;
                _device.LastUpdated = LastUpdated = DateTime.Now;                
                if (CheckValueChanges(value) || LastUpdated - _lastNewValue > TimeSpan.FromMinutes(60))
                {
                    _stringValue = value;
                    _lastNewValue = DateTime.Now;
                    if (_synchronizationContext != null)
                        _synchronizationContext.Post(OnValueChangedEvent, _stringValue);
                    else
                        OnValueChangedEvent(_stringValue);
                }
            }
        }

        private bool CheckValueChanges(string value)
        {
            var formatInfo = new NumberFormatInfo { NumberDecimalSeparator = "," };
            double oldValue, newValue;
            if (double.TryParse(value.Replace('.', ','), NumberStyles.Any, formatInfo, out newValue) &&
                double.TryParse(_stringValue.Replace('.',','), NumberStyles.Any, formatInfo, out oldValue) &&
                Math.Abs(newValue - oldValue) > 0.1)
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

        public bool IsExist()
        {
            throw new NotImplementedException();
        }

        public object Get(BacnetPropertyId propertyId = BacnetPropertyId.PresentValue, int arrayIndex = -1)
        {           
            return _device.ReadProperty(this, propertyId, arrayIndex);
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
            return await Task.Run(() => Get<T>(propertyId));
        }

        public bool Set(object value, BacnetPropertyId propertyId = BacnetPropertyId.PresentValue)
        {
            return _device.WriteProperty(this, propertyId, value);
        }

        public bool Create(List<BACnetPropertyValue> data = null)
        {
            if (data == null)
                data = new List<BACnetPropertyValue>();
            return _device.CreateObject(this, data);               
        }

        public async Task<bool> CreateAsync(List<BACnetPropertyValue> data)
        {
            return await Task.Run(() => Create(data));
        }

        public bool Delete()
        {
            return _device.DeleteObject(this);
        }

        #endregion

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
                /*case "LO":
                    return BacnetObjectType.LightingOutput;*/
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

        public static string GetStringId(BacnetObjectType type)
        {
            string res = string.Empty;
            switch (type)
            {
                case BacnetObjectType.Accumulator:
                    res = "AC"; break;
                case BacnetObjectType.AnalogInput:
                    res = "AI"; break;
                case BacnetObjectType.AnalogOutput:
                    res = "AO"; break;
                case BacnetObjectType.AnalogValue:
                    res = "AV"; break;
                case BacnetObjectType.Averaging:
                    res = "AR"; break;
                case BacnetObjectType.BinaryInput:
                    res = "BI"; break;
                case BacnetObjectType.BinaryOutput:
                    res = "BO"; break;
                case BacnetObjectType.BinaryValue:
                    res = "BV"; break;
                case BacnetObjectType.Calendar:
                    res = "CAL"; break;
                case BacnetObjectType.Command:
                    res = "CMD"; break;
                case BacnetObjectType.Device:
                    res = "DEV"; break;
                case BacnetObjectType.Door:
                    res = "DC"; break;
                case BacnetObjectType.EventEnrollment:
                    res = "EE"; break;
                case BacnetObjectType.EventLog:
                    res = "EL"; break;
                case BacnetObjectType.File:
                    res = "FILE"; break;
                case BacnetObjectType.GlobalGroup:
                    res = "GG"; break;
                case BacnetObjectType.Group:
                    res = "GR"; break;
                case BacnetObjectType.LifeSafetyPoint:
                    res = "LSP"; break;
                case BacnetObjectType.LifeSafetyZone:
                    res = "LSZ"; break;
                /*case BacnetObjectType.LightingOutput:
                    res = "LO"; break;*/
                case BacnetObjectType.LoadControl:
                    res = "LC"; break;
                case BacnetObjectType.Loop:
                    res = "LOOP"; break;
                case BacnetObjectType.MultiStateInput:
                    res = "MI"; break;
                case BacnetObjectType.MultiStateOutput:
                    res = "MO"; break;
                case BacnetObjectType.MultiStateValue:
                    res = "MV"; break;
                case BacnetObjectType.NotificationClass:
                    res = "NC"; break;
                case BacnetObjectType.Program:
                    res = "PROG"; break;
                case BacnetObjectType.PulseConverter:
                    res = "PC"; break;
                case BacnetObjectType.Schedule:
                    res = "SCH"; break;
                case BacnetObjectType.StructuredView:
                    res = "SV"; break;
                case BacnetObjectType.Trendlog:
                    res = "TL"; break;
                case BacnetObjectType.TrendLogMultiple:
                    res = "TLM"; break;
            }
            return res;
        }
    }
}