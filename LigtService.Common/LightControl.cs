using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BacNetApi;

namespace LigtService.Common
{
	public class LightControl
	{
		private readonly List<LightZone> _lightZones = new List<LightZone>();
		private readonly BacNet _network;
		public static volatile bool HasConsole;

		public LightControl(BacNet network, List<LightZone> lightZones)
		{
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Initializing...");
			_network = network;

			lock (this) { lightZones.ForEach(AddZone); }
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Initialized");
		}

		private void AddZone(LightZone controlledObject)
		{
			_network.GetObject(controlledObject.InputAddress).ValueChangedEvent += OnValueChanged;
			_network.GetObject(controlledObject.SetPointAddress).ValueChangedEvent += OnValueChanged;
			_lightZones.Add(controlledObject);
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Added zone at " + controlledObject.InputAddress);
		}

		private void RemoveZone(LightZone controlledObject)
		{
			_network.GetObject(controlledObject.InputAddress).ValueChangedEvent -= OnValueChanged;
			_network.GetObject(controlledObject.SetPointAddress).ValueChangedEvent -= OnValueChanged;
			_lightZones.Remove(controlledObject);
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Removed zone at " + controlledObject.InputAddress);
		}

		public void Unsubscribe()
		{
			lock (this) { _lightZones.ForEach(RemoveZone); }
		}

		public void Resubscribe(List<LightZone> lightZones)
		{
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Resubscribing...");
			lock (this)
			{
				var zonesToRemove = _lightZones.Where(l => !lightZones.Contains(l)).ToList();
				zonesToRemove.ForEach(RemoveZone);

				var zonesToAdd = lightZones.Where(l => !_lightZones.Contains(l)).ToList();
				zonesToAdd.ForEach(AddZone);
			}
			if (HasConsole)
				Console.WriteLine("{0:H:mm:ss:ffff}: {1}", DateTime.Now, "Resubscribed");
		}

		public List<LightZone> Subscriptions()
		{
			List<LightZone> result;
			lock (this)
			{
				result = new List<LightZone>(_lightZones);
			}
			return result;
		}

