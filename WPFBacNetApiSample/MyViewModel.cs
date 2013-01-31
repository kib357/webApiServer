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
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi;
using BacNetApi.AccessControl;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.ViewModel;

namespace WPFBacNetApiSample
{
	public delegate void ValuesChangedEventHandler(string address, string oldValue, string newValue);

    public class MyViewModel : NotificationObject
    {
        public DelegateCommand SetValueCommand { get; set; }
        public DelegateCommand GetValueCommand { get; set; }

		public static BacNet Bacnet;

	    private Dictionary<string, string> _cabinetes; 

		public static event ValuesChangedEventHandler ValuesChanged;

	    private List<uint> Chast;
	    private ObjectAddresses _oa;

        public MyViewModel()
        {
            _sensors = new ObservableCollection<sensor>();
            Bacnet = new BacNet("10.81.32.199");//192.168.0.168");
            Bacnet.NetworkModelChangedEvent += OnNetworkModelChanged;
            /*Bacnet[600].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            Bacnet[600].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            Bacnet[600].Objects["AV5432"].ValueChangedEvent += OnBacnetValueChanged;
            Bacnet[600].Objects["BV1"].ValueChangedEvent += OnBacnetValueChanged;
            Bacnet[600].Objects["BV2"].ValueChangedEvent += OnBacnetValueChanged;
            Bacnet[1700].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            Bacnet[1700].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            Bacnet[1701].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            Bacnet[1701].Objects["AV3"].ValueChangedEvent += OnBacnetValueChanged;*/
            //Bacnet[600].Objects["SCH1"].Get((BacnetPropertyId)85);
            //Bacnet[600].Objects["SCH1"].Get((BacnetPropertyId)123);
            //var dev = Bacnet[100].Objects["AV1"].Get();
            //var users = Bacnet[100].Users.Get();
            //var areas = Bacnet[100].AccessGroups[1].Areas;
            //Bacnet[100].AccessGroups[1].SubmitAreas();
            //var exceptions = Bacnet[100].AccessGroups[1].Exceptions;
            //Bacnet[100].AccessGroups[1].Exceptions.Add(new AccessArea() {InstanceNumber = 206002, Type = BacnetObjectType.Door});
            //Bacnet[100].AccessGroups[1].SubmitExceptions();
            ////Bacnet[100].Users[1].WriteCard();
            //var k = Bacnet[100].Users[1].Cards;
            ////var g = Bacnet[100].Users[1].AccessGroups;
            //var name = Bacnet[100].Users[1].Name;
            //Bacnet[100].Users[1].Name = name + " вот";
            //Bacnet[100].Users[1].AccessGroups.Add(3);
            //Bacnet[100].Users[1].SubmitAccessGroups();
            ////Bacnet[100].Users[1].AccessGroups.Clear();
            ////Bacnet[100].Users[1].SubmitAccessGroups();
            //Bacnet[100].Users[1].Cards.Clear();
            //Bacnet[100].Users[1].Cards.Add(new Card() {Number = 123456, SiteCode = 11, Status = 0});
            //Bacnet[100].Users[1].Cards.Add(new Card() { Number = 654321, SiteCode = 11, Status = 0 });
            //Bacnet[100].Users[1].Cards.Add(new Card() { Number = 200000, SiteCode = 11, Status = 0 });
            //Bacnet[100].Users[1].SubmitCards();
            //Bacnet[600].Objects["AV102"].ValueChangedEvent += OnBacnetValueChanged;
            //Bacnet[600].Objects["AV202"].ValueChangedEvent += OnBacnetValueChanged;
            
            SetValueCommand = new DelegateCommand(SetValue);
            GetValueCommand = new DelegateCommand(GetValue);
            SchValues = new List<string>();
			Chast = new List<uint>();
			for (int i = 61; i < 80; i++)
				Chast.Add((uint)i);

			

			//Bacnet[1400].Objects["AV1102"].ValueChangedEvent += OnBacnetValueChanged;
			//Bacnet[1400].Objects["BV1102"].ValueChangedEvent += OnBacnetValueChanged;

			Bacnet[100].Objects["MV1"].ValueChangedEvent += OnBacnetValueChanged;

			//var lc = new LightControl();
			_cabinetes = new Dictionary<string, string>();
	        InitializeCabinetesList1Floor();
			_oa = new ObjectAddresses(1300, InitializeCabinetesList1Floor());
			
        }

		private List<string> get1FloorCabinetesList()
		{
			var cab = new List<string>();
			cab.Add("101(104)");
			cab.Add("113(107)");
			cab.Add("(123)");
			cab.Add("118(109)");
			cab.Add("117(110)");
			cab.Add("(111)");
			cab.Add("116(112)");
			cab.Add("114(122)");
			cab.Add("(136)");

			cab.Add("(137)");
			cab.Add("(145)");
			cab.Add("111(113)");
			cab.Add("110(127)");
			cab.Add("(133)");
			cab.Add("106(129)");
			cab.Add("104(130)");
			cab.Add("(132)");
			cab.Add("103(101)");
			cab.Add("102(102)");
			cab.Add("107(128)");
			cab.Add("105(131)");
			cab.Add("(1321)");
			cab.Add("109(119)");
			cab.Add("108(118)");
			cab.Add("112(117)");
			cab.Add("113(116)");
			cab.Add("(115)");
			cab.Add("115(114)");
			cab.Add("(120)");
			cab.Add("(146)");
			cab.Add("(142)");
			cab.Add("(141)");
			cab.Add("(140)");
			cab.Add("(143)");
			cab.Add("(144)");
			cab.Add("(138)");
			cab.Add("(139)");
			return cab;
		}

