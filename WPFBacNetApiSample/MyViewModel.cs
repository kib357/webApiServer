using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
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

        public DelegateCommand SetValueCommand { get; set; }
        public DelegateCommand GetValueCommand { get; set; }

        private BacNet _bacnet;

        public MyViewModel()
        {
            _sensors = new ObservableCollection<sensor>();
            _bacnet = new BacNet("192.168.0.134");
            Thread.Sleep(100);
            _bacnet[600].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["BV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["BV2"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["MV1"].ValueChangedEvent += OnBacnetValueChanged;
            //_bacnet[600].Objects["SCH1"].Get((BacnetPropertyId)85);
            //_bacnet[600].Objects["SCH1"].Get((BacnetPropertyId)123);
            
            
            SetValueCommand = new DelegateCommand(SetValue);
            GetValueCommand = new DelegateCommand(GetValue);
            SchValues = new List<string>();
        }

        private void GetValue()
        {
            /*for (int i = 0; i < 8; i++)
            {
                var tmp = _bacnet[600].Objects["SCH1"].Get((BacnetPropertyId) 123, i);
                if (tmp != null)
                    if (SchValues.Contains(tmp.ToString()))
                        SchValues[SchValues.IndexOf(tmp.ToString())] = tmp.ToString();
                    else
                        SchValues.Add(tmp.ToString());
            }*/
            /*var tmp = _bacnet[600].Objects["SCH1"].Get(BacnetPropertyId.WeeklySchedule);
            string res = tmp.ToString();*/
            /*var res = _bacnet[600].Objects["SCH1"].Get(BacnetPropertyId.ListOfObjectPropertyReferences);
            List<string> tmp = new List<string>();
            if(res is List<BACnetDataType>)
                foreach (var obj in res as List<BACnetDataType>)
                {
                    tmp.Add(obj.ToString());
                }
            else tmp.Add(res.ToString());
            var newControlledObjects = new ListOfObjectPropertyReferences();
            BACnetDeviceObjectPropertyReference obj1 = new BACnetDeviceObjectPropertyReference();
            obj1.ObjectId.ObjectType = (int)BacnetObjectType.BinaryValue;
            obj1.ObjectId.Instance = 1;
            obj1.PropertyId = new BACnetEnumerated((int)BacnetPropertyId.PresentValue, 1);
            newControlledObjects.Objects.Add(obj1);
            _bacnet[600].Objects["SCH1"].Set(newControlledObjects, BacnetPropertyId.ListOfObjectPropertyReferences);
            var res1 = _bacnet[600].Objects["SCH1"].Get(BacnetPropertyId.ListOfObjectPropertyReferences);*/
            BacNetDevice dev = new BacNetDevice(600, _bacnet);
            BacNetObject obj = new BacNetObject(dev, "SCH2");
            _bacnet[600].Objects["SCH3"].Create();
            _bacnet[600].Objects["SCH2"].Create();
            /*tmp = new List<string>();
            foreach (var obj in res as List<BACnetDataType>)
            {
                tmp.Add(obj.ToString());
            }*/
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
    }
}
