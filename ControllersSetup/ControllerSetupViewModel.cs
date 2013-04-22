using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using BacNetApi;
using Microsoft.Office.Interop.Excel;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.ViewModel;
using Application = Microsoft.Office.Interop.Excel.Application;

namespace ControllersSetup
{
	internal class ControllerSetupViewModel : NotificationObject
	{
		public static BacNet Bacnet;
		private readonly string _dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		private List<ControllersObjects> _co = new List<ControllersObjects>();

		public DelegateCommand StartProviderCommand { get; set; }
		public DelegateCommand GenerateProgramsCommand { get; set; }
		public DelegateCommand CreateAllObjectsCommand { get; set; }
		
		public ControllerSetupViewModel()
		{
			var strHostName = Dns.GetHostName();
			var ipHostEntry = Dns.GetHostByName(strHostName);
			IP = ipHostEntry.AddressList[0].ToString();
			ProgramsPath = _dir + @"\ControllersPrograms";
			SavedPGTextBlockVisibility = Visibility.Collapsed;
			CreatedAllObjectsTextBlockVisibility = Visibility.Collapsed;

			StartProviderCommand = new DelegateCommand(StartProvider);
			GenerateProgramsCommand = new DelegateCommand(GeneratePrograms);
			CreateAllObjectsCommand = new DelegateCommand(CreateAllObjects);

			LoadCabinetes();
		}

		private void LoadCabinetes()
		{
			var path = _dir + @"\Rooms";
			if (!Directory.Exists(path)) return;
			foreach (var fileName in Directory.GetFiles(path).Where(f => f.EndsWith(".xlsx")))
			{
				var xlApp = new Application();
				var xlWorkBook = xlApp.Workbooks.Open(fileName);
				var xlWorkSheet = (Worksheet) xlWorkBook.Worksheets.Item[1];
				var range = xlWorkSheet.Range["A1", Missing.Value];
				var controller = range.Text;
				var rooms = new Dictionary<string, KeyValuePair<uint?, string>>();
				var i = 1;
				while (true)
				{
					range = xlWorkSheet.Range["B" + i, Missing.Value];
					var room = range.Text;
					if (string.IsNullOrEmpty(room))
						break;
					range = xlWorkSheet.Range["C" + i, Missing.Value];
					var vav = string.IsNullOrEmpty(range.Text) ? null : Convert.ToUInt32(range.Text);
					range = xlWorkSheet.Range["D" + i, Missing.Value];
					string lcd = string.IsNullOrWhiteSpace(range.Text) ? "101" : range.Text;
					var kvp = new KeyValuePair<uint?, string>(vav, lcd);
					rooms.Add(room, kvp);
					i++;
				}
				//xlWorkBook.Close(Missing.Value, Missing.Value, Missing.Value);
				xlApp.Quit();
				ReleaseObject(xlWorkSheet);
				ReleaseObject(xlWorkBook);
				ReleaseObject(xlApp);
				_co.Add(new ControllersObjects(Convert.ToUInt32(controller), rooms));
			}
		}

		private static void ReleaseObject(object obj)
		{
			try
			{
				System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
				obj = null;
			}
			catch (Exception ex)
			{
				obj = null;
				//Console.WriteLine("Exception Occured while releasing object " + ex.ToString());
			}
			finally
			{
				GC.Collect();
			}
		}

		private void StartProvider()
		{
			Bacnet = new BacNet(IP);
			Bacnet.NetworkModelChangedEvent += OnNetworkModelChanged;
		}

		private void GeneratePrograms()
		{
			SavedPGTextBlockVisibility = Visibility.Collapsed;
			foreach (var controllerObjects in _co)
			{
				controllerObjects.CreateAllPG(ProgramsPath);
			}
			SavedPGTextBlockVisibility = Visibility.Visible;
		}

		private void CreateAllObjects()
		{
			CreatedAllObjectsTextBlockVisibility = Visibility.Collapsed;
			foreach (var controllerObjects in _co)
			{
				controllerObjects.CreateAllObjects();
			}
			CreatedAllObjectsTextBlockVisibility = Visibility.Visible;
		}

		private void OnNetworkModelChanged()
		{

			Devices = new ObservableCollection<BacNetDevice>(Bacnet.OnlineDevices);
		}

		private string _ip;
		public string IP
		{
			get { return _ip; }
			set
			{
				if (_ip == value) return;
				_ip = value;
				RaisePropertyChanged("IP");
			}
		}

		private string _programsPath;
		public string ProgramsPath
		{
			get { return _programsPath; }
			set
			{
				if (_programsPath == value) return;
				_programsPath = value;
				RaisePropertyChanged("ProgramsPath");
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
					RaisePropertyChanged("DeviceCount");
				}
			}
		}

		public int DeviceCount
		{
			get
			{
				return _devices != null ? _devices.Count : 0;
			}
		}

		private Visibility _savedPGTextBlockVisibility;
		public Visibility SavedPGTextBlockVisibility
		{
			get { return _savedPGTextBlockVisibility; }
			set
			{
				if (_savedPGTextBlockVisibility == value) return;
				_savedPGTextBlockVisibility = value;
				RaisePropertyChanged("SavedPGTextBlockVisibility");
			}

		}

		private Visibility _createdAllObjectsTextBlockVisibility;
		public Visibility CreatedAllObjectsTextBlockVisibility
		{
			get { return _createdAllObjectsTextBlockVisibility; }
			set
			{
				if (_createdAllObjectsTextBlockVisibility == value) return;
				_createdAllObjectsTextBlockVisibility = value;
				RaisePropertyChanged("CreatedAllObjectsTextBlockVisibility");
			}

		}
	}
}