		private void ClearErrors()
		{
			foreach (var c in Chast)
			{
				Bacnet[c].Objects["BV17"].BeginSet(1);
			}
			foreach (var c in Chast)
			{
				Bacnet[c].Objects["BV17"].BeginSet(0);
			}
		}

		private Dictionary<string, uint?> InitializeCabinetesList1Floor()
		{
			var res = new Dictionary<string, uint?>
				          {
					          {"101(104)", 1310},
					          {"113(107)", 1312},
					          {"(123)", null},
					          {"118(109)", 1317},
					          {"117(110)", 1320},
					          {"(111)", 1322},
					          {"116(112)", 1322},
					          {"114(122)", 1350},
					          {"(136)", null},
					          {"(137)", null},
					          {"(145)", 1350},
					          {"111(113)", 1301},
					          {"110(127)", 1351},
					          {"(133)", null},
					          {"106(129)", 1302},
					          {"104(130)", 1303},
					          {"(132)", 1305},
					          {"103(101)", 1306},
					          {"102(102)", 1308},
					          {"107(128)", null},
					          {"105(131)", null},
					          {"(1321)", 1350},
					          {"109(119)", 1350},
					          {"108(118)", 1350},
					          {"112(117)", 1313},
					          {"113(116)", 1313},
					          {"(115)", 1314},
					          {"115(114)", 1314},
					          {"(120)", null},
					          {"(146)", 1350},
					          {"(142)", null},
					          {"(141)", null},
					          {"(140)", null},
					          {"(143)", null},
					          {"(144)", 1350},
					          {"(138)", 1350},
					          {"(139)", 1350}
				          };

			return res;
		}

		private void InitializeCabinetesList2Floor()
		{
			//2300
			_cabinetes.Add("202A(229)", "2358");
			_cabinetes.Add("202(228)", "2357");
			_cabinetes.Add("(204a1)", "2357");
			_cabinetes.Add("(204a2)", "2352");
			_cabinetes.Add("201A(201)", "2354");
			_cabinetes.Add("201(202)", "2352");
			_cabinetes.Add("(2041)", "2351");
			_cabinetes.Add("(2042)", "2349");
			_cabinetes.Add("218(205)", "2349");
			_cabinetes.Add("216(206)", "2345");
			_cabinetes.Add("214(208)", "2340");
			_cabinetes.Add("213(209)", "2338");
			_cabinetes.Add("213A(210)", "2336");
			_cabinetes.Add("(240b1)", "2338");
			_cabinetes.Add("(204b2)", "2341");
			_cabinetes.Add("212A(211)", "2333");
			_cabinetes.Add("217A(227)", string.Empty);
			_cabinetes.Add("(222)", string.Empty);
			_cabinetes.Add("(221)", "2350");
			_cabinetes.Add("217(220)", "2346");
			_cabinetes.Add("215(219)", "2342");
			_cabinetes.Add("(213)", "2341");
			_cabinetes.Add("(214)", string.Empty);
			//2400
			_cabinetes.Add("(242)", string.Empty);
			_cabinetes.Add("(221)", "2350");
		}

        private void OnValueChanged(string address, string value)
        {
            DataString = value;
            RaisePropertyChanged("DataString");
        }

        public string DataString { get; set; }

