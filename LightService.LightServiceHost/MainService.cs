using System;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using BacNetApi;
using LightService.ControlService;
using LigtService.Common;

namespace LightService.LightServiceHost
{
	public partial class MainService : ServiceBase
	{
		private BacNet _network;
		private LightControl _control;
		private readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
		private Thread _thread;
		private volatile bool _workerStarted;
		private ServiceControl _lightService;
		private ServiceHost _serviceHost = null;

		public MainService()
		{
			InitializeComponent();
		}

		public void Start(string[] args)
		{
			OnStart(args);
		}

		protected override void OnStart(string[] args)
		{
			if (args != null && args.Any() && args[0] == "/c")
				LightControl.HasConsole = true;

			if (_serviceHost != null)
				_serviceHost.Close();

			_thread = new Thread(ProcessBacNet) { Name = "Light worker", IsBackground = true };
			_thread.Start();

			if (LightControl.HasConsole)
			{
				Console.ReadLine();
			}
		}

		private void ProcessBacNet()
		{
			while (!_shutdownEvent.WaitOne(0))
			{
				if (!_workerStarted)
				{
					_workerStarted = true;

					var ip = ConfigurationManager.AppSettings["BacNetIp"];
					var controlledObjects = LightControl.InitLightZones();

					_network = new BacNet(ip);
					_control = new LightControl(_network, controlledObjects);

					_lightService = new ServiceControl(_control);
					_serviceHost = new ServiceHost(_lightService);
					_serviceHost.Open();
				}
			}
		}

		protected override void OnStop()
		{
			_shutdownEvent.Set();

			if (!_thread.Join(3000))
			{
				_thread.Abort();
			}

			if (_control != null)
				_control.Unsubscribe();

			if (_serviceHost != null)
			{
				_serviceHost.Close();
				_serviceHost = null;
			}
		}

		public void Close()
		{
			OnStop();
		}
	}
}
