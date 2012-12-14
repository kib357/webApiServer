using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using BACsharp;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi;
using BacNetApi.AccessControl;
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
            _bacnet = new BacNet("192.168.0.168");
            _bacnet.NetworkModelChangedEvent += OnNetworkModelChanged;
            Thread.Sleep(100);
            /*_bacnet[600].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["AV5432"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["BV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["BV2"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[1700].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[1700].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[1701].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[1701].Objects["AV3"].ValueChangedEvent += OnBacnetValueChanged;*/
            //_bacnet[600].Objects["SCH1"].Get((BacnetPropertyId)85);
            //_bacnet[600].Objects["SCH1"].Get((BacnetPropertyId)123);
            //GetBacnetAddresses();
            var dev = _bacnet[100].Objects["AV1"].Get();
            Thread.Sleep(500);
            var users = _bacnet[100].Users.Get();
            var areas = _bacnet[100].AccessGroups[1].Areas;
            _bacnet[100].AccessGroups[1].SubmitAreas();
            var exceptions = _bacnet[100].AccessGroups[1].Exceptions;
            _bacnet[100].AccessGroups[1].Exceptions.Add(new AccessArea() {InstanceNumber = 206002, Type = BacnetObjectType.Door});
            _bacnet[100].AccessGroups[1].SubmitExceptions();
            //_bacnet[100].Users[1].WriteCard();
            var k = _bacnet[100].Users[1].Cards;
            //var g = _bacnet[100].Users[1].AccessGroups;
            _bacnet[100].Users[1].AccessGroups.Add(3);
            _bacnet[100].Users[1].SubmitAccessGroups();
            //_bacnet[100].Users[1].AccessGroups.Clear();
            //_bacnet[100].Users[1].SubmitAccessGroups();
            _bacnet[100].Users[1].Cards.Clear();
            _bacnet[100].Users[1].Cards.Add(new Card() {Number = 123456, SiteCode = 11, Status = 0});
            _bacnet[100].Users[1].Cards.Add(new Card() { Number = 654321, SiteCode = 11, Status = 0 });
            _bacnet[100].Users[1].Cards.Add(new Card() { Number = 200000, SiteCode = 11, Status = 0 });
            _bacnet[100].Users[1].SubmitCards();
            //_bacnet[600].Objects["AV102"].ValueChangedEvent += OnBacnetValueChanged;
            //_bacnet[600].Objects["AV202"].ValueChangedEvent += OnBacnetValueChanged;
            
            SetValueCommand = new DelegateCommand(SetValue);
            GetValueCommand = new DelegateCommand(GetValue);
            SchValues = new List<string>();
        }

        private void OnNetworkModelChanged()
        {
            Devices = new ObservableCollection<BacNetDevice>(_bacnet.SubscribedDevices);
        }

        private ObservableCollection<BacNetDevice> _devices;
        public ObservableCollection<BacNetDevice> Devices
        {
            get { return _devices; }
            set
            {
                if (_devices != value)
                {
                    _devices = value;
                    RaisePropertyChanged("Devices");
                }
            }
        }

        private void GetValue()
        {
            /*List<BACnetDataType> propertyValues = new List<BACnetDataType>();
            propertyValues.Add(new BACnetDailySchedule()); //1
            BACnetDailySchedule dailySchedule = new BACnetDailySchedule();
            dailySchedule.Values.Add(new BACnetTimeValue(new BACnetTime(0, 0, 0, 0), new BACnetNull()));
            dailySchedule.Values.Add(new BACnetTimeValue(new BACnetTime(9, 30, 0, 0), new BACnetEnumerated(1)));
            dailySchedule.Values.Add(new BACnetTimeValue(new BACnetTime(13, 30, 0, 0), new BACnetNull()));
            propertyValues.Add(dailySchedule); // 2
            propertyValues.Add(new BACnetDailySchedule()); //3
            propertyValues.Add(new BACnetDailySchedule()); //4
            propertyValues.Add(new BACnetDailySchedule()); //5
            propertyValues.Add(new BACnetDailySchedule()); //6
            propertyValues.Add(new BACnetDailySchedule()); //7
            var val = new List<string> { "600.AV2" };
            var bacval = new List<BACnetDataType>();
            foreach (var v in val)
            {
                bacval.Add(GetPropertyReferensFromString(v));
            }
            List<BACnetPropertyValue> list = new List<BACnetPropertyValue>();
            list.Add(new BACnetPropertyValue((int)BacnetPropertyId.WeeklySchedule, propertyValues));
            list.Add(new BACnetPropertyValue((int)BacnetPropertyId.ListOfObjectPropertyReferences, bacval));
            var lst = new List<BACnetDataType>{new BACnetCharacterString("qwe")};
            list.Add(new BACnetPropertyValue((int)BacnetPropertyId.ObjectName, lst));
            var tmp = _bacnet[600].Objects["SCH1"].Set(propertyValues, BacnetPropertyId.WeeklySchedule);*/
            /*var values = new Dictionary<string, Dictionary<BacnetPropertyId, object>>();
            var val = new Dictionary<BacnetPropertyId, object>();
            val.Add(BacnetPropertyId.ObjectName, "AnalogValue1");
            val.Add(BacnetPropertyId.PresentValue, 10);
            values.Add("AV1", val);
            val = new Dictionary<BacnetPropertyId, object>();
            val.Add(BacnetPropertyId.ObjectName, "AnalogValue2");
            val.Add(BacnetPropertyId.PresentValue, 10);
            values.Add("AV2", val);

            _bacnet[600].WritePropertyMultiple(values);*/
        }

        private void GetBacnetAddresses(string relativePath = @"Resources\Dictionaries")
        {
            var path = @"C:\dict";
            var addresses = new Dictionary<uint, List<string>>();
            foreach (string fileName in Directory.GetFiles(path).Where(f => f.EndsWith(".xaml")))
            {
                XDocument doc;
                using (var sr = new StreamReader(fileName))
                {
                    doc = XDocument.Load(sr);
                }

                foreach (var descendant in doc.Root.Descendants())
                {
                    foreach (var xAttribute in descendant.Attributes())
                    {
                        if (xAttribute.Name.LocalName.ToLower().Contains("address"))
                        {
                            var addrList = xAttribute.Value.Split(',');
                            foreach (var addr in addrList)
                            {
                                uint instance;
                                if (!uint.TryParse(addr.Split('.')[0].Trim(), out instance)) continue;
                                string objAddress = addr.Split('.')[1].Trim();
                                if (addresses.ContainsKey(instance))
                                {
                                    if (!addresses[instance].Contains(objAddress))
                                        addresses[instance].Add(objAddress);
                                }
                                else
                                    addresses.Add(instance, new List<string>{objAddress});                                
                            }
                        }
                    }
                }
            }
            AddBacnetObjects(addresses);
        }

        public void AddBacnetObject(string address)
        {
            
            if (string.IsNullOrWhiteSpace(address) || !address.Contains('.')) return;

            uint instance;
            if (uint.TryParse(address.Split('.')[0].Trim(), out instance))
            {
                string objAddress = address.Split('.')[1].Trim();

                    _bacnet[instance].Objects[objAddress].ValueChangedEvent += OnBacnetValueChanged;
            }            
        }

        public void AddBacnetObjects(Dictionary<uint, List<string>> addresses)
        {
            var start = DateTime.Now;
            if (addresses == null) return;

            foreach (var address in addresses)
            {
                if (address.Value == null) continue;
                foreach (var objAddress in address.Value)
                {
                    _bacnet[address.Key].Objects[objAddress].ValueChangedEvent += OnBacnetValueChanged;
                }
                //Thread.Sleep(100);
            }
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
