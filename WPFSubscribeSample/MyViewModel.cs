using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.ViewModel;

namespace WPFSubscribeSample
{
    public class MyViewModel : NotificationObject
    {
        private readonly BacNet _bacnet;
        public DelegateCommand CreateObjectsCommand { get; set; }
        public DelegateCommand SubscribeObjectsCommand { get; set; }

        public MyViewModel()
        {
            CreateObjectsCommand = new DelegateCommand(OnCreateObjects, CanCreateAndSubscribe);
            SubscribeObjectsCommand = new DelegateCommand(OnSubscribeObjects, CanCreateAndSubscribe);

            _bacnet = new BacNet("192.168.0.168");//"10.81.32.211");
            _bacnet.NetworkModelChangedEvent += OnNetworkModelChanged;
            Sensors = new ObservableCollection<int>();
            for (var i = 0; i < 100; i++)
            {
                Sensors.Add(0);
            }
        }

        private bool CanCreateAndSubscribe()
        {
            return SelectedDevice != null && SelectedDevice.Status == DeviceStatus.Online;
        }

        private void OnNetworkModelChanged()
        {
            Devices = new ObservableCollection<BacNetDevice>(_bacnet.OnlineDevices);
            CreateObjectsCommand.RaiseCanExecuteChanged();
            SubscribeObjectsCommand.RaiseCanExecuteChanged();
        }

        private void OnCreateObjects()
        {
            for (int i = 1; i <= 100; i++)
            {
                var values = new List<BACnetPropertyValue>()
                    {
                        new BACnetPropertyValue(85, new List<BACnetDataType>() {new BACnetReal( new Random().Next(0,100))})
                    };
                _bacnet[SelectedDevice.Id].Objects["AV" + i].Create(values);
            }
        }

        private void OnSubscribeObjects()
        {
            for (int i = 1; i <= 100; i++)
            {
                _bacnet[SelectedDevice.Id].Objects["AV" + i].ValueChangedEvent += OnValueChanged;
            }
        }

        private void OnValueChanged(string address, string value)
        {
            int index;
            double val;
            if (int.TryParse(address.Replace("AV", ""), out index) &&
                index <= 100 && 
                double.TryParse(value, out val))
            {
                Sensors[index - 1] = (int)val;
            }
        }

        private ObservableCollection<int> _sensors;
        public ObservableCollection<int> Sensors
        {
            get { return _sensors; }
            set
            {
                _sensors = value;
                RaisePropertyChanged("Sensors");
            }
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

        private BacNetDevice _selectedDevice;
        public BacNetDevice SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    SelectedDeviceName = _selectedDevice.Id + " " + _selectedDevice.Title;
                    RaisePropertyChanged("SelectedDevice");
                    RaisePropertyChanged("SelectedDeviceName");
                    CreateObjectsCommand.RaiseCanExecuteChanged();
                    SubscribeObjectsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SelectedDeviceName { get; set; }
    }
}
