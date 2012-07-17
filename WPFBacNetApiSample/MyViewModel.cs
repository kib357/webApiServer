using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
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

        public DelegateCommand SetValueCommand { get; set; }

        private BacNet _bacnet;

        public MyViewModel()
        {
            _sensors = new ObservableCollection<sensor>();
            _bacnet = new BacNet("192.168.0.109");
            Thread.Sleep(100);
            _bacnet[600].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["BV1"].ValueChangedEvent += OnBacnetValueChanged;
            _bacnet[600].Objects["BV2"].ValueChangedEvent += OnBacnetValueChanged;
            SetValueCommand = new DelegateCommand(SetValue);
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
