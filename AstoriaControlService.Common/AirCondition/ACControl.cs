using System;
using System.Collections.Generic;
using System.Linq;
using BacNetApi;

namespace AstoriaControlService.Common.AirCondition
{
	public class ACControl : CommonControl
	{
		private readonly List<ACZone> _acZones = new List<ACZone>();
		private readonly BacNet _network;

		public ACControl(BacNet network, List<ACZone> ACZones)
		{
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Initializing...");
			_network = network;

			lock (this) { ACZones.ForEach(AddZone); }
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Initialized");
		}

		private void AddZone(ACZone controlledObject)
		{
			_network.GetObject(controlledObject.SetpointAddress).ValueChangedEvent += OnValueChanged;
			_network.GetObject(controlledObject.TemperatureSetpointAddress).ValueChangedEvent += OnValueChanged;
			_network.GetObject(controlledObject.ShutterSetpointAddress).ValueChangedEvent += OnValueChanged;
			_acZones.Add(controlledObject);
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Added zone at " + controlledObject.SetpointAddress);
		}

		private void RemoveZone(ACZone controlledObject)
		{
			_network.GetObject(controlledObject.SetpointAddress).ValueChangedEvent -= OnValueChanged;
			_network.GetObject(controlledObject.TemperatureSetpointAddress).ValueChangedEvent -= OnValueChanged;
			_network.GetObject(controlledObject.ShutterSetpointAddress).ValueChangedEvent -= OnValueChanged;
			_acZones.Remove(controlledObject);
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Removed zone at " + controlledObject.SetpointAddress);
		}

		public void Unsubscribe()
		{
			lock (this) { _acZones.ForEach(RemoveZone); }
		}

		public void Resubscribe(List<ACZone> acZones)
		{
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Resubscribing...");
			lock (this)
			{
				var zonesToRemove = _acZones.Where(l => !acZones.Contains(l)).ToList();
				zonesToRemove.ForEach(RemoveZone);

				var zonesToAdd = acZones.Where(l => !_acZones.Contains(l)).ToList();
				zonesToAdd.ForEach(AddZone);
			}
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Resubscribed");
		}

		public List<ACZone> Subscriptions()
		{
			List<ACZone> result;
			lock (this)
			{
				result = new List<ACZone>(_acZones);
			}
			return result;
		}

		public void OnValueChanged(string address, string value)
		{
			ACZone zoneWithChangedSetpoint;
			ACZone zoneWithChangedTemperatureSetpoint;
			ACZone zoneWithChangedShutterSetpoint;

			#region setpoint
			lock (this)
			{
				zoneWithChangedSetpoint = _acZones.FirstOrDefault(l => l.SetpointAddress == address);
			}

			if (zoneWithChangedSetpoint == null) return;

			zoneWithChangedSetpoint.SetpointValue = value;
			if (zoneWithChangedSetpoint.SetpointValue == "0")
			{
				zoneWithChangedSetpoint.OnOffValue = "0";
				WriteToNetwork(zoneWithChangedSetpoint.OnOffOutputAddress, zoneWithChangedSetpoint.OnOffValue);
				return;
			}
			if (zoneWithChangedSetpoint.OnOffValue == "0")
			{
				zoneWithChangedSetpoint.OnOffValue = "1";
				WriteToNetwork(zoneWithChangedSetpoint.OnOffOutputAddress, zoneWithChangedSetpoint.OnOffValue);
			}
			if (zoneWithChangedSetpoint.SetpointValue == "1")
			{
				WriteToNetwork(zoneWithChangedSetpoint.SetpointOutputAddress, "1");
				return;
			}
			if (zoneWithChangedSetpoint.SetpointValue == "2")
			{
				WriteToNetwork(zoneWithChangedSetpoint.SetpointOutputAddress, "4");
				return;
			}
			if (zoneWithChangedSetpoint.SetpointValue == "3")
			{
				WriteToNetwork(zoneWithChangedSetpoint.SetpointOutputAddress, "3");
				return;
			}
			if (zoneWithChangedSetpoint.SetpointValue == "4")
			{
				WriteToNetwork(zoneWithChangedSetpoint.SetpointOutputAddress, "2");
				return;
			}
			#endregion

			#region Temperature setpoint
			lock (this)
			{
				zoneWithChangedTemperatureSetpoint = _acZones.FirstOrDefault(l => l.TemperatureSetpointAddress == address);
			}
			if (zoneWithChangedTemperatureSetpoint == null) return;
			zoneWithChangedTemperatureSetpoint.TemperatureSetpointValue = value;
			WriteToNetwork(zoneWithChangedTemperatureSetpoint.TemperatureSetpointOutputAddress, zoneWithChangedTemperatureSetpoint.TemperatureSetpointValue);
			#endregion

			#region Shutter setpoint
			lock (this)
			{
				zoneWithChangedShutterSetpoint = _acZones.FirstOrDefault(l => l.ShutterSetpointAddress == address);
			}
			if (zoneWithChangedShutterSetpoint == null) return;
			zoneWithChangedShutterSetpoint.ShutterSetpointValue = value;
			WriteToNetwork(zoneWithChangedShutterSetpoint.ShutterSetpointOutputAddress, zoneWithChangedShutterSetpoint.ShutterSetpointValue);
			#endregion

			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, string.Format("New value at {0}: {1}", address, value));
		}

		private void WriteToNetwork(string address, string value)
		{
			var addressList = address.Split('.');
			uint dev;
			if (addressList.Length != 2 || !uint.TryParse(addressList[0], out dev))
				throw new Exception("проверь как забил адреса");
			_network[dev].Objects[addressList[1]].BeginSet(value);
		}

		public static List<ACZone> InitACZones()
		{
			var acZones = new List<ACZone>();
			return acZones;
		}
	}
}
