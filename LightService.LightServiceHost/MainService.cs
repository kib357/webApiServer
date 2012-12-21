using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Xml.Serialization;
using BacNetApi;
using LightService.Common;
using LightService.ControlService;

namespace LightService.LightServiceHost
{
	public partial class MainService : ServiceBase
	{
		private const string _lightzonesXml = "lightZones.xml";
		private BacNet _network;
		private LightControl _control;
		private Thread _thread;
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
			var ip = ConfigurationManager.AppSettings["BacNetIp"];

			List<LightZone> zones = null;
			try
			{
				if (File.Exists(_lightzonesXml))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(List<LightZone>));
					using (var stream = File.OpenRead(_lightzonesXml))
					{
						zones = (List<LightZone>)serializer.Deserialize(stream);
					}
				}
			}
			catch (Exception)
			{
			}
			if (zones == null)
				zones = LightControl.InitLightZones();

			_network = new BacNet(ip);
			_control = new LightControl(_network, zones);

			_lightService = new ServiceControl(_control);
			_serviceHost = new ServiceHost(_lightService);
			_serviceHost.Open();
		}

		protected override void OnStop()
		{
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
