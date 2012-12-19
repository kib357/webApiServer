using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
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
        public DelegateCommand WriteObjectsCommand { get; set; }
        public DelegateCommand StopWriteObjectsCommand { get; set; }

        private volatile bool _write;

        public MyViewModel()
        {
            CreateObjectsCommand = new DelegateCommand(OnCreateObjects, CanCreateAndSubscribe);
            SubscribeObjectsCommand = new DelegateCommand(OnSubscribeObjects, CanCreateAndSubscribe);
            WriteObjectsCommand = new DelegateCommand(OnWriteObjects, CanWriteObjects);
            StopWriteObjectsCommand = new DelegateCommand(OnStopWrite);

            _bacnet = new BacNet("10.81.32.199");//"10.81.32.211");
            _bacnet.NetworkModelChangedEvent += OnNetworkModelChanged;
            Sensors = new ObservableCollection<int>();
            for (var i = 0; i < 100; i++)
            {
                Sensors.Add(0);
            }

            CanChangeSelectedDevice = true;
        }

        private void OnStopWrite()
        {
            _write = false;
        }

        private bool CanWriteObjects()
        {
            return SelectedDevice != null && SelectedDevice.Status == DeviceStatus.Online;
        }

        private void OnWriteObjects()
        {
            _write = true;
            Task.Factory.StartNew(WriteObjects, TaskCreationOptions.LongRunning);
        }

        private void WriteObjects()
        {
            while (_write)
            {
                for (int i = 1; i <= 100; i++)
                {
                    _bacnet[SelectedDevice.Id].Objects["AV" + i].BeginSet((new Random().Next(0,100)).ToString());
                    Thread.Sleep(new Random().Next(20, 100));
                }
            }
        }


        private bool CanCreateAndSubscribe()
        {
            return SelectedDevice != null && SelectedDevice.Status == DeviceStatus.Online && CanChangeSelectedDevice;
        }

        private void OnNetworkModelChanged()
        {
            Devices = new ObservableCollection<BacNetDevice>(_bacnet.OnlineDevices);
            CreateObjectsCommand.RaiseCanExecuteChanged();
            SubscribeObjectsCommand.RaiseCanExecuteChanged();
            WriteObjectsCommand.RaiseCanExecuteChanged();
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
            CanChangeSelectedDevice = false;
        }

        private void OnValueChanged(string address, string value)
        {
            int index;
            double val;
            if (int.TryParse(address.Replace(SelectedDevice.Id + ".AV", ""), out index) &&
                index <= 100 && 
                double.TryParse(value, out val))
            {
                Sensors[index - 1] = (int)val;
            }
        }

        private bool _canChangeSelectedDevice;
        public bool CanChangeSelectedDevice
        {
            get { return _canChangeSelectedDevice; }
            set
            {
                _canChangeSelectedDevice = value;
                CreateObjectsCommand.RaiseCanExecuteChanged();
                SubscribeObjectsCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged("CanChangeSelectedDevice");
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
                    WriteObjectsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SelectedDeviceName { get; set; }
    }
}
