using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using BacNetApi;
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

        public MyViewModel()
        {
            _sensors = new ObservableCollection<sensor>();
            var bacnet = new BacNet();
            bacnet.Initialize(IPAddress.Parse("192.168.0.101"));            
            bacnet[200].Objects["AV21"].ValueChangedEvent += OnBacnetValueChanged;
            bacnet[200].Objects["AV1"].ValueChangedEvent += OnBacnetValueChanged;
            bacnet[200].Objects["AV2"].ValueChangedEvent += OnBacnetValueChanged;
            bacnet[200].Objects["AV5"].ValueChangedEvent += OnBacnetValueChanged;
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
