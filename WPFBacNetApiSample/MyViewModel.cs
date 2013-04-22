using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BacNetApi;
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

		public static event ValuesChangedEventHandler ValuesChanged;

        public MyViewModel()
        {
			GetValueCommand = new DelegateCommand(GetValue);
            _sensors = new ObservableCollection<sensor>();
	        Bacnet = new BacNet("10.81.32.199");//10.81.32.199");
            Bacnet.NetworkModelChangedEvent += OnNetworkModelChanged;
			Bacnet[17822].Objects["AO68099"].ValueChangedEvent += OnValueChanged;
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
	        Bacnet[17822].Objects["AO68099"].Set(30);
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
