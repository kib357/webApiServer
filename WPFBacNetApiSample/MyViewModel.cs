using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
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
        private string ll;

        public MyViewModel()
        {
            //ll = 100;
            _sensors = new ObservableCollection<sensor>();
            _bacnet = new BacNet("10.81.32.211");
            _bacnet.NetworkModelChangedEvent += OnNetworkModelChanged;
            Thread.Sleep(100);
            
            SetValueCommand = new DelegateCommand(SetValue);
            GetValueCommand = new DelegateCommand(GetValue);

            var tmpList = new List<uint>();
            tmpList.Add(1100);
            tmpList.Add(1200);
            tmpList.Add(1400);
            tmpList.Add(1500);

            //_bacnet.FindSeveral(tmpList);
            //var tmp = _bacnet[1600].Objects["BI1101"].Get();
            _bacnet[1400].Objects["BI1102"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[1400].Objects["BV1102"].ValueChangedEvent += OnBacnetValueChanged;
            SchValues = new List<string>();
            object obj = _bacnet[17811].Objects["AO1104"].Get();
            ll = obj.ToString();

            TestDali();

        }

        private void TestDali()
        {
            int time = 1000;
            while (true)
            {
                ll = ll == "0" ? "100" : "0";
                _bacnet[17811].Objects["AO1104"].Set(ll);
                Thread.Sleep(time);
            }
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
            var q = _bacnet.DeviceList;
            var tmp = 0;
        }

        private void GetBacnetAddresses(string relativePath = @"Resources\Dictionaries")
        {
            var path = @"C:\dict";
            if(!Directory.Exists(path)) return;
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
            ll = ll == "0" ? "100" : "0";
            _bacnet[17811].Objects["AO1104"].Set(ll);
            /*try
            {
                var address = Address.Split('.');
                _bacnet[Convert.ToUInt32(address[0])].Objects[address[1]].Set(Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }*/
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
