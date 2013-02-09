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
			if (controlledObject.SetpointAddress != null)
				_network.GetObject(controlledObject.SetpointAddress).ValueChangedEvent += OnValueChanged;
			if (controlledObject.TemperatureSetpointAddress != null)
				_network.GetObject(controlledObject.TemperatureSetpointAddress).ValueChangedEvent += OnValueChanged;
			if (controlledObject.ShutterSetpointAddress != null)
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

			if (zoneWithChangedSetpoint != null)
			{
				zoneWithChangedSetpoint.SetpointValue = value;
				if (zoneWithChangedSetpoint.SetpointValue == "0")
				{
					zoneWithChangedSetpoint.OnOffValue = "0";
					WriteToNetwork(zoneWithChangedSetpoint.OnOffOutputAddress, zoneWithChangedSetpoint.OnOffValue);
				}
				else
				{
					if (zoneWithChangedSetpoint.OnOffValue == "0")
					{
						zoneWithChangedSetpoint.OnOffValue = "1";
						WriteToNetwork(zoneWithChangedSetpoint.OnOffOutputAddress, zoneWithChangedSetpoint.OnOffValue);
					}
					if (zoneWithChangedSetpoint.SetpointValue == "1")
					{
						WriteToNetwork(zoneWithChangedSetpoint.SetpointOutputAddress, "1");
					}
					if (zoneWithChangedSetpoint.SetpointValue == "2")
					{
						WriteToNetwork(zoneWithChangedSetpoint.SetpointOutputAddress, "4");
					}
					if (zoneWithChangedSetpoint.SetpointValue == "3")
					{
						WriteToNetwork(zoneWithChangedSetpoint.SetpointOutputAddress, "3");
					}
					if (zoneWithChangedSetpoint.SetpointValue == "4")
					{
						WriteToNetwork(zoneWithChangedSetpoint.SetpointOutputAddress, "2");
					}
				}
			}

			#endregion

			#region Temperature setpoint
			lock (this)
			{
				zoneWithChangedTemperatureSetpoint = _acZones.FirstOrDefault(l => l.TemperatureSetpointAddress == address);
			}
			if (zoneWithChangedTemperatureSetpoint != null)
			{
				zoneWithChangedTemperatureSetpoint.TemperatureSetpointValue = value;
				WriteToNetwork(zoneWithChangedTemperatureSetpoint.TemperatureSetpointOutputAddress,
				               zoneWithChangedTemperatureSetpoint.TemperatureSetpointValue);
			}

			#endregion

			#region Shutter setpoint
			lock (this)
			{
				zoneWithChangedShutterSetpoint = _acZones.FirstOrDefault(l => l.ShutterSetpointAddress == address);
			}
			if (zoneWithChangedShutterSetpoint != null)
			{
				zoneWithChangedShutterSetpoint.ShutterSetpointValue = value;
				WriteToNetwork(zoneWithChangedShutterSetpoint.ShutterSetpointOutputAddress,
				               zoneWithChangedShutterSetpoint.ShutterSetpointValue);
			}

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
			//первый этаж
			acZones.Add(new ACZone { OnOffOutputAddress = "8102.BO10501", SetpointAddress = "1300.AV41127", SetpointOutputAddress = "8102.MO10507", TemperatureSetpointAddress = "1300.AV11127", TemperatureSetpointOutputAddress = "8102.AV10510" }); //110(127)
			acZones.Add(new ACZone { OnOffOutputAddress = "8102.BO10601", SetpointAddress = "1300.AV41113", SetpointOutputAddress = "8102.MO10607", TemperatureSetpointAddress = "1300.AV11113", TemperatureSetpointOutputAddress = "8102.AV10610" }); //111(113)
			//второй этаж
			//2300
			acZones.Add(new ACZone { OnOffOutputAddress = "8102.BO10301", SetpointAddress = "2300.AV41229", SetpointOutputAddress = "8102.MO10307", TemperatureSetpointAddress = "2300.AV11229", TemperatureSetpointOutputAddress = "8102.AV10310", ShutterSetpointAddress = "2300.AV42229", ShutterSetpointOutputAddress = "8102.MO10322"});//202A(229)
			acZones.Add(new ACZone { OnOffOutputAddress = "8102.BO10401", SetpointAddress = "2300.AV41201", SetpointOutputAddress = "8102.MO10407", TemperatureSetpointAddress = "2300.AV11201", TemperatureSetpointOutputAddress = "8102.AV10410", ShutterSetpointAddress = "2300.AV42201", ShutterSetpointOutputAddress = "8102.MO10422" });//201A(201)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO13601", SetpointAddress = "2300.AV41210", SetpointOutputAddress = "8101.MO13607", TemperatureSetpointAddress = "2300.AV11210", TemperatureSetpointOutputAddress = "8101.AV13610" });//231A(210)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO13501", SetpointAddress = "2300.AV41211", SetpointOutputAddress = "8101.MO13507", TemperatureSetpointAddress = "2300.AV11211", TemperatureSetpointOutputAddress = "8101.AV13510" });//212A(211)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO13401", SetpointAddress = "2300.AV41213", SetpointOutputAddress = "8101.MO13407", TemperatureSetpointAddress = "2300.AV11213", TemperatureSetpointOutputAddress = "8101.AV13410" });//(213)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO13301", SetpointAddress = "2300.AV41219", SetpointOutputAddress = "8101.MO13307", TemperatureSetpointAddress = "2300.AV11219", TemperatureSetpointOutputAddress = "8101.AV13310" });//215(219)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO13201", SetpointAddress = "2300.AV41220", SetpointOutputAddress = "8101.MO13207", TemperatureSetpointAddress = "2300.AV11220", TemperatureSetpointOutputAddress = "8101.AV13210" });//217(220)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO13101", SetpointAddress = "2300.AV41221", SetpointOutputAddress = "8101.MO13107", TemperatureSetpointAddress = "2300.AV11221", TemperatureSetpointOutputAddress = "8101.AV13110" });//(221)
			//2400
			acZones.Add(new ACZone { OnOffOutputAddress = "8102.BO10201", SetpointAddress = "2400.AV41231", SetpointOutputAddress = "8102.MO10207", TemperatureSetpointAddress = "2400.AV11231", TemperatureSetpointOutputAddress = "8102.AV10210", ShutterSetpointAddress = "2400.AV42231", ShutterSetpointOutputAddress = "8102.MO10222" });//205A(231)
			acZones.Add(new ACZone { OnOffOutputAddress = "8102.BO10101", SetpointAddress = "2400.AV412341", SetpointOutputAddress = "8102.MO10107", TemperatureSetpointAddress = "2400.AV112341", TemperatureSetpointOutputAddress = "8102.AV10110", ShutterSetpointAddress = "2400.AV422341", ShutterSetpointOutputAddress = "8102.MO10122" });//206A(2341)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12401", SetpointAddress = "2400.AV41237", SetpointOutputAddress = "8101.MO12407", TemperatureSetpointAddress = "2400.AV11237", TemperatureSetpointOutputAddress = "8101.AV12410" });//208A(237)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12501", SetpointAddress = "2400.AV41238", SetpointOutputAddress = "8101.MO12507", TemperatureSetpointAddress = "2400.AV11238", TemperatureSetpointOutputAddress = "8101.AV12510" });//210A(238)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12601", SetpointAddress = "2400.AV41241", SetpointOutputAddress = "8101.MO12607", TemperatureSetpointAddress = "2400.AV11241", TemperatureSetpointOutputAddress = "8101.AV12610" });//211A(241)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12701", SetpointAddress = "2400.AV41224", SetpointOutputAddress = "8101.MO12707", TemperatureSetpointAddress = "2400.AV11224", TemperatureSetpointOutputAddress = "8101.AV12710" });//(224)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12801", SetpointAddress = "2400.AV41218", SetpointOutputAddress = "8101.MO12807", TemperatureSetpointAddress = "2400.AV11218", TemperatureSetpointOutputAddress = "8101.AV12810" });//207(218)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12901", SetpointAddress = "2400.AV41217", SetpointOutputAddress = "8101.MO12907", TemperatureSetpointAddress = "2400.AV11217", TemperatureSetpointOutputAddress = "8101.AV12910" });//209(217)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO13001", SetpointAddress = "2400.AV41216", SetpointOutputAddress = "8101.MO13007", TemperatureSetpointAddress = "2400.AV11216", TemperatureSetpointOutputAddress = "8101.AV13010" });//(216)
			//третий этаж
			//3300
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12201", SetpointAddress = "3300.AV41301", SetpointOutputAddress = "8101.MO12207", TemperatureSetpointAddress = "3300.AV11301", TemperatureSetpointOutputAddress = "8101.AV12210" });//301A(301)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12301", SetpointAddress = "3300.AV41309", SetpointOutputAddress = "8101.MO12307", TemperatureSetpointAddress = "3300.AV11309", TemperatureSetpointOutputAddress = "8101.AV12310" });//314A(309)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12101", SetpointAddress = "3300.AV41310", SetpointOutputAddress = "8101.MO12107", TemperatureSetpointAddress = "3300.AV11310", TemperatureSetpointOutputAddress = "8101.AV12110" });//313A(310)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO12001", SetpointAddress = "3300.AV41312", SetpointOutputAddress = "8101.MO12007", TemperatureSetpointAddress = "3300.AV11312", TemperatureSetpointOutputAddress = "8101.AV12010" });//(312)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11901", SetpointAddress = "3300.AV41318", SetpointOutputAddress = "8101.MO11907", TemperatureSetpointAddress = "3300.AV11318", TemperatureSetpointOutputAddress = "8101.AV11910" });//315(318)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11801", SetpointAddress = "3300.AV41319", SetpointOutputAddress = "8101.MO11807", TemperatureSetpointAddress = "3300.AV11319", TemperatureSetpointOutputAddress = "8101.AV11810" });//317(319)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11701", SetpointAddress = "3300.AV41320", SetpointOutputAddress = "8101.MO11707", TemperatureSetpointAddress = "3300.AV11320", TemperatureSetpointOutputAddress = "8101.AV11710" });//(320)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11201", SetpointAddress = "3300.AV41328", SetpointOutputAddress = "8101.MO11207", TemperatureSetpointAddress = "3300.AV11328", TemperatureSetpointOutputAddress = "8101.AV11210" });//302A(328)
			//3400
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10701", SetpointAddress = "3400.AV41330", SetpointOutputAddress = "8101.MO10707", TemperatureSetpointAddress = "3400.AV11330", TemperatureSetpointOutputAddress = "8101.AV10710" });//305A(330)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10801", SetpointAddress = "3400.AV41335", SetpointOutputAddress = "8101.MO10807", TemperatureSetpointAddress = "3400.AV11335", TemperatureSetpointOutputAddress = "8101.AV10810" });//307A(335)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10901", SetpointAddress = "3400.AV41338", SetpointOutputAddress = "8101.MO10907", TemperatureSetpointAddress = "3400.AV11338", TemperatureSetpointOutputAddress = "8101.AV10910" });//309A(338)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11001", SetpointAddress = "3400.AV41339", SetpointOutputAddress = "8101.MO11007", TemperatureSetpointAddress = "3400.AV11339", TemperatureSetpointOutputAddress = "8101.AV11010" });//311A(339)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11101", SetpointAddress = "3400.AV41342", SetpointOutputAddress = "8101.MO11107", TemperatureSetpointAddress = "3400.AV11342", TemperatureSetpointOutputAddress = "8101.AV11110" });//312A(342)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11301", SetpointAddress = "3400.AV41323", SetpointOutputAddress = "8101.MO11307", TemperatureSetpointAddress = "3400.AV11323", TemperatureSetpointOutputAddress = "8101.AV11310" });//(323)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11401", SetpointAddress = "3400.AV41317", SetpointOutputAddress = "8101.MO11407", TemperatureSetpointAddress = "3400.AV11317", TemperatureSetpointOutputAddress = "8101.AV11410" });//308(317)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11501", SetpointAddress = "3400.AV41316", SetpointOutputAddress = "8101.MO11507", TemperatureSetpointAddress = "3400.AV11316", TemperatureSetpointOutputAddress = "8101.AV11510" });//310(316)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO11601", SetpointAddress = "3400.AV41315", SetpointOutputAddress = "8101.MO11607", TemperatureSetpointAddress = "3400.AV11315", TemperatureSetpointOutputAddress = "8101.AV11610" });//(315)
			//четвертый этаж
			//4300
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10501", SetpointAddress = "4300.AV41416", SetpointOutputAddress = "8101.MO10507", TemperatureSetpointAddress = "4300.AV11416", TemperatureSetpointOutputAddress = "8101.AV10510" });//412(416)
			//4400
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10101", SetpointAddress = "4400.AV41425", SetpointOutputAddress = "8101.MO10107", TemperatureSetpointAddress = "4400.AV11425", TemperatureSetpointOutputAddress = "8101.AV10110" });//408(425)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10201", SetpointAddress = "4400.AV41427", SetpointOutputAddress = "8101.MO10207", TemperatureSetpointAddress = "4400.AV11427", TemperatureSetpointOutputAddress = "8101.AV10210" });//(427)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10301", SetpointAddress = "4400.AV41429", SetpointOutputAddress = "8101.MO10307", TemperatureSetpointAddress = "4400.AV11429", TemperatureSetpointOutputAddress = "8101.AV10310" });//410A(429)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10401", SetpointAddress = "4400.AV41431", SetpointOutputAddress = "8101.MO10407", TemperatureSetpointAddress = "4400.AV11431", TemperatureSetpointOutputAddress = "8101.AV10410" });//411(431)
			acZones.Add(new ACZone { OnOffOutputAddress = "8101.BO10601", SetpointAddress = "4400.AV41415", SetpointOutputAddress = "8101.MO10607", TemperatureSetpointAddress = "4400.AV11415", TemperatureSetpointOutputAddress = "8101.AV10610" });//412B(415)
			return acZones;
		}
	}
}
