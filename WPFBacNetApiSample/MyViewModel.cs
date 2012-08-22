using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using BACsharp;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.ViewModel;

namespace WPFBacNetApiSample
{
    public class MyViewModel : NotificationObject
    {
        public DelegateCommand SetValueCommand { get; set; }
        public DelegateCommand GetValueCommand { get; set; }

        private BacNet _bacnet;

        public MyViewModel()
        {
            _sensors = new ObservableCollection<sensor>();
            _bacnet = new BacNet("192.168.0.121");
            Thread.Sleep(100);
            _bacnet[600].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["BV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["BV2"].ValueChangedEvent += OnBacnetValueChanged;
            //_bacnet[600].Objects["SCH1"].Get((BacnetPropertyId)85);
            //_bacnet[600].Objects["SCH1"].Get((BacnetPropertyId)123);
            
            
            SetValueCommand = new DelegateCommand(SetValue);
            GetValueCommand = new DelegateCommand(GetValue);
            SchValues = new List<string>();
        }

        private void GetValue()
        {
            /*List<BACnetDataType> propertyValues = new List<BACnetDataType>();
            propertyValues.Add(new BACnetDailySchedule()); //1
            BACnetDailySchedule dailySchedule = new BACnetDailySchedule();
            dailySchedule.Values.Add(new BACnetTimeValue(new BACnetTime(9, 30, 0, 0), new BACnetEnumerated(1)));
            dailySchedule.Values.Add(new BACnetTimeValue(new BACnetTime(13, 30, 0, 0), new BACnetNull()));
            propertyValues.Add(dailySchedule); // 2
            propertyValues.Add(new BACnetDailySchedule()); //3
            propertyValues.Add(new BACnetDailySchedule()); //4
            propertyValues.Add(new BACnetDailySchedule()); //5
            propertyValues.Add(new BACnetDailySchedule()); //6
            propertyValues.Add(new BACnetDailySchedule()); //7*/
            var val = new List<string> { "600.AV2" };
            var bacval = new List<BACnetDeviceObjectPropertyReference>();
            foreach (var v in val)
            {
                bacval.Add(GetPropertyReferensFromString(v));
            }
            var tmp = _bacnet[600].Objects["SCH301"].Set(bacval, (BacnetPropertyId)54);
        }

        private void SetValue()
        {
            try
            {
                var address = Address.Split('.');
                _bacnet[Convert.ToUInt32(address[0])].Objects[address[1]].Set(Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnBacnetValueChanged(string address, string value)
        {
            var el = Sensors.FirstOrDefault(s => s.Address == address);
            if (el == null)
                Sensors.Add(new sensor { Address = address, Value = value });
            else
            {
                Sensors.Remove(el);
                Sensors.Add(new sensor { Address = address, Value = value });
            }

            RaisePropertyChanged("Sensors");
        }

        private BACnetDeviceObjectPropertyReference GetPropertyReferensFromString(string obj)
        {
            var res = new BACnetDeviceObjectPropertyReference();
            string devAddr = string.Empty;
            string objAddress;
            int devAddress = 0;
            if (obj.Contains("."))
            {
                devAddr = obj.Split('.')[0];
                int.TryParse(devAddr, out devAddress);
                objAddress = obj.Split('.')[1];
            }
            else
                objAddress = obj;

            var objType = new Regex(@"[a-z\-A-Z]+").Match(objAddress).Value;
            var objNum = new Regex(@"[0-9]+").Match(objAddress).Value;

            int objNumber;
            int.TryParse(objNum, out objNumber);

            if (!string.IsNullOrWhiteSpace(devAddr))
                res.DeviceId = new BACnetObjectId((int)BacnetObjectType.Device, devAddress, 3);
            res.ObjectId.Instance = objNumber;
            res.PropertyId = new BACnetEnumerated((int)BacnetPropertyId.PresentValue, 1);

            objType = objType.ToUpper();
            switch (objType)
            {
                case "AI":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.AnalogInput;
                    break;
                case "AO":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.AnalogOutput;
                    break;
                case "AV":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.AnalogValue;
                    break;
                case "BI":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.BinaryInput;
                    break;
                case "BO":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.BinaryOutput;
                    break;
                case "BV":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.BinaryValue;
                    break;
                case "MI":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.MultiStateInput;
                    break;
                case "MO":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.MultiStateOutput;
                    break;
                case "MV":
                    res.ObjectId.ObjectType = (int)BacnetObjectType.MultiStateValue;
                    break;
            }
            return res;
        }

        public class sensor
        {
            public string Address { get; set; }
            public string Value { get; set; }
        }

        private ObservableCollection<sensor> _sensors;
        public ObservableCollection<sensor> Sensors
        {
            get { return _sensors; }
            set
            {
                if (_sensors != value)
                {
                    _sensors = value;
                    RaisePropertyChanged("Sensors");
                }
            }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            set
            {
                if (_address != value)
                {
                    _address = value;
                    RaisePropertyChanged("Address");
                }
            }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    RaisePropertyChanged("Value");
                }
            }
        }

        private List<string> _schValues;
        public List<string> SchValues
        {
            get { return _schValues; }
            set
            {
                if (_schValues != value)
                {
                    _schValues = value;
                    RaisePropertyChanged("SchValues");
                }
            }
        }
    }
}
