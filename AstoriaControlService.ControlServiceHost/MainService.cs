using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Xml.Serialization;
using AstoriaControlService.Common;
using AstoriaControlService.Common.AirCondition;
using AstoriaControlService.Common.Light;
using AstoriaControlService.ControlService;
using BacNetApi;

namespace AstoriaControlService.ControlServiceHost
{
	public partial class MainService : ServiceBase
	{
		private const string LightzonesXml = "lightZones.xml";
		private const string ACXml = "acZones.xml";
		private BacNet _network;
		private LightControl _lightControl;
		private ACControl _acControl;
		private Thread _thread;
		private ServiceControl _serviceControl;
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
				CommonControl.HasConsole = true;

			if (_serviceHost != null)
				_serviceHost.Close();

			_thread = new Thread(ProcessBacNet) { Name = "Light worker", IsBackground = true };
			_thread.Start();
			if (CommonControl.HasConsole)
			{
				Console.ReadLine();
			}
		}

		private void ProcessBacNet()
		{
			var ip = ConfigurationManager.AppSettings["BacNetIp"];

			List<LightZone> lightZones = null;
			List<ACZone> acZones = null;
			try
			{
				if (File.Exists(LightzonesXml))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(List<LightZone>));
					using (var stream = File.OpenRead(LightzonesXml))
					{
						lightZones = (List<LightZone>)serializer.Deserialize(stream);
					}
				}
			}
			catch (Exception)
			{
			}
			try
			{
				if (File.Exists(ACXml))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(List<ACZone>));
					using (var stream = File.OpenRead(ACXml))
					{
						acZones = (List<ACZone>)serializer.Deserialize(stream);
					}
				}
			}
			catch (Exception)
			{
			}
			if (lightZones == null)
				lightZones = LightControl.InitLightZones();
			if (acZones == null)
				acZones = ACControl.InitACZones();

			_network = new BacNet(ip);
			_lightControl = new LightControl(_network, lightZones);
			_acControl = new ACControl(_network, acZones);

			_serviceControl = new ServiceControl(_lightControl, _acControl);
			_serviceHost = new ServiceHost(_serviceControl);
			_serviceHost.Open();
		}

		protected override void OnStop()
		{
			if (_lightControl != null)
				_lightControl.Unsubscribe();

			if (_serviceHost == null) return;
			_serviceHost.Close();
			_serviceHost = null;
		}

		public void Close()
		{
			OnStop();
		}
	}
}