		public void OnValueChanged(string address, string value)
		{
			LightZone zoneWithChangedSetpoint;
			LightZone zoneWithChangedInput;
			lock (this)
			{
				zoneWithChangedSetpoint = _lightZones.FirstOrDefault(l => l.SetPointAddress == address);
			}

			if (zoneWithChangedSetpoint != null)
			{
				zoneWithChangedSetpoint.SetPointValue = value;
				if (zoneWithChangedSetpoint.InputValue == "1")
				{
					foreach (var output in zoneWithChangedSetpoint.OutputAddresses.Where(o => o.Contains("AO")))
					{
						WriteToNetwork(output, zoneWithChangedSetpoint.SetPointValue);
					}
				}
				return;
			}

			lock (this)
			{
				zoneWithChangedInput = _lightZones.FirstOrDefault(l => l.InputAddress == address);
			}

			if (zoneWithChangedInput == null) return;

			zoneWithChangedInput.InputValue = value;
			string setpoint;
			if (zoneWithChangedInput.InputValue == "1")
			{
				setpoint = string.IsNullOrWhiteSpace(zoneWithChangedInput.SetPointValue)
							   ? "100"
							   : zoneWithChangedInput.SetPointValue;
				foreach (var output in zoneWithChangedInput.OutputAddresses.Where(o => o.Contains("AO")))
				{
					WriteToNetwork(output, setpoint);
				}
			}
			else
			{
				setpoint = "0";
				foreach (var output in zoneWithChangedInput.OutputAddresses)
				{
					WriteToNetwork(output, setpoint);
				}

				if (zoneWithChangedInput.OutputAlarmAddresses != null && zoneWithChangedInput.OutputAlarmAddresses.Count > 0)
					foreach (var alarmAddress in zoneWithChangedInput.OutputAlarmAddresses)
						WriteToNetwork(alarmAddress, "100");
			}
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

		public static List<LightZone> InitLightZones()
		{
			var lightZones = new List<LightZone>();
			/*_lightZones.Add(new LightZone { InputAddress = "200.BV1", OutputAddresses = new List<string> { "17811.AO111" }, SetPointAddress = "200.AV2" });
			_lightZones.Add(new LightZone { InputAddress = "200.BV2", OutputAddresses = new List<string> { "17811.AO112" }, SetPointAddress = "200.AV2" });
			_lightZones.Add(new LightZone { InputAddress = "200.BV3", OutputAddresses = new List<string> { "17811.AO113" }, SetPointAddress = "200.AV2" });
			_lightZones.Add(new LightZone { InputAddress = "200.BV4", OutputAddresses = new List<string> { "17811.AO114" }, SetPointAddress = "200.AV2" });
			_lightZones.Add(new LightZone { InputAddress = "200.BV5", OutputAddresses = new List<string> { "17811.AO115" }, SetPointAddress = "200.AV2" });
			_lightZones.Add(new LightZone { InputAddress = "200.BV6", OutputAddresses = new List<string> { "17811.AO116" }, SetPointAddress = "200.AV2" });*/

			// Контроллер 1400 кабинеты
			lightZones.Add(new LightZone { InputAddress = "1400.BV1102", OutputAddresses = new List<string> { "17811.AO1104" }, SetPointAddress = "100.AV10231" }); //102
			lightZones.Add(new LightZone { InputAddress = "1400.BV1101", OutputAddresses = new List<string> { "17811.AO1103" }, SetPointAddress = "1400.AV1101" }); //103(101)
			lightZones.Add(new LightZone { InputAddress = "1400.BV1108", OutputAddresses = new List<string> { "17811.AO1101" }, SetPointAddress = "1400.AV1108" }); //105(131)
			lightZones.Add(new LightZone { InputAddress = "1400.BV1106", OutputAddresses = new List<string> { "17811.AO1000" }, SetPointAddress = "1400.AV1106" }); //110(127)
			lightZones.Add(new LightZone { InputAddress = "1400.BV1107", OutputAddresses = new List<string> { "17811.AO1102" }, SetPointAddress = "1400.AV1107" }); //104(130)
			lightZones.Add(new LightZone { InputAddress = "1400.BV1302", OutputAddresses = new List<string> { "17811.AO1001" }, SetPointAddress = "1400.AV1302" }); //111(113)
			lightZones.Add(new LightZone { InputAddress = "1400.BV1304", OutputAddresses = new List<string> { "17811.AO1002" }, SetPointAddress = "1400.AV1304" }); //145
			lightZones.Add(new LightZone { InputAddress = "1400.BV1203", OutputAddresses = new List<string> { "17811.AO1003", "1400.BV1304", "1400.BV1205", "1400.BV1206", "1400.BV1204" }, SetPointAddress = "1400.AV1203" }); //138
			lightZones.Add(new LightZone { InputAddress = "1400.BV1206", OutputAddresses = new List<string> { "17811.AO1004" }, SetPointAddress = "1400.AV1206" }); //144
			lightZones.Add(new LightZone { InputAddress = "1400.BV1305", OutputAddresses = new List<string> { "17811.AO1005" }, SetPointAddress = "1400.AV1305" }); //146
			lightZones.Add(new LightZone { InputAddress = "1400.BV1301", OutputAddresses = new List<string> { "17811.AO1006" }, SetPointAddress = "1400.AV1301" }); //143
			lightZones.Add(new LightZone { InputAddress = "1400.BV1208", OutputAddresses = new List<string> { "17811.AO1007" }, SetPointAddress = "1400.AV1208" }); //142
			lightZones.Add(new LightZone { InputAddress = "1400.BV1207", OutputAddresses = new List<string> { "17811.AO1008" }, SetPointAddress = "1400.AV1207" }); //141
			lightZones.Add(new LightZone { InputAddress = "1400.BV1204", OutputAddresses = new List<string> { "17811.AO1009", "1400.BV1207", "1400.BV1208" }, SetPointAddress = "1400.AV1204" }); //140
			lightZones.Add(new LightZone { InputAddress = "1400.BV1205", OutputAddresses = new List<string> { "17811.AO1010" }, SetPointAddress = "1400.AV1205" }); //139
			//// Контроллер 1400 коридоры
			lightZones.Add(new LightZone { InputAddress = "1400.BV1306", OutputAddresses = new List<string> { "17811.AO1105" }, OutputAlarmAddresses = new List<string> { "17811.AO1108" }, SetPointAddress = "1400.AV1306" }); //132
			lightZones.Add(new LightZone { InputAddress = "1400.BV1307", OutputAddresses = new List<string> { "17811.AO1106" }, OutputAlarmAddresses = new List<string> { "17811.AO1110" }, SetPointAddress = "1400.AV1307" }); //132a
			// Контроллер 1300 кабинеты
			lightZones.Add(new LightZone { InputAddress = "1300.BV1101", OutputAddresses = new List<string> { "17812.AO68097" }, SetPointAddress = "1300.AV1101" }); //101(104)
			lightZones.Add(new LightZone { InputAddress = "1300.BV1106", OutputAddresses = new List<string> { "17812.AO68352" }, SetPointAddress = "1300.AV1106" }); //114a(122a)
			lightZones.Add(new LightZone { InputAddress = "1300.BV1209", OutputAddresses = new List<string> { "17812.AO68353" }, SetPointAddress = "1300.AV1106" }); //114b(122b)
			lightZones.Add(new LightZone { InputAddress = "1300.BV1103", OutputAddresses = new List<string> { "17812.AO68608" }, SetPointAddress = "1300.AV1103" }); //118(109)
			lightZones.Add(new LightZone { InputAddress = "1300.BV1104", OutputAddresses = new List<string> { "17812.AO68096" }, SetPointAddress = "1300.AV1104" }); //113(107b)
			lightZones.Add(new LightZone { InputAddress = "1300.BV1105", OutputAddresses = new List<string> { "17812.AO68612" }, SetPointAddress = "1300.AV1105" }); //116(112)
			lightZones.Add(new LightZone { InputAddress = "1300.BV1201", OutputAddresses = new List<string> { "17812.AO68611" }, SetPointAddress = "1300.AV1201" }); //115(114)
			lightZones.Add(new LightZone { InputAddress = "1300.BV1202", OutputAddresses = new List<string> { "17812.AO68101" }, SetPointAddress = "1300.AV1202" }); //UPS(120)
			lightZones.Add(new LightZone { InputAddress = "1300.BV1102", OutputAddresses = new List<string> { "17812.AO68609" }, SetPointAddress = "1300.AV1102" }); //117(110)
			//// Контроллер 1300 коридоры
			lightZones.Add(new LightZone { InputAddress = "1300.BV1210", OutputAddresses = new List<string> { "17812.AO68104" }, OutputAlarmAddresses = new List<string> { "17812.AO68103" }, SetPointAddress = "1300.AV1210" }); //115
			lightZones.Add(new LightZone { InputAddress = "1300.BV1211", OutputAddresses = new List<string> { "17812.AO68610" }, OutputAlarmAddresses = new List<string> { "17812.AO68614" }, SetPointAddress = "1300.AV1211" }); //111
			// Контроллер 2300 кабинеты
			lightZones.Add(new LightZone { InputAddress = "2300.BV1101", OutputAddresses = new List<string> { "17822.AO68352" }, SetPointAddress = "2300.AV1101" }); //218(205)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1102", OutputAddresses = new List<string> { "17822.AO68353" }, SetPointAddress = "2300.AV1102" }); //216(206)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1103", OutputAddresses = new List<string> { "17822.AO68354" }, SetPointAddress = "2300.AV1103" }); //214(208)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1104", OutputAddresses = new List<string> { "17822.AO68355" }, SetPointAddress = "2300.AV1104" }); //213(209)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1105", OutputAddresses = new List<string> { "17822.AO68356" }, SetPointAddress = "2300.AV1105" }); //213A(210)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1106", OutputAddresses = new List<string> { "17822.AO68611" }, SetPointAddress = "2300.AV1106" }); //212A(211)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1107", OutputAddresses = new List<string> { "17822.AO68612" }, SetPointAddress = "2300.AV1107" }); //212
			lightZones.Add(new LightZone { InputAddress = "2300.BV1302", OutputAddresses = new List<string> { "17822.AO68614" }, SetPointAddress = "2300.AV1302" }); //211(240)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1303", OutputAddresses = new List<string> { "17822.AO68613" }, SetPointAddress = "2300.AV1303" }); //211A(241)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1108", OutputAddresses = new List<string> { "17822.AO68357" }, SetPointAddress = "2300.AV1108" }); //213
			lightZones.Add(new LightZone { InputAddress = "2300.BV1202", OutputAddresses = new List<string> { "17822.AO68358", "2300.BV1108" }, SetPointAddress = "2300.AV1202" }); //215(219)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1204", OutputAddresses = new List<string> { "17822.AO68359", "2300.BV1206", "2300.BV1208" }, SetPointAddress = "2300.AV1204" }); //217(220)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1206", OutputAddresses = new List<string> { "17822.AO68360" }, SetPointAddress = "2300.AV1206" }); //221
			lightZones.Add(new LightZone { InputAddress = "2300.BV1208", OutputAddresses = new List<string> { "17822.AO68361" }, SetPointAddress = "2300.AV1208" }); //217C(227)
			lightZones.Add(new LightZone { InputAddress = "2300.BV1301", OutputAddresses = new List<string> { "17822.AO68615" }, SetPointAddress = "2300.AV1301" }); //210(239)
			// Контроллер 2300 коридоры
			lightZones.Add(new LightZone { InputAddress = "2300.BV1305", OutputAddresses = new List<string> { "17822.AO68097" }, OutputAlarmAddresses = new List<string> { "17822.AO68100" }, SetPointAddress = "2300.AV1304" }); //204a1
			lightZones.Add(new LightZone { InputAddress = "2300.BV1306", OutputAddresses = new List<string> { "17822.AO68096" }, OutputAlarmAddresses = new List<string> { "17822.AO68101" }, SetPointAddress = "2300.AV1305" }); //204a2
			lightZones.Add(new LightZone { InputAddress = "2300.BV1307", OutputAddresses = new List<string> { "17822.AO68099" }, OutputAlarmAddresses = new List<string> { "17822.AO68102" }, SetPointAddress = "2300.AV1306" }); //2041
			lightZones.Add(new LightZone { InputAddress = "2300.BV1308", OutputAddresses = new List<string> { "17822.AO68098", "17822.AO68608" }, OutputAlarmAddresses = new List<string> { "17822.AO68103", "17822.AO68616" }, SetPointAddress = "2300.AV1307" }); //2042
			lightZones.Add(new LightZone { InputAddress = "2300.BV1309", OutputAddresses = new List<string> { "17822.AO68609" }, OutputAlarmAddresses = new List<string> { "17822.AO68617" }, SetPointAddress = "2300.AV1308" }); //204b1
			lightZones.Add(new LightZone { InputAddress = "2300.BV1310", OutputAddresses = new List<string> { "17822.AO68610" }, OutputAlarmAddresses = new List<string> { "17822.AO68618" }, SetPointAddress = "2300.AV1309" }); //204b2
			// Контроллер 2400 кабинеты
			lightZones.Add(new LightZone { InputAddress = "2400.BV1", OutputAddresses = new List<string> { "17821.AO1203" }, SetPointAddress = "2400.AV1" }); //201A(201)
			lightZones.Add(new LightZone { InputAddress = "2400.BV2", OutputAddresses = new List<string> { "17821.AO1204" }, SetPointAddress = "2400.AV2" }); //201(202)
			lightZones.Add(new LightZone { InputAddress = "2400.BV13", OutputAddresses = new List<string> { "17821.AO1202" }, SetPointAddress = "2400.AV13" }); //202(228)
			lightZones.Add(new LightZone { InputAddress = "2400.BV14", OutputAddresses = new List<string> { "17821.AO1201" }, SetPointAddress = "2400.AV14" }); //202A(229)
			lightZones.Add(new LightZone { InputAddress = "2400.BV16", OutputAddresses = new List<string> { "17821.AO1007" }, SetPointAddress = "2400.AV16" }); //205A(231)
			lightZones.Add(new LightZone { InputAddress = "2400.BV17", OutputAddresses = new List<string> { "17821.AO1006" }, SetPointAddress = "2400.AV17" }); //205(232)
			lightZones.Add(new LightZone { InputAddress = "2400.BV18", OutputAddresses = new List<string> { "17821.AO1005" }, SetPointAddress = "2400.AV18" }); //206(234)
			lightZones.Add(new LightZone { InputAddress = "2400.BV19", OutputAddresses = new List<string> { "17821.AO1107" }, SetPointAddress = "2400.AV19" }); //208(236)
			lightZones.Add(new LightZone { InputAddress = "2400.BV20", OutputAddresses = new List<string> { "17821.AO1106" }, SetPointAddress = "2400.AV20" }); //208A(237)
			lightZones.Add(new LightZone { InputAddress = "2400.BV21", OutputAddresses = new List<string> { "17821.AO1105" }, SetPointAddress = "2400.AV21" }); //210A(238)
			lightZones.Add(new LightZone { InputAddress = "2400.BV22", OutputAddresses = new List<string> { "17821.AO1004" }, SetPointAddress = "2400.AV22" }); //206A(234A)
			lightZones.Add(new LightZone { InputAddress = "2400.BV4", OutputAddresses = new List<string> { "17821.AO1003" }, SetPointAddress = "2400.AV4" }); //216
			lightZones.Add(new LightZone { InputAddress = "2400.BV5", OutputAddresses = new List<string> { "17821.AO1002", "2400.BV4" }, SetPointAddress = "2400.AV5" }); //209(217)
			lightZones.Add(new LightZone { InputAddress = "2400.BV7", OutputAddresses = new List<string> { "17821.AO1001", "2400.BV10" }, SetPointAddress = "2400.AV7" }); //207(218)
			lightZones.Add(new LightZone { InputAddress = "2400.BV10", OutputAddresses = new List<string> { "17821.AO1000" }, SetPointAddress = "2400.AV10" }); //224
			lightZones.Add(new LightZone { InputAddress = "2400.BV15", OutputAddresses = new List<string> { "17821.AO1200" }, SetPointAddress = "2400.AV15" }); //230
			// Контроллер 2400 коридоры
			lightZones.Add(new LightZone { InputAddress = "2400.BV25", OutputAddresses = new List<string> { "17821.AO1101" }, OutputAlarmAddresses = new List<string> { "17821.AO1109" }, SetPointAddress = "2400.AV24" }); //2351
			lightZones.Add(new LightZone { InputAddress = "2400.BV26", OutputAddresses = new List<string> { "17821.AO1100" }, OutputAlarmAddresses = new List<string> { "17821.AO1110" }, SetPointAddress = "2400.AV25" }); //2352
			lightZones.Add(new LightZone { InputAddress = "2400.BV27", OutputAddresses = new List<string> { "17821.AO1103" }, OutputAlarmAddresses = new List<string> { "17821.AO1111" }, SetPointAddress = "2400.AV26" }); //2353
			lightZones.Add(new LightZone { InputAddress = "2400.BV28", OutputAddresses = new List<string> { "17821.AO1108" }, OutputAlarmAddresses = new List<string> { "17821.AO1112" }, SetPointAddress = "2400.AV27" }); //2354
			// Контроллер 3300 кабинеты
			lightZones.Add(new LightZone { InputAddress = "3300.BV16", OutputAddresses = new List<string> { "17832.AO68615" }, SetPointAddress = "3300.AV16" }); //311(340)
			lightZones.Add(new LightZone { InputAddress = "3300.BV17", OutputAddresses = new List<string> { "17832.AO68614" }, SetPointAddress = "3300.AV17" }); //312(341)
			lightZones.Add(new LightZone { InputAddress = "3300.BV18", OutputAddresses = new List<string> { "17832.AO68613" }, SetPointAddress = "3300.AV18" }); //312A(342)
			lightZones.Add(new LightZone { InputAddress = "3300.BV6", OutputAddresses = new List<string> { "17832.AO68612" }, SetPointAddress = "3300.AV5" }); //313(311)
			lightZones.Add(new LightZone { InputAddress = "3300.BV5", OutputAddresses = new List<string> { "17832.AO68611" }, SetPointAddress = "3300.AV6" }); //313A(310)
			lightZones.Add(new LightZone { InputAddress = "3300.BV4", OutputAddresses = new List<string> { "17832.AO68355" }, SetPointAddress = "3300.AV4" }); //314A(309)
			lightZones.Add(new LightZone { InputAddress = "3300.BV3", OutputAddresses = new List<string> { "17832.AO68354" }, SetPointAddress = "3300.AV3" }); //314(308)
			lightZones.Add(new LightZone { InputAddress = "3300.BV2", OutputAddresses = new List<string> { "17832.AO68353" }, SetPointAddress = "3300.AV2" }); //316(306)
			lightZones.Add(new LightZone { InputAddress = "3300.BV1", OutputAddresses = new List<string> { "17832.AO68352" }, SetPointAddress = "3300.AV1" }); //318(305)
			lightZones.Add(new LightZone { InputAddress = "3300.BV9", OutputAddresses = new List<string> { "17832.AO68357", "3300.BV7" }, SetPointAddress = "3300.AV9" }); //315(318)
			lightZones.Add(new LightZone { InputAddress = "3300.BV7", OutputAddresses = new List<string> { "17832.AO68356" }, SetPointAddress = "3300.AV7" }); //312
			lightZones.Add(new LightZone { InputAddress = "3300.BV11", OutputAddresses = new List<string> { "17832.AO68358", "3300.BV13" }, SetPointAddress = "3300.AV11" }); //317(319)
			lightZones.Add(new LightZone { InputAddress = "3300.BV13", OutputAddresses = new List<string> { "17832.AO68359" }, SetPointAddress = "3300.AV13" }); //320
			lightZones.Add(new LightZone { InputAddress = "3300.BV15", OutputAddresses = new List<string> { "17832.AO68360" }, SetPointAddress = "3300.AV15" }); //317A(326)
			// Контроллер 3300 коридоры
			lightZones.Add(new LightZone { InputAddress = "3300.BV20", OutputAddresses = new List<string> { "17832.AO68097" }, OutputAlarmAddresses = new List<string> { "17832.AO68100" }, SetPointAddress = "3300.AV19" }); //304a1
			lightZones.Add(new LightZone { InputAddress = "3300.BV21", OutputAddresses = new List<string> { "17832.AO68096" }, OutputAlarmAddresses = new List<string> { "17832.AO68101" }, SetPointAddress = "3300.AV20" }); //304a2
			lightZones.Add(new LightZone { InputAddress = "3300.BV22", OutputAddresses = new List<string> { "17832.AO68099" }, OutputAlarmAddresses = new List<string> { "17832.AO68102" }, SetPointAddress = "3300.AV21" }); //304
			lightZones.Add(new LightZone { InputAddress = "3300.BV23", OutputAddresses = new List<string> { "17832.AO68098", "17832.AO68608" }, OutputAlarmAddresses = new List<string> { "17832.AO68103", "17832.AO68616" }, SetPointAddress = "3300.AV22" }); //304b1
			lightZones.Add(new LightZone { InputAddress = "3300.BV24", OutputAddresses = new List<string> { "17832.AO68609" }, OutputAlarmAddresses = new List<string> { "17832.AO68617" }, SetPointAddress = "3300.AV23" }); //304b2
			lightZones.Add(new LightZone { InputAddress = "3300.BV25", OutputAddresses = new List<string> { "17832.AO68610" }, OutputAlarmAddresses = new List<string> { "17832.AO68618" }, SetPointAddress = "3300.AV24" }); //304b3
			// Контроллер 3400 кабинеты
			lightZones.Add(new LightZone { InputAddress = "3400.BV1", OutputAddresses = new List<string> { "17831.AO68611" }, SetPointAddress = "3400.AV1" }); //301A(301)
			lightZones.Add(new LightZone { InputAddress = "3400.BV2", OutputAddresses = new List<string> { "17831.AO68612" }, SetPointAddress = "3400.AV2" }); //301(302)
			lightZones.Add(new LightZone { InputAddress = "3400.BV13", OutputAddresses = new List<string> { "17831.AO68610" }, SetPointAddress = "3400.AV13" }); //302(327)
			lightZones.Add(new LightZone { InputAddress = "3400.BV14", OutputAddresses = new List<string> { "17831.AO68609" }, SetPointAddress = "3400.AV14" }); //302A(328)
			lightZones.Add(new LightZone { InputAddress = "3400.BV16", OutputAddresses = new List<string> { "17831.AO68104" }, SetPointAddress = "3400.AV16" }); //305A(330)
			lightZones.Add(new LightZone { InputAddress = "3400.BV17", OutputAddresses = new List<string> { "17831.AO68103" }, SetPointAddress = "3400.AV17" }); //305(331)
			lightZones.Add(new LightZone { InputAddress = "3400.BV19", OutputAddresses = new List<string> { "17831.AO68101" }, SetPointAddress = "3400.AV19" }); //307(334)
			lightZones.Add(new LightZone { InputAddress = "3400.BV20", OutputAddresses = new List<string> { "17831.AO68100" }, SetPointAddress = "3400.AV20" }); //307A(335)
			lightZones.Add(new LightZone { InputAddress = "3400.BV21", OutputAddresses = new List<string> { "17831.AO68357" }, SetPointAddress = "3400.AV21" }); //309(337)
			lightZones.Add(new LightZone { InputAddress = "3400.BV22", OutputAddresses = new List<string> { "17831.AO68356" }, SetPointAddress = "3400.AV22" }); //309A(338)
			lightZones.Add(new LightZone { InputAddress = "3400.BV4", OutputAddresses = new List<string> { "17831.AO68099" }, SetPointAddress = "3400.AV4" }); //315
			lightZones.Add(new LightZone { InputAddress = "3400.BV5", OutputAddresses = new List<string> { "17831.AO68098", "3400.BV4" }, SetPointAddress = "3400.AV5" }); //310(316)
			lightZones.Add(new LightZone { InputAddress = "3400.BV7", OutputAddresses = new List<string> { "17831.AO68097", "3400.BV10" }, SetPointAddress = "3400.AV7" }); //308(317)
			lightZones.Add(new LightZone { InputAddress = "3400.BV10", OutputAddresses = new List<string> { "17831.AO68096" }, SetPointAddress = "3400.AV10" }); //323
			lightZones.Add(new LightZone { InputAddress = "3400.BV15", OutputAddresses = new List<string> { "17831.AO68608" }, SetPointAddress = "3400.AV15" }); //329
			lightZones.Add(new LightZone { InputAddress = "3400.BV18", OutputAddresses = new List<string> { "17831.AO68102" }, SetPointAddress = "3400.AV18" }); //306(332)
			lightZones.Add(new LightZone { InputAddress = "3400.BV23", OutputAddresses = new List<string> { "17831.AO68355" }, SetPointAddress = "3400.AV23" }); //311A(339)
			// Контроллер 3400 коридоры
			lightZones.Add(new LightZone { InputAddress = "3400.BV25", OutputAddresses = new List<string> { "17831.AO68353" }, OutputAlarmAddresses = new List<string> { "17831.AO68359" }, SetPointAddress = "3400.AV24" }); //3361
			lightZones.Add(new LightZone { InputAddress = "3400.BV26", OutputAddresses = new List<string> { "17831.AO68352" }, OutputAlarmAddresses = new List<string> { "17831.AO68360" }, SetPointAddress = "3400.AV25" }); //3362
			lightZones.Add(new LightZone { InputAddress = "3400.BV27", OutputAddresses = new List<string> { "17831.AO68354" }, OutputAlarmAddresses = new List<string> { "17831.AO68361" }, SetPointAddress = "3400.AV26" }); //3363
			lightZones.Add(new LightZone { InputAddress = "3400.BV28", OutputAddresses = new List<string> { "17831.AO68358" }, OutputAlarmAddresses = new List<string> { "17831.AO68362" }, SetPointAddress = "3400.AV27" }); //3364
			// Контроллер 4300 кабинеты
			lightZones.Add(new LightZone { InputAddress = "4300.BV1103", OutputAddresses = new List<string> { "17842.AO68610" }, SetPointAddress = "4300.AV1103" }); //419(408)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1104", OutputAddresses = new List<string> { "17842.AO68609" }, SetPointAddress = "4300.AV1104" }); //418(409)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1105", OutputAddresses = new List<string> { "17842.AO68608" }, SetPointAddress = "4300.AV1105" }); //417(410)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1106", OutputAddresses = new List<string> { "17842.AO68357" }, SetPointAddress = "4300.AV1106" }); //416(411)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1107", OutputAddresses = new List<string> { "17842.AO68358" }, SetPointAddress = "4300.AV1107" }); //415(412)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1301", OutputAddresses = new List<string> { "17842.AO68615" }, SetPointAddress = "4300.AV1301" }); //410(430)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1302", OutputAddresses = new List<string> { "17842.AO68614" }, SetPointAddress = "4300.AV1302" }); //411(431)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1303", OutputAddresses = new List<string> { "17842.AO68613" }, SetPointAddress = "4300.AV1303" }); //413(432)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1204", OutputAddresses = new List<string> { "17842.AO68097" }, SetPointAddress = "4300.AV1204" }); //402(416)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1202", OutputAddresses = new List<string> { "17842.AO68099" }, SetPointAddress = "4300.AV1202" }); //412B(415)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1208", OutputAddresses = new List<string> { "17842.AO68096" }, SetPointAddress = "4300.AV1208" }); //412A(417)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1108", OutputAddresses = new List<string> { "17842.AO68098" }, SetPointAddress = "4300.AV1108" }); //414(413)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1101", OutputAddresses = new List<string> { "17842.AO68612" }, SetPointAddress = "4300.AV1101" }); //421(405)
			lightZones.Add(new LightZone { InputAddress = "4300.BV1102", OutputAddresses = new List<string> { "17842.AO68611" }, SetPointAddress = "4300.AV1102" }); //420(406)
			// Контроллер 4300 коридоры
			lightZones.Add(new LightZone { InputAddress = "4300.BV1305", OutputAddresses = new List<string> { "17842.AO68353" }, OutputAlarmAddresses = new List<string> { "17842.AO68359" }, SetPointAddress = "4300.AV1304" }); //404a1
			lightZones.Add(new LightZone { InputAddress = "4300.BV1306", OutputAddresses = new List<string> { "17842.AO68352" }, OutputAlarmAddresses = new List<string> { "17842.AO68360" }, SetPointAddress = "4300.AV1305" }); //404a2
			lightZones.Add(new LightZone { InputAddress = "4300.BV1307", OutputAddresses = new List<string> { "17842.AO68354" }, OutputAlarmAddresses = new List<string> { "17842.AO68361" }, SetPointAddress = "4300.AV1306" }); //404
			lightZones.Add(new LightZone { InputAddress = "4300.BV1308", OutputAddresses = new List<string> { "17842.AO68355" }, OutputAlarmAddresses = new List<string> { "17842.AO68362" }, SetPointAddress = "4300.AV1307" }); //404b1
			lightZones.Add(new LightZone { InputAddress = "4300.BV1309", OutputAddresses = new List<string> { "17842.AO68356" }, OutputAlarmAddresses = new List<string> { "17842.AO68363" }, SetPointAddress = "4300.AV1308" }); //404b2
			// Контроллер 4400 кабинеты
			lightZones.Add(new LightZone { InputAddress = "4400.BV1101", OutputAddresses = new List<string> { "17841.AO68099" }, SetPointAddress = "4400.AV1101" }); //402(401)
			lightZones.Add(new LightZone { InputAddress = "4400.BV1102", OutputAddresses = new List<string> { "17841.AO68100" }, SetPointAddress = "4400.AV1102" }); //401(402)
			lightZones.Add(new LightZone { InputAddress = "4400.BV1105", OutputAddresses = new List<string> { "17841.AO68098" }, SetPointAddress = "4400.AV1105" }); //404(420)
			lightZones.Add(new LightZone { InputAddress = "4400.BV1106", OutputAddresses = new List<string> { "17841.AO68097" }, SetPointAddress = "4400.AV1106" }); //403(421)
			lightZones.Add(new LightZone { InputAddress = "4400.BV1107", OutputAddresses = new List<string> { "17841.AO68096" }, SetPointAddress = "4400.AV1107" }); //407(422)
			lightZones.Add(new LightZone { InputAddress = "4400.BV1201", OutputAddresses = new List<string> { "17841.AO68355" }, SetPointAddress = "4400.AV1201" }); //409(424)
			lightZones.Add(new LightZone { InputAddress = "4400.BV1202", OutputAddresses = new List<string> { "17841.AO68356" }, SetPointAddress = "4400.AV1202" }); //408(425)
			// Контроллер 4400 коридоры
			lightZones.Add(new LightZone { InputAddress = "4400.BV1209", OutputAddresses = new List<string> { "17841.AO68353" }, OutputAlarmAddresses = new List<string> { "17841.AO68357" }, SetPointAddress = "4400.AV1208" }); //4231
			lightZones.Add(new LightZone { InputAddress = "4400.BV1210", OutputAddresses = new List<string> { "17841.AO68352" }, OutputAlarmAddresses = new List<string> { "17841.AO68358" }, SetPointAddress = "4400.AV1209" }); //4232
			lightZones.Add(new LightZone { InputAddress = "4400.BV1211", OutputAddresses = new List<string> { "17841.AO68354" }, OutputAlarmAddresses = new List<string> { "17841.AO68359" }, SetPointAddress = "4400.AV1210" }); //4233
			return lightZones;
		}
	}
}
