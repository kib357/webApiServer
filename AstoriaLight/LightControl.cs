using System;
using System.Collections.Generic;
using System.Linq;
using BacNetApi;

namespace AstoriaLight
{
	public class LightZone
	{
		public string InputAddress { get; set; }
		public string InputValue { get; set; }
		public List<string> OutputAddresses { get; set; }
		public List<string> OutputAlarmAddresses { get; set; }
		public string SetPointAddress { get; set; }
		public string SetPointValue { get; set; }
		public string LastSettedValue { get; set; }
	}

	public class LightControl
	{
		private readonly List<LightZone> _lightZones;
		private BacNet _network;

		public LightControl(BacNet network, List<LightZone> lightZones)
		{
			_network = network;
			_lightZones = lightZones;
		}

		public void OnValueChanged(string address, string newvalue)
		{
			var zoneWithChangedSetpoint = _lightZones.FirstOrDefault(l => l.SetPointAddress == address);
			if (zoneWithChangedSetpoint != null)
			{
				zoneWithChangedSetpoint.SetPointValue = newvalue;
				if (zoneWithChangedSetpoint.InputValue == "1")
					foreach (var output in zoneWithChangedSetpoint.OutputAddresses)
					{
						if (output.Contains("AO"))
							WriteToNetwork(output, zoneWithChangedSetpoint.SetPointValue);
					}
				return;
			}
			var zoneWithChangedInput = _lightZones.FirstOrDefault(l => l.InputAddress == address);
			if (zoneWithChangedInput == null) return;

			zoneWithChangedInput.InputValue = newvalue;
			string setpoint;
			if (zoneWithChangedInput.InputValue == "1")
			{
				setpoint = string.IsNullOrWhiteSpace(zoneWithChangedInput.SetPointValue)
							   ? "100"
							   : zoneWithChangedInput.SetPointValue;
				foreach (var output in zoneWithChangedInput.OutputAddresses)
				{
					if (output.Contains("AO"))
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
					{
						WriteToNetwork(alarmAddress, "100");
					}
			}
		}

		private void WriteToNetwork(string address, string newvalue)
		{
			var addressList = address.Split('.');
			uint dev;
			if (addressList.Length != 2 || !uint.TryParse(addressList[0], out dev))
				throw new Exception("проверь как забил адреса");
			_network[dev].Objects[addressList[1]].BeginSet(newvalue);
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
			// Контроллер 1400 кабинеты (коридоры пока что с контроллера)
			lightZones.Add(new LightZone { InputAddress = "1400.BV1102", OutputAddresses = new List<string> { "17811.AO1104" }, SetPointAddress = "1400.AV1102" }); //102
			/*_lightZones.Add(new LightZone { InputAddress = "1400.BV1102", OutputAddresses = new List<string> { "17811.AO1104" }, SetPointAddress = "1400.AV1102" }); //102
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1101", OutputAddresses = new List<string> { "17811.AO1103" }, SetPointAddress = "1400.AV1101" }); //103(101)
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1108", OutputAddresses = new List<string> { "17811.AO1101" }, SetPointAddress = "1400.AV1108" }); //105(131)
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1106", OutputAddresses = new List<string> { "17811.AO1000" }, SetPointAddress = "1400.AV1106" }); //110(127)
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1107", OutputAddresses = new List<string> { "17811.AO1102" }, SetPointAddress = "1400.AV1107" }); //104(130)
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1302", OutputAddresses = new List<string> { "17811.AO1001" }, SetPointAddress = "1400.AV1302" }); //111(113)
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1304", OutputAddresses = new List<string> { "17811.AO1002" }, SetPointAddress = "1400.AV1304" }); //145
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1203", OutputAddresses = new List<string> { "17811.AO1003", "1400.BV1304", "1400.BV1205", "1400.BV1206", "1400.BV1204" }, SetPointAddress = "1400.AV1203" }); //138
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1206", OutputAddresses = new List<string> { "17811.AO1004" }, SetPointAddress = "1400.AV1206" }); //144
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1305", OutputAddresses = new List<string> { "17811.AO1005" }, SetPointAddress = "1400.AV1305" }); //146
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1301", OutputAddresses = new List<string> { "17811.AO1006" }, SetPointAddress = "1400.AV1301" }); //143
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1208", OutputAddresses = new List<string> { "17811.AO1007" }, SetPointAddress = "1400.AV1208" }); //142
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1207", OutputAddresses = new List<string> { "17811.AO1008" }, SetPointAddress = "1400.AV1207" }); //141
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1204", OutputAddresses = new List<string> { "17811.AO1009", "1400.BV1207", "1400.BV1208" }, SetPointAddress = "1400.AV1204" }); //140
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1205", OutputAddresses = new List<string> { "17811.AO1010" }, SetPointAddress = "1400.AV1205" }); //139
			// Контроллер 1400 коридоры
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1306", OutputAddresses = new List<string> { "17811.AO1105" }, OutputAlarmAddresses = new List<string> { "17811.AO1108" }, SetPointAddress = "1400.AV1306" }); //132
			_lightZones.Add(new LightZone { InputAddress = "1400.BV1307", OutputAddresses = new List<string> { "17811.AO1106" }, OutputAlarmAddresses = new List<string> { "17811.AO1110" }, SetPointAddress = "1400.AV1307" }); //132a
			// Контроллер 1300 кабинеты
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1101", OutputAddresses = new List<string> { "17812.AO68097" }, SetPointAddress = "1300.AV1101" }); //101(104)
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1106", OutputAddresses = new List<string> { "17812.AO68352" }, SetPointAddress = "1300.AV1106" }); //114a(122a)
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1209", OutputAddresses = new List<string> { "17812.AO68353" }, SetPointAddress = "1300.AV1106" }); //114b(122b)
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1103", OutputAddresses = new List<string> { "17812.AO68608" }, SetPointAddress = "1300.AV1103" }); //118(109)
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1104", OutputAddresses = new List<string> { "17812.AO68096" }, SetPointAddress = "1300.AV1104" }); //113(107b)
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1105", OutputAddresses = new List<string> { "17812.AO68612" }, SetPointAddress = "1300.AV1105" }); //116(112)
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1201", OutputAddresses = new List<string> { "17812.AO68611" }, SetPointAddress = "1300.AV1201" }); //115(114)
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1202", OutputAddresses = new List<string> { "17812.AO68101" }, SetPointAddress = "1300.AV1202" }); //UPS(120)
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1102", OutputAddresses = new List<string> { "17812.AO68609" }, SetPointAddress = "1300.AV1102" }); //117(110)
			// Контроллер 1300 коридоры
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1210", OutputAddresses = new List<string> { "17812.AO68104" }, OutputAlarmAddresses = new List<string> { "17812.AO68103" }, SetPointAddress = "1300.AV1210" }); //115
			_lightZones.Add(new LightZone { InputAddress = "1300.BV1211", OutputAddresses = new List<string> { "17812.AO68610" }, OutputAlarmAddresses = new List<string> { "17812.AO68614" }, SetPointAddress = "1300.AV1211" }); //111
			// Контроллер 2300 кабинеты
			/*_lightZones.Add(new LightZone { InputAddress = "2300.BV1101", OutputAddresses = new List<string> { "17822.AO68352" }, SetPointAddress = "2300.AV1101" }); //218(205)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1102", OutputAddresses = new List<string> { "17822.AO68353" }, SetPointAddress = "2300.AV1102" }); //216(206)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1103", OutputAddresses = new List<string> { "17822.AO68354" }, SetPointAddress = "2300.AV1103" }); //214(208)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1104", OutputAddresses = new List<string> { "17822.AO68355" }, SetPointAddress = "2300.AV1104" }); //213(209)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1105", OutputAddresses = new List<string> { "17822.AO68356" }, SetPointAddress = "2300.AV1105" }); //213A(210)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1106", OutputAddresses = new List<string> { "17822.AO68611" }, SetPointAddress = "2300.AV1106" }); //212A(211)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1107", OutputAddresses = new List<string> { "17822.AO68612" }, SetPointAddress = "2300.AV1107" }); //212
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1302", OutputAddresses = new List<string> { "17822.AO68614" }, SetPointAddress = "2300.AV1302" }); //211(240)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1303", OutputAddresses = new List<string> { "17822.AO68613" }, SetPointAddress = "2300.AV1303" }); //211A(241)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1108", OutputAddresses = new List<string> { "17822.AO68357" }, SetPointAddress = "2300.AV1108" }); //213
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1202", OutputAddresses = new List<string> { "17822.AO68358", "2300.BV1108" }, SetPointAddress = "2300.AV1202" }); //215(219)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1204", OutputAddresses = new List<string> { "17822.AO68359", "2300.BV1206", "2300.BV1208" }, SetPointAddress = "2300.AV1204" }); //217(220)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1206", OutputAddresses = new List<string> { "17822.AO68360" }, SetPointAddress = "2300.AV1206" }); //221
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1208", OutputAddresses = new List<string> { "17822.AO68361" }, SetPointAddress = "2300.AV1208" }); //217C(227)
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1301", OutputAddresses = new List<string> { "17822.AO68615" }, SetPointAddress = "2300.AV1301" }); //210(239)
			// Контроллер 2300 коридоры
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1305", OutputAddresses = new List<string> { "17822.AO68097" }, OutputAlarmAddresses = new List<string> { "17822.AO68100" }, SetPointAddress = "2300.AV1304" }); //204a1
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1306", OutputAddresses = new List<string> { "17822.AO68096" }, OutputAlarmAddresses = new List<string> { "17822.AO68101" }, SetPointAddress = "2300.AV1305" }); //204a2
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1307", OutputAddresses = new List<string> { "17822.AO68099" }, OutputAlarmAddresses = new List<string> { "17822.AO68102" }, SetPointAddress = "2300.AV1306" }); //2041
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1308", OutputAddresses = new List<string> { "17822.AO68098", "17822.AO68608" }, OutputAlarmAddresses = new List<string> { "17822.AO68103", "17822.AO68616" }, SetPointAddress = "2300.AV1307" }); //2042
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1309", OutputAddresses = new List<string> { "17822.AO68609" }, OutputAlarmAddresses = new List<string> { "17822.AO68617" }, SetPointAddress = "2300.AV1308" }); //204b1
			_lightZones.Add(new LightZone { InputAddress = "2300.BV1310", OutputAddresses = new List<string> { "17822.AO68610" }, OutputAlarmAddresses = new List<string> { "17822.AO68618" }, SetPointAddress = "2300.AV1309" }); //204b2*/

			//lightZones.Add(new LightZone
			//				   {
			//					   InputAddress = "3700.BV9",
			//					   OutputAddresses = new List<string> { "17832.AO68357" },
			//					   SetPointAddress = "3700.AV9"
			//				   });
			return lightZones;
		}
	}
}