        private void OnNetworkModelChanged()
        {
            Devices = new ObservableCollection<BacNetDevice>(Bacnet.OnlineDevices);
            //IamCount = Bacnet.IamCount;
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
                    RaisePropertyChanged("DeviceCount");
                }
            }
        }

        public int DeviceCount
        {
            get { return _devices.Count; }
        }

        private int _iamCount;
        public int IamCount
        {
            get { return _iamCount; }
            set
            {
                if (_iamCount != value)
                {
                    _iamCount = value;
                    RaisePropertyChanged("IamCount");
                }
            }
        }

        private void GetValue()
        {
	        _oa.WriteAllObjectsToController();
			/*
	        foreach (var cabinete in _cabinetes)
	        {
				//if(string.IsNullOrEmpty(cabinete.Value)) continue;
		        CreateObj("1300", "AV", cabinete.Key, "TemperatureSetpoint", "11");
				//Thread.Sleep(50);
				CreateObj("1300", "AV", cabinete.Key, "CurrentTemperature", "12");
				//Thread.Sleep(50);
				CreateObj("1300", "AV", cabinete.Key, "VentilationSetpoint", "21");
				//Thread.Sleep(50);
				CreateObj("1300", "AV", cabinete.Key, "CurrentVentilationLevel", "22");
				//Thread.Sleep(50);
				CreateObj("1300", "AV", cabinete.Key, "LightLevelSetpoint", "31");
				//Thread.Sleep(50);
				CreateObj("1300", "AV", cabinete.Key, "ConditionerLevelSetpoint", "41");
				//Thread.Sleep(50);
				CreateObj("1300", "BV", cabinete.Key, "TemperatureBacstatAllowed", "19");
				//Thread.Sleep(50);
				CreateObj("1300", "BV", cabinete.Key, "VentilationBacstatAllowed", "29");
				//Thread.Sleep(50);
				CreateObj("1300", "BV", cabinete.Key, "LightStateSetpoint", "31");
				//Thread.Sleep(50);
				CreateObj("1300", "BV", cabinete.Key, "AutoLightLevel", "32");
				//Thread.Sleep(50);
				CreateObj("1300", "BV", cabinete.Key, "LightBacstatAllowed", "39");
				//Thread.Sleep(50);
				CreateObj("1300", "BV", cabinete.Key, "ConditionerStateSetpoint", "41");
				//Thread.Sleep(50);
				CreateObj("1300", "BV", cabinete.Key, "ConditionerBacstatAllowed", "49");
				/*CreateObj(cabinete.Value, "AV", cabinete.Key, "TemperatureSetpointMin", "13");
				CreateObj(cabinete.Value, "AV", cabinete.Key, "TemperatureSetpointMax", "14");
				CreateObj(cabinete.Value, "AV", cabinete.Key, "TActuator", "15");
				CreateObj(cabinete.Value, "AV", cabinete.Key, "CurrentLightLevel", "32");
				CreateObj(cabinete.Value, "AV", cabinete.Key, "MinLightLevel", "33");
				CreateObj(cabinete.Value, "AV", cabinete.Key, "MaxLightLevel", "34");
				CreateObj(cabinete.Value, "BV", cabinete.Key, "WaitLightSensorResponse", "91");
				//CreateObj(cabinete.Value, "CO", cabinete.Key, "TActuatorPID", "91");
				CreateObj(cabinete.Value, "AV", cabinete.Key, "LCDCurrentPage", "91");
				WriteCOV("1300", cabinete.Key, "11", "16");
				WriteCOV("1300", cabinete.Key, "12", "16");
				//WriteCOV(cabinete.Value, cabinete.Key, "34", "16");
				//WriteCOV(cabinete.Value, cabinete.Key, "14", "26");
	        }*/
        }

		private void WriteCOV(string device, string cabinete, string objNumber, string value)
		{
			int start = cabinete.IndexOf('(');
			int end = cabinete.IndexOf(')');
			char[] tmp = new char[0];
			Array.Resize(ref tmp, end - start - 1);
			cabinete.CopyTo(start + 1, tmp, 0, end - start - 1);
			string tmpStr = string.Empty;
			foreach (var chr in tmp)
			{
				tmpStr = tmpStr + chr;
			}
			string createdObject = "AV" + tmpStr + objNumber;
			uint instance;
			if (uint.TryParse(device, out instance))
			{
				Bacnet[instance].Objects[createdObject].BeginSet(0.5, BacnetPropertyId.COVIncrement);
			}
		}

		private void CreateObj(string device, string obj, string cabinete, string objectDescription, string objNumber)
		{
			var objName = new List<BACnetPropertyValue>
                              {
                                  new BACnetPropertyValue((int) BacnetPropertyId.ObjectName,
                                                          new List<BACnetDataType> {new BACnetCharacterString(cabinete + objectDescription)})
                              };
			int start = cabinete.IndexOf('(');
			int end = cabinete.IndexOf(')');
			char[] tmp = new char[0];
			Array.Resize(ref tmp, end - start - 1);
			cabinete.CopyTo(start + 1, tmp, 0, end - start - 1);
			string tmpStr = string.Empty;
			foreach (var chr in tmp)
			{
				if(chr=='A' || chr=='a')
					tmpStr = tmpStr + 1;
				if (chr == 'B' || chr == 'b')
					tmpStr = tmpStr + 2;
				tmpStr = tmpStr + chr;
			}
			string createdObject = obj + tmpStr + objNumber;
			uint instance;
			if (uint.TryParse(device, out instance))
			{
				Bacnet[instance].Objects[createdObject].Create(objName);
				if(objectDescription.Contains("Temperature") && obj=="AV")
					Bacnet[instance].Objects[createdObject].BeginSet(0.5, BacnetPropertyId.COVIncrement); 
			}
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

                    Bacnet[instance].Objects[objAddress].ValueChangedEvent += OnBacnetValueChanged;
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
                    Bacnet[address.Key].Objects[objAddress].ValueChangedEvent += OnBacnetValueChanged;
                }
                //Thread.Sleep(100);
            }
        }

        private void SetValue()
        {
            try
            {
                var address = Address.Split('.');
                Bacnet[Convert.ToUInt32(address[0])].Objects[address[1]].Set(Value);
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
			{
				Sensors.Add(new sensor { Address = address, Value = value });
				if (ValuesChanged != null)
					ValuesChanged(address, null, value);
			}
			else
			{
				if (ValuesChanged != null)
					ValuesChanged(address, el.Value, value);
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
