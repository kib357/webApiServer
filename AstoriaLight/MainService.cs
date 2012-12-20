using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BacNetApi;
using BacNetApi.Data;

namespace AstoriaLight
{
	public partial class MainService : ServiceBase
	{
		private static BacNet _network;
		private static LightControl _control;
		
		public MainService()
		{
			InitializeComponent();
		}

		public void Start()
		{
			OnStart(null);
		}

		protected override void OnStart(string[] args)
		{
			//Thread.Sleep(20000);			

			var ip = "10.81.32.199";//ConfigurationManager.AppSettings["BacNetIp"];
			var controlledObjects = LightControl.InitLightZones();

			_network = new BacNet(ip);
			_control = new LightControl(_network, controlledObjects);
			SubscribeToObjects(controlledObjects);
		}

		private void SubscribeToObjects(IEnumerable<LightZone> controlledObjects)
		{
			foreach (var controlledObject in controlledObjects)
			{
				_network.GetObject(controlledObject.InputAddress).ValueChangedEvent += OnValueChangedEvent;
				_network.GetObject(controlledObject.SetPointAddress).ValueChangedEvent += OnValueChangedEvent;
				//controlledObject.OutputAlarmAddresses.ForEach(e => _network.GetObject(e).ValueChangedEvent += OnValueChangedEvent);
				//controlledObject.OutputAddresses.ForEach(e => _network.GetObject(e).ValueChangedEvent += OnValueChangedEvent);
			}
		}

		private void OnValueChangedEvent(string address, string value)
		{
			_control.OnValueChanged(address, value);
		}

		protected override void OnStop()
		{
			//  unsubscribe
		}
	}

	public static class Helper
	{
		public static PrimitiveObject GetObject(this BacNet net, string address)
		{
			var addressList = address.Split('.');
			uint dev;
			if (addressList.Length != 2 || !uint.TryParse(addressList[0], out dev))
				throw new Exception("проверь как забил адреса");
			return net[dev].Objects[addressList[1]];
		}
	}
}
