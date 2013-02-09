using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BacNetApi;

namespace AstoriaControlService.Common.Light
{
	public class LightControl : CommonControl
	{
		private readonly List<LightZone> _lightZones = new List<LightZone>();
		private readonly BacNet _network;

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
				var zonesToRemove = _lightZones.Where(l => lightZones.All(z => !z.Equals(l))).ToList();
				zonesToRemove.ForEach(RemoveZone);

				var zonesToAdd = lightZones.Where(l => !_lightZones.All(z => z.Equals(l))).ToList();
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

			if (zoneWithChangedInput != null)
			{
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
			lightZones.Add(new LightZone { InputAddress = "1300.BV31102", OutputAddresses = new ObservableCollection<string> { "17811.AO1104" }, SetPointAddress = "1300.AV31102" }); //102
			lightZones.Add(new LightZone { InputAddress = "1300.BV31101", OutputAddresses = new ObservableCollection<string> { "17811.AO1103" }, SetPointAddress = "1300.AV31101" }); //103(101)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31131", OutputAddresses = new ObservableCollection<string> { "17811.AO1101" }, SetPointAddress = "1300.AV31131" }); //105(131)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31127", OutputAddresses = new ObservableCollection<string> { "17811.AO1000" }, SetPointAddress = "1300.AV31127" }); //110(127)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31130", OutputAddresses = new ObservableCollection<string> { "17811.AO1102" }, SetPointAddress = "1300.AV31130" }); //104(130)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31113", OutputAddresses = new ObservableCollection<string> { "17811.AO1001" }, SetPointAddress = "1300.AV31113" }); //111(113)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31145", OutputAddresses = new ObservableCollection<string> { "17811.AO1002" }, SetPointAddress = "1300.AV31145" }); //145
			lightZones.Add(new LightZone { InputAddress = "1300.BV31138", OutputAddresses = new ObservableCollection<string> { "17811.AO1003", "1300.BV31140", "1300.BV31139", "1300.BV31144", "1300.BV31145" }, SetPointAddress = "1300.AV31138" }); //138
			lightZones.Add(new LightZone { InputAddress = "1300.BV31144", OutputAddresses = new ObservableCollection<string> { "17811.AO1004" }, SetPointAddress = "1300.AV31144" }); //144
			lightZones.Add(new LightZone { InputAddress = "1300.BV31146", OutputAddresses = new ObservableCollection<string> { "17811.AO1005" }, SetPointAddress = "1300.AV31146" }); //146
			lightZones.Add(new LightZone { InputAddress = "1300.BV31143", OutputAddresses = new ObservableCollection<string> { "17811.AO1006" }, SetPointAddress = "1300.AV31143" }); //143
			lightZones.Add(new LightZone { InputAddress = "1300.BV31142", OutputAddresses = new ObservableCollection<string> { "17811.AO1007" }, SetPointAddress = "1300.AV31142" }); //142
			lightZones.Add(new LightZone { InputAddress = "1300.BV31141", OutputAddresses = new ObservableCollection<string> { "17811.AO1008" }, SetPointAddress = "1300.AV31141" }); //141
			lightZones.Add(new LightZone { InputAddress = "1300.BV31140", OutputAddresses = new ObservableCollection<string> { "17811.AO1009", "1300.BV31141", "1300.BV31142" }, SetPointAddress = "1300.AV31140" }); //140
			lightZones.Add(new LightZone { InputAddress = "1300.BV31139", OutputAddresses = new ObservableCollection<string> { "17811.AO1010" }, SetPointAddress = "1300.AV31139" }); //139
			//// Контроллер 1400 коридоры
			lightZones.Add(new LightZone { InputAddress = "1300.BV31132", OutputAddresses = new ObservableCollection<string> { "17811.AO1105" }, OutputAlarmAddresses = new ObservableCollection<string> { "17811.AO1108" }, SetPointAddress = "1300.AV31132" }); //132
			lightZones.Add(new LightZone { InputAddress = "1300.BV311321", OutputAddresses = new ObservableCollection<string> { "17811.AO1106" }, OutputAlarmAddresses = new ObservableCollection<string> { "17811.AO1110" }, SetPointAddress = "1300.AV311321" }); //132a
			// Контроллер 1300 кабинеты
			lightZones.Add(new LightZone { InputAddress = "1300.BV31104", OutputAddresses = new ObservableCollection<string> { "17812.AO68097" }, SetPointAddress = "1300.AV31104" }); //101(104)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31122", OutputAddresses = new ObservableCollection<string> { "17812.AO68352" }, SetPointAddress = "1300.AV31122" }); //114a(122a)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31122", OutputAddresses = new ObservableCollection<string> { "17812.AO68353" }, SetPointAddress = "1300.AV31122" }); //114b(122b)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31109", OutputAddresses = new ObservableCollection<string> { "17812.AO68608" }, SetPointAddress = "1300.AV31109" }); //118(109)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31107", OutputAddresses = new ObservableCollection<string> { "17812.AO68096" }, SetPointAddress = "1300.AV31107" }); //113(107b)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31112", OutputAddresses = new ObservableCollection<string> { "17812.AO68612" }, SetPointAddress = "1300.AV31122" }); //116(112)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31114", OutputAddresses = new ObservableCollection<string> { "17812.AO68611" }, SetPointAddress = "1300.AV31114" }); //115(114)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31120", OutputAddresses = new ObservableCollection<string> { "17812.AO68101" }, SetPointAddress = "1300.AV31120" }); //UPS(120)
			lightZones.Add(new LightZone { InputAddress = "1300.BV31110", OutputAddresses = new ObservableCollection<string> { "17812.AO68609" }, SetPointAddress = "1300.AV31110" }); //117(110)
			//// Контроллер 1300 коридоры
			lightZones.Add(new LightZone { InputAddress = "1300.BV31115", OutputAddresses = new ObservableCollection<string> { "17812.AO68104" }, OutputAlarmAddresses = new ObservableCollection<string> { "17812.AO68103" }, SetPointAddress = "1300.AV31115" }); //115
			lightZones.Add(new LightZone { InputAddress = "1300.BV31111", OutputAddresses = new ObservableCollection<string> { "17812.AO68610" }, OutputAlarmAddresses = new ObservableCollection<string> { "17812.AO68614" }, SetPointAddress = "1300.AV31111" }); //111
			// Контроллер 2300 кабинеты
			lightZones.Add(new LightZone { InputAddress = "2300.BV31205", OutputAddresses = new ObservableCollection<string> { "17822.AO68352" }, SetPointAddress = "2300.AV31205" }); //218(205)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31206", OutputAddresses = new ObservableCollection<string> { "17822.AO68353" }, SetPointAddress = "2300.AV31206" }); //216(206)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31208", OutputAddresses = new ObservableCollection<string> { "17822.AO68354" }, SetPointAddress = "2300.AV31208" }); //214(208)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31209", OutputAddresses = new ObservableCollection<string> { "17822.AO68355" }, SetPointAddress = "2300.AV31209" }); //213(209)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31210", OutputAddresses = new ObservableCollection<string> { "17822.AO68356" }, SetPointAddress = "2300.AV31210" }); //213A(210)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31211", OutputAddresses = new ObservableCollection<string> { "17822.AO68611" }, SetPointAddress = "2300.AV31211" }); //212A(211)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31201", OutputAddresses = new ObservableCollection<string> { "17821.AO1203" }, SetPointAddress = "2300.AV31201" }); //201A(201)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31202", OutputAddresses = new ObservableCollection<string> { "17821.AO1204" }, SetPointAddress = "2300.AV31202" }); //201(202)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31228", OutputAddresses = new ObservableCollection<string> { "17821.AO1202" }, SetPointAddress = "2300.AV31228" }); //202(228)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31229", OutputAddresses = new ObservableCollection<string> { "17821.AO1201" }, SetPointAddress = "2300.AV31229" }); //202A(229)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31213", OutputAddresses = new ObservableCollection<string> { "17822.AO68357" }, SetPointAddress = "2300.AV31213" }); //213
			lightZones.Add(new LightZone { InputAddress = "2300.BV31219", OutputAddresses = new ObservableCollection<string> { "17822.AO68358", "2300.BV31213" }, SetPointAddress = "2300.AV31219" }); //215(219)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31220", OutputAddresses = new ObservableCollection<string> { "17822.AO68359", "2300.BV31221", "2300.BV31227" }, SetPointAddress = "2300.AV31220" }); //217(220)
			lightZones.Add(new LightZone { InputAddress = "2300.BV31221", OutputAddresses = new ObservableCollection<string> { "17822.AO68360" }, SetPointAddress = "2300.AV31221" }); //221
			lightZones.Add(new LightZone { InputAddress = "2300.BV31227", OutputAddresses = new ObservableCollection<string> { "17822.AO68361" }, SetPointAddress = "2300.AV31227" }); //217A(227)
			// Контроллер 2300 коридоры
			lightZones.Add(new LightZone { InputAddress = "2300.BV3120411", OutputAddresses = new ObservableCollection<string> { "17822.AO68097" }, OutputAlarmAddresses = new ObservableCollection<string> { "17822.AO68100" }, SetPointAddress = "2300.AV3120411" }); //204a1
			lightZones.Add(new LightZone { InputAddress = "2300.BV3120412", OutputAddresses = new ObservableCollection<string> { "17822.AO68096" }, OutputAlarmAddresses = new ObservableCollection<string> { "17822.AO68101" }, SetPointAddress = "2300.AV3120412" }); //204a2
			lightZones.Add(new LightZone { InputAddress = "2300.BV312041", OutputAddresses = new ObservableCollection<string> { "17822.AO68099" }, OutputAlarmAddresses = new ObservableCollection<string> { "17822.AO68102" }, SetPointAddress = "2300.AV312041" }); //2041
			lightZones.Add(new LightZone { InputAddress = "2300.BV312042", OutputAddresses = new ObservableCollection<string> { "17822.AO68098", "17822.AO68608" }, OutputAlarmAddresses = new ObservableCollection<string> { "17822.AO68103", "17822.AO68616" }, SetPointAddress = "2300.AV312042" }); //2042
			lightZones.Add(new LightZone { InputAddress = "2300.BV3120421", OutputAddresses = new ObservableCollection<string> { "17822.AO68609" }, OutputAlarmAddresses = new ObservableCollection<string> { "17822.AO68617" }, SetPointAddress = "2300.AV3120421" }); //204b1
			lightZones.Add(new LightZone { InputAddress = "2300.BV3120422", OutputAddresses = new ObservableCollection<string> { "17822.AO68610" }, OutputAlarmAddresses = new ObservableCollection<string> { "17822.AO68618" }, SetPointAddress = "2300.AV3120422" }); //204b2
			// Контроллер 2400 кабинеты
			lightZones.Add(new LightZone { InputAddress = "2400.BV31239", OutputAddresses = new ObservableCollection<string> { "17822.AO68615" }, SetPointAddress = "2400.AV31239" }); //210(239)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31212", OutputAddresses = new ObservableCollection<string> { "17822.AO68612" }, SetPointAddress = "2400.AV31212" }); //212
			lightZones.Add(new LightZone { InputAddress = "2400.BV31240", OutputAddresses = new ObservableCollection<string> { "17822.AO68614" }, SetPointAddress = "2400.AV31240" }); //211(240)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31241", OutputAddresses = new ObservableCollection<string> { "17822.AO68613" }, SetPointAddress = "2400.AV31241" }); //211A(241)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31231", OutputAddresses = new ObservableCollection<string> { "17821.AO1007" }, SetPointAddress = "2400.AV31231" }); //205A(231)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31232", OutputAddresses = new ObservableCollection<string> { "17821.AO1006" }, SetPointAddress = "2400.AV31232" }); //205(232)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31234", OutputAddresses = new ObservableCollection<string> { "17821.AO1005" }, SetPointAddress = "2400.AV31234" }); //206(234)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31236", OutputAddresses = new ObservableCollection<string> { "17821.AO1107" }, SetPointAddress = "2400.AV31236" }); //208(236)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31237", OutputAddresses = new ObservableCollection<string> { "17821.AO1106" }, SetPointAddress = "2400.AV31237" }); //208A(237)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31238", OutputAddresses = new ObservableCollection<string> { "17821.AO1105" }, SetPointAddress = "2400.AV31238" }); //210A(238)
			lightZones.Add(new LightZone { InputAddress = "2400.BV312341", OutputAddresses = new ObservableCollection<string> { "17821.AO1004" }, SetPointAddress = "2400.AV312341" }); //206A(234A)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31216", OutputAddresses = new ObservableCollection<string> { "17821.AO1003" }, SetPointAddress = "2400.AV31216" }); //216
			lightZones.Add(new LightZone { InputAddress = "2400.BV31217", OutputAddresses = new ObservableCollection<string> { "17821.AO1002", "2400.BV31216" }, SetPointAddress = "2400.AV31217" }); //209(217)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31218", OutputAddresses = new ObservableCollection<string> { "17821.AO1001", "2400.BV31224" }, SetPointAddress = "2400.AV31218" }); //207(218)
			lightZones.Add(new LightZone { InputAddress = "2400.BV31224", OutputAddresses = new ObservableCollection<string> { "17821.AO1000" }, SetPointAddress = "2400.AV31224" }); //224
			lightZones.Add(new LightZone { InputAddress = "2400.BV31230", OutputAddresses = new ObservableCollection<string> { "17821.AO1200" }, SetPointAddress = "2400.AV31230" }); //230
			// Контроллер 2400 коридоры
			lightZones.Add(new LightZone { InputAddress = "2400.BV312351", OutputAddresses = new ObservableCollection<string> { "17821.AO1101" }, OutputAlarmAddresses = new ObservableCollection<string> { "17821.AO1109" }, SetPointAddress = "2400.AV312351" }); //2351
			lightZones.Add(new LightZone { InputAddress = "2400.BV312352", OutputAddresses = new ObservableCollection<string> { "17821.AO1100" }, OutputAlarmAddresses = new ObservableCollection<string> { "17821.AO1110" }, SetPointAddress = "2400.AV312352" }); //2352
			lightZones.Add(new LightZone { InputAddress = "2400.BV312353", OutputAddresses = new ObservableCollection<string> { "17821.AO1103" }, OutputAlarmAddresses = new ObservableCollection<string> { "17821.AO1111" }, SetPointAddress = "2400.AV312353" }); //2353
			lightZones.Add(new LightZone { InputAddress = "2400.BV312354", OutputAddresses = new ObservableCollection<string> { "17821.AO1108" }, OutputAlarmAddresses = new ObservableCollection<string> { "17821.AO1112" }, SetPointAddress = "2400.AV312354" }); //2354
			// Контроллер 3300 кабинеты
			lightZones.Add(new LightZone { InputAddress = "3300.BV31310", OutputAddresses = new ObservableCollection<string> { "17832.AO68611" }, SetPointAddress = "3300.AV31310" }); //313A(310)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31309", OutputAddresses = new ObservableCollection<string> { "17832.AO68355" }, SetPointAddress = "3300.AV31309" }); //314A(309)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31308", OutputAddresses = new ObservableCollection<string> { "17832.AO68354" }, SetPointAddress = "3300.AV31308" }); //314(308)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31306", OutputAddresses = new ObservableCollection<string> { "17832.AO68353" }, SetPointAddress = "3300.AV31306" }); //316(306)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31305", OutputAddresses = new ObservableCollection<string> { "17832.AO68352" }, SetPointAddress = "3300.AV31305" }); //318(305)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31318", OutputAddresses = new ObservableCollection<string> { "17832.AO68357", "3300.BV31312" }, SetPointAddress = "3300.AV31318" }); //315(318)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31312", OutputAddresses = new ObservableCollection<string> { "17832.AO68356" }, SetPointAddress = "3300.AV31312" }); //312
			lightZones.Add(new LightZone { InputAddress = "3300.BV31319", OutputAddresses = new ObservableCollection<string> { "17832.AO68358", "3300.BV31320" }, SetPointAddress = "3300.AV31319" }); //317(319)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31320", OutputAddresses = new ObservableCollection<string> { "17832.AO68359" }, SetPointAddress = "3300.AV31320" }); //320
			lightZones.Add(new LightZone { InputAddress = "3300.BV31326", OutputAddresses = new ObservableCollection<string> { "17832.AO68360" }, SetPointAddress = "3300.AV31326" }); //317A(326)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31301", OutputAddresses = new ObservableCollection<string> { "17831.AO68611" }, SetPointAddress = "3300.AV31301" }); //301A(301)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31302", OutputAddresses = new ObservableCollection<string> { "17831.AO68612" }, SetPointAddress = "3300.AV31302" }); //301(302)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31327", OutputAddresses = new ObservableCollection<string> { "17831.AO68610" }, SetPointAddress = "3300.AV31327" }); //302(327)
			lightZones.Add(new LightZone { InputAddress = "3300.BV31328", OutputAddresses = new ObservableCollection<string> { "17831.AO68609" }, SetPointAddress = "3300.AV31328" }); //302A(328)
			// Контроллер 3300 коридоры
			lightZones.Add(new LightZone { InputAddress = "3300.BV3130411", OutputAddresses = new ObservableCollection<string> { "17832.AO68097" }, OutputAlarmAddresses = new ObservableCollection<string> { "17832.AO68100" }, SetPointAddress = "3300.AV3130411" }); //304a1
			lightZones.Add(new LightZone { InputAddress = "3300.BV3130412", OutputAddresses = new ObservableCollection<string> { "17832.AO68096" }, OutputAlarmAddresses = new ObservableCollection<string> { "17832.AO68101" }, SetPointAddress = "3300.AV3130412" }); //304a2
			lightZones.Add(new LightZone { InputAddress = "3300.BV31304", OutputAddresses = new ObservableCollection<string> { "17832.AO68099" }, OutputAlarmAddresses = new ObservableCollection<string> { "17832.AO68102" }, SetPointAddress = "3300.AV31304" }); //304
			lightZones.Add(new LightZone { InputAddress = "3300.BV3130421", OutputAddresses = new ObservableCollection<string> { "17832.AO68098", "17832.AO68608" }, OutputAlarmAddresses = new ObservableCollection<string> { "17832.AO68103", "17832.AO68616" }, SetPointAddress = "3300.AV3130421" }); //304b1
			lightZones.Add(new LightZone { InputAddress = "3300.BV3130422", OutputAddresses = new ObservableCollection<string> { "17832.AO68609" }, OutputAlarmAddresses = new ObservableCollection<string> { "17832.AO68617" }, SetPointAddress = "3300.AV3130422" }); //304b2
			lightZones.Add(new LightZone { InputAddress = "3300.BV3130423", OutputAddresses = new ObservableCollection<string> { "17832.AO68610" }, OutputAlarmAddresses = new ObservableCollection<string> { "17832.AO68618" }, SetPointAddress = "3300.AV3139423" }); //304b3
			// Контроллер 3400 кабинеты
			lightZones.Add(new LightZone { InputAddress = "3400.BV31330", OutputAddresses = new ObservableCollection<string> { "17831.AO68104" }, SetPointAddress = "3400.AV31330" }); //305A(330)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31331", OutputAddresses = new ObservableCollection<string> { "17831.AO68103" }, SetPointAddress = "3400.AV31331" }); //305(331)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31334", OutputAddresses = new ObservableCollection<string> { "17831.AO68101" }, SetPointAddress = "3400.AV31334" }); //307(334)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31335", OutputAddresses = new ObservableCollection<string> { "17831.AO68100" }, SetPointAddress = "3400.AV31335" }); //307A(335)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31337", OutputAddresses = new ObservableCollection<string> { "17831.AO68357" }, SetPointAddress = "3400.AV31337" }); //309(337)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31338", OutputAddresses = new ObservableCollection<string> { "17831.AO68356" }, SetPointAddress = "3400.AV31338" }); //309A(338)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31315", OutputAddresses = new ObservableCollection<string> { "17831.AO68099" }, SetPointAddress = "3400.AV31315" }); //315
			lightZones.Add(new LightZone { InputAddress = "3400.BV31316", OutputAddresses = new ObservableCollection<string> { "17831.AO68098", "3400.BV31315" }, SetPointAddress = "3400.AV31316" }); //310(316)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31317", OutputAddresses = new ObservableCollection<string> { "17831.AO68097", "3400.BV31323" }, SetPointAddress = "3400.AV31317" }); //308(317)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31323", OutputAddresses = new ObservableCollection<string> { "17831.AO68096" }, SetPointAddress = "3400.AV31323" }); //323
			lightZones.Add(new LightZone { InputAddress = "3400.BV31329", OutputAddresses = new ObservableCollection<string> { "17831.AO68608" }, SetPointAddress = "3400.AV31329" }); //329
			lightZones.Add(new LightZone { InputAddress = "3400.BV31332", OutputAddresses = new ObservableCollection<string> { "17831.AO68102" }, SetPointAddress = "3400.AV31332" }); //306(332)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31339", OutputAddresses = new ObservableCollection<string> { "17831.AO68355" }, SetPointAddress = "3400.AV31339" }); //311A(339)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31340", OutputAddresses = new ObservableCollection<string> { "17832.AO68615" }, SetPointAddress = "3400.AV31340" }); //311(340)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31341", OutputAddresses = new ObservableCollection<string> { "17832.AO68614" }, SetPointAddress = "3400.AV31341" }); //312(341)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31342", OutputAddresses = new ObservableCollection<string> { "17832.AO68613" }, SetPointAddress = "3400.AV31342" }); //312A(342)
			lightZones.Add(new LightZone { InputAddress = "3400.BV31311", OutputAddresses = new ObservableCollection<string> { "17832.AO68612" }, SetPointAddress = "3400.AV31311" }); //313(311)
			// Контроллер 3400 коридоры
			lightZones.Add(new LightZone { InputAddress = "3400.BV313361", OutputAddresses = new ObservableCollection<string> { "17831.AO68353" }, OutputAlarmAddresses = new ObservableCollection<string> { "17831.AO68359" }, SetPointAddress = "3400.AV313361" }); //3361
			lightZones.Add(new LightZone { InputAddress = "3400.BV313362", OutputAddresses = new ObservableCollection<string> { "17831.AO68352" }, OutputAlarmAddresses = new ObservableCollection<string> { "17831.AO68360" }, SetPointAddress = "3400.AV313362" }); //3362
			lightZones.Add(new LightZone { InputAddress = "3400.BV313363", OutputAddresses = new ObservableCollection<string> { "17831.AO68354" }, OutputAlarmAddresses = new ObservableCollection<string> { "17831.AO68361" }, SetPointAddress = "3400.AV313363" }); //3363
			lightZones.Add(new LightZone { InputAddress = "3400.BV313364", OutputAddresses = new ObservableCollection<string> { "17831.AO68358" }, OutputAlarmAddresses = new ObservableCollection<string> { "17831.AO68362" }, SetPointAddress = "3400.AV313364" }); //3364
			// Контроллер 4300 кабинеты
			lightZones.Add(new LightZone { InputAddress = "4300.BV31408", OutputAddresses = new ObservableCollection<string> { "17842.AO68610" }, SetPointAddress = "4300.AV31408" }); //419(408)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31409", OutputAddresses = new ObservableCollection<string> { "17842.AO68609" }, SetPointAddress = "4300.AV31409" }); //418(409)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31410", OutputAddresses = new ObservableCollection<string> { "17842.AO68608" }, SetPointAddress = "4300.AV31410" }); //417(410)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31411", OutputAddresses = new ObservableCollection<string> { "17842.AO68357" }, SetPointAddress = "4300.AV31411" }); //416(411)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31416", OutputAddresses = new ObservableCollection<string> { "17842.AO68097" }, SetPointAddress = "4300.AV31416" }); //402(416)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31417", OutputAddresses = new ObservableCollection<string> { "17842.AO68096" }, SetPointAddress = "4300.AV31417" }); //412A(417)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31413", OutputAddresses = new ObservableCollection<string> { "17842.AO68098" }, SetPointAddress = "4300.AV31413" }); //414(413)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31405", OutputAddresses = new ObservableCollection<string> { "17842.AO68612" }, SetPointAddress = "4300.AV31405" }); //421(405)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31406", OutputAddresses = new ObservableCollection<string> { "17842.AO68611" }, SetPointAddress = "4300.AV31406" }); //420(406)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31401", OutputAddresses = new ObservableCollection<string> { "17841.AO68099" }, SetPointAddress = "4300.AV31401" }); //402(401)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31402", OutputAddresses = new ObservableCollection<string> { "17841.AO68100" }, SetPointAddress = "4300.AV31402" }); //401(402)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31420", OutputAddresses = new ObservableCollection<string> { "17841.AO68098" }, SetPointAddress = "4300.AV31420" }); //404(420)
			lightZones.Add(new LightZone { InputAddress = "4300.BV31421", OutputAddresses = new ObservableCollection<string> { "17841.AO68097" }, SetPointAddress = "4300.AV31421" }); //403(421)
			// Контроллер 4300 коридоры
			lightZones.Add(new LightZone { InputAddress = "4300.BV3140411", OutputAddresses = new ObservableCollection<string> { "17842.AO68353" }, OutputAlarmAddresses = new ObservableCollection<string> { "17842.AO68359" }, SetPointAddress = "4300.AV3140411" }); //404a1
			lightZones.Add(new LightZone { InputAddress = "4300.BV3140412", OutputAddresses = new ObservableCollection<string> { "17842.AO68352" }, OutputAlarmAddresses = new ObservableCollection<string> { "17842.AO68360" }, SetPointAddress = "4300.AV3140412" }); //404a2
			lightZones.Add(new LightZone { InputAddress = "4300.BV31404", OutputAddresses = new ObservableCollection<string> { "17842.AO68354" }, OutputAlarmAddresses = new ObservableCollection<string> { "17842.AO68361" }, SetPointAddress = "4300.AV31404" }); //404
			lightZones.Add(new LightZone { InputAddress = "4300.BV3140421", OutputAddresses = new ObservableCollection<string> { "17842.AO68355" }, OutputAlarmAddresses = new ObservableCollection<string> { "17842.AO68362" }, SetPointAddress = "4300.AV3140421" }); //404b1
			lightZones.Add(new LightZone { InputAddress = "4300.BV3140422", OutputAddresses = new ObservableCollection<string> { "17842.AO68356" }, OutputAlarmAddresses = new ObservableCollection<string> { "17842.AO68363" }, SetPointAddress = "4300.AV3140422" }); //404b2
			// Контроллер 4400 кабинеты
			lightZones.Add(new LightZone { InputAddress = "4400.BV31422", OutputAddresses = new ObservableCollection<string> { "17841.AO68096" }, SetPointAddress = "4400.AV31422" }); //407(422)
			lightZones.Add(new LightZone { InputAddress = "4400.BV31424", OutputAddresses = new ObservableCollection<string> { "17841.AO68355" }, SetPointAddress = "4400.AV31424" }); //409(424)
			lightZones.Add(new LightZone { InputAddress = "4400.BV31425", OutputAddresses = new ObservableCollection<string> { "17841.AO68356" }, SetPointAddress = "4400.AV31425" }); //408(425)
			lightZones.Add(new LightZone { InputAddress = "4400.BV31412", OutputAddresses = new ObservableCollection<string> { "17842.AO68358" }, SetPointAddress = "4400.AV31412" }); //415(412)
			lightZones.Add(new LightZone { InputAddress = "4400.BV31430", OutputAddresses = new ObservableCollection<string> { "17842.AO68615" }, SetPointAddress = "4400.AV31430" }); //410(430)
			lightZones.Add(new LightZone { InputAddress = "4400.BV31431", OutputAddresses = new ObservableCollection<string> { "17842.AO68614" }, SetPointAddress = "4400.AV31431" }); //411(431)
			lightZones.Add(new LightZone { InputAddress = "4400.BV31432", OutputAddresses = new ObservableCollection<string> { "17842.AO68613" }, SetPointAddress = "4400.AV31432" }); //413(432)
			lightZones.Add(new LightZone { InputAddress = "4400.BV31415", OutputAddresses = new ObservableCollection<string> { "17842.AO68099" }, SetPointAddress = "4400.AV31415" }); //412B(415)
			// Контроллер 4400 коридоры
			lightZones.Add(new LightZone { InputAddress = "4400.BV314231", OutputAddresses = new ObservableCollection<string> { "17841.AO68353" }, OutputAlarmAddresses = new ObservableCollection<string> { "17841.AO68357" }, SetPointAddress = "4400.AV314231" }); //4231
			lightZones.Add(new LightZone { InputAddress = "4400.BV314232", OutputAddresses = new ObservableCollection<string> { "17841.AO68352" }, OutputAlarmAddresses = new ObservableCollection<string> { "17841.AO68358" }, SetPointAddress = "4400.AV314232" }); //4232
			lightZones.Add(new LightZone { InputAddress = "4400.BV314233", OutputAddresses = new ObservableCollection<string> { "17841.AO68354" }, OutputAlarmAddresses = new ObservableCollection<string> { "17841.AO68359" }, SetPointAddress = "4400.AV314233" }); //4233
			return lightZones;
		}
	}
}
