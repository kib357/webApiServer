using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BACsharp;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;

namespace ControllersSetup
{
	class ControllersObjects
	{

#region objects names

		private const string TemperatureSetpoint = " Temperature setpoint";
		private const string CurrentTemperature = " Current temperature";
		private const string TemperatureBacstatAllowed = " Temperature bacstat allowed";
		private const string VentilationSetpoint = " Ventilation setpoint";
		private const string CurrentVentilationLevel = " Current ventilation level";
		private const string VentilationBacstatAllowed = " Ventilation bacstat allowed";
		private const string LightStateSetpoint = " Light state setpoint";
		private const string LightLevelSetpoint = " Light level setpoint";
		private const string AutoLightLevel = " Auto light level";
		private const string LightBacstatAllowed = " Light bacstat allowed";
		private const string ConditionerStateSetpoint = " Conditioner state setpoint";
		private const string ConditionerLevelSetpoint = " Conditioner level setpoint";
		private const string ShutterLevelSetpoint = " Shutter level setpoint";
		private const string ConditionerBacstatAllowed = " Conditioner bacstat allowed";
		private const string AirQualityTransmitter = " Air quality transmitter";
		private const string LeakSensor = " Leak sensor";
		private const string TemperatureSetpointMin = " Temperature setpoint min";
		private const string TemperatureSetpointMax = " Temperature setpoint max";
		private const string TActuator = " TActuator";
		private const string VentilationLCDSetpoint = " Ventilation LCD setpoint";
		private const string CurrentLightLevel = " Current light level";
		private const string LxLightLevelSetpoint = " Lx light level setpoint";
		private const string LightLevelLCDSetpont = " Light level LCD setpont";
		private const string LCDCurrentPage = " LCD current page";
#endregion

		private readonly Dictionary<string, string> _controllerAddresses;
		private readonly Dictionary<string, string> _vavAddresses;


		public Dictionary<string, KeyValuePair<uint?, string>> Rooms { get; set; }
		private readonly uint _controller;
		public uint Controller { get { return _controller; } }

		public ControllersObjects(uint controller)
		{
			_controllerAddresses = InitializeControllerAddresses();
			_vavAddresses = InitializeVAVAddresses();
			_controller = controller;
			
		}

		public ControllersObjects(uint controller, Dictionary<string, KeyValuePair<uint?, string>> rooms)
		{
			_controllerAddresses = InitializeControllerAddresses();
			_vavAddresses = InitializeVAVAddresses();
			_controller = controller;
			Rooms = rooms;
		}
		
		private static Dictionary<string, string> InitializeControllerAddresses()
		{
			var res = new Dictionary<string, string>
				          {
					          {TemperatureSetpoint, "AV11"},
					          {CurrentTemperature, "AV12"},
					          {TemperatureBacstatAllowed, "BV19"},
					          {VentilationSetpoint, "AV21"},
					          {CurrentVentilationLevel, "AV22"},
					          {VentilationBacstatAllowed, "BV29"},
					          {LightStateSetpoint, "BV31"},
					          {LightLevelSetpoint, "AV31"},
					          {AutoLightLevel, "BV32"},
					          {LightBacstatAllowed, "BV39"},
					          {ConditionerStateSetpoint, "BV41"},
					          {ConditionerLevelSetpoint, "AV41"},
					          {ShutterLevelSetpoint, "AV42"},
					          {ConditionerBacstatAllowed, "BV49"},
							  {AirQualityTransmitter, "AV51"},
							  {LeakSensor, "AV61"}
				          };

			return res;
		}

		private static Dictionary<string, string> InitializeVAVAddresses()
		{
			var res = new Dictionary<string, string>
				          {
					          {TemperatureSetpointMin, "AV13"},
					          {TemperatureSetpointMax, "AV14"},
					          {TActuator, "AV15"},

							  {VentilationLCDSetpoint, "AV23"},

					          {CurrentLightLevel, "AV32"},
					          {LxLightLevelSetpoint, "AV33"},
					          {LightLevelLCDSetpont, "AV35"},

					          {LCDCurrentPage, "AV91"}
				          };

			return res;
		}

		public void CreateAllObjects()
		{
			foreach (var room in Rooms)
			{
				CreateRoomObjects(room);
			}
		}

		public void WriteAllUnitsAndCovToObjects()
		{
			foreach (var room in Rooms)
			{
				CreateRoomObjects(room, false);
			}
		}

		public void CreateAllPG(string dir = @"C:\ControllersPrograms")
		{
			dir = dir + "\\" + Controller.ToString(CultureInfo.InvariantCulture);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			foreach (var room in Rooms)
			{
				var roomDir = dir + "\\" + room.Key;
				if (!Directory.Exists(roomDir))
					Directory.CreateDirectory(roomDir);
				var controllerPGDir = roomDir + "\\ControllerPrograms";
				var controllerVAVPGDir = roomDir + "\\VAVPrograms(" + room.Value.Key.ToString() + ")";
				if (!Directory.Exists(controllerPGDir))
					Directory.CreateDirectory(controllerPGDir);
				if (!Directory.Exists(controllerVAVPGDir))
					Directory.CreateDirectory(controllerVAVPGDir);

				if (room.Value.Key != null)
				{
					var roomNumber = GenCabNumber(room.Key);
					var fileName = controllerPGDir + "\\" + room.Key + "Variable control.txt";
					var tmp = CreateVariableControlPGForController(roomNumber, room.Value.Key.ToString());
					File.WriteAllLines(fileName, tmp);

					fileName = controllerVAVPGDir + "\\" + room.Key + "Variable control.txt";
					tmp = CreateVariableControlPGForVAV(roomNumber);
					File.WriteAllLines(fileName, tmp);

					CreateLCDPG(room.Key, room.Value.Value, controllerVAVPGDir);
				}
			}
		}

		private void CreateRoomObjects(KeyValuePair<string, KeyValuePair<uint?, string>> room, bool create = true)
		{
			if(_controller!=room.Value.Key)
				foreach (var controllerAddress in _controllerAddresses)
				{
					if (create)
						CreateObject(_controller, controllerAddress.Key, room.Key, controllerAddress.Value);
					else
						WriteObject(_controller, controllerAddress.Key, room.Key, controllerAddress.Value);
				}
			if (room.Value.Key == null) return;
			foreach (var controllerAddress in _controllerAddresses)
			{
				if (create)
					CreateObject((uint) room.Value.Key, controllerAddress.Key, room.Key, controllerAddress.Value);
				else
					WriteObject((uint) room.Value.Key, controllerAddress.Key, room.Key, controllerAddress.Value);
			}
			foreach (var vavAddress in _vavAddresses)
			{
				if (create)
					CreateObject((uint) room.Value.Key, vavAddress.Key, room.Key, vavAddress.Value);
				else
					WriteObject((uint) room.Value.Key, vavAddress.Key, room.Key, vavAddress.Value);
			}
		}

		private void CreateObject(uint controller, string objectDescription, string room, string writedObject)
		{
			var objProperties = new List<BACnetPropertyValue>
				                    {
					                    new BACnetPropertyValue((int) BacnetPropertyId.ObjectName,
					                                            new List<BACnetDataType>
						                                            {new BACnetCharacterString(room + objectDescription)})
				                    };
			if (objectDescription.ToLower().Contains("temperature") && writedObject.Contains("AV"))
			{
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.Units,
				                                          new List<BACnetDataType> {new BACnetEnumerated(62)}));
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.COVIncrement,
				                                          new List<BACnetDataType> {new BACnetReal((float) 0.5)}));
			}
			if (objectDescription == TemperatureSetpointMin)
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.PresentValue,
				                                          new List<BACnetDataType> {new BACnetReal(16)}));
			if (objectDescription == TemperatureSetpointMax)
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.PresentValue,
				                                          new List<BACnetDataType> {new BACnetReal(26)}));

			if (objectDescription == VentilationSetpoint || objectDescription == VentilationLCDSetpoint
			    || objectDescription == LightLevelLCDSetpont || objectDescription == LCDCurrentPage
			    || objectDescription == ConditionerLevelSetpoint || objectDescription == ShutterLevelSetpoint)
			{
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.Units,
				                                          new List<BACnetDataType> {new BACnetEnumerated(95)}));
			}
			if (objectDescription == LxLightLevelSetpoint || objectDescription == CurrentLightLevel)
			{
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.Units,
				                                          new List<BACnetDataType> {new BACnetEnumerated(37)}));
			}

			string tmpStr = GenCabNumber(room);
			string createdObject = writedObject + tmpStr;
			ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].Create(objProperties);
		}

		private void WriteObject(uint controller, string objectDescription, string room, string writedObject)
		{
			string tmpStr = GenCabNumber(room);
			string createdObject = writedObject + tmpStr;

			if (objectDescription.ToLower().Contains("temperature") && writedObject.Contains("AV"))
			{
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet(0.5, BacnetPropertyId.COVIncrement);
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet("°C", BacnetPropertyId.Units);
			}
			if (objectDescription == TemperatureSetpointMin)
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet(16);
			if (objectDescription == TemperatureSetpointMax)
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet(26);
			if (objectDescription == VentilationSetpoint || objectDescription == VentilationLCDSetpoint
				|| objectDescription == LightLevelLCDSetpont || objectDescription == LCDCurrentPage
				|| objectDescription == ConditionerLevelSetpoint || objectDescription == ShutterLevelSetpoint)
			{
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet("", BacnetPropertyId.Units);
			}
			if (objectDescription == LxLightLevelSetpoint || objectDescription == CurrentLightLevel)
			{
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet("lx", BacnetPropertyId.Units);
			}
		}

		private string GenCabNumber(string room)
		{
			var start = room.IndexOf('(');
			var end = room.IndexOf(')');
			var tmp = new char[0];
			Array.Resize(ref tmp, end - start - 1);
			room.CopyTo(start + 1, tmp, 0, end - start - 1);
			var tmpStr = string.Empty;
			foreach (var chr in tmp)
			{
				if (chr == 'A' || chr == 'a')
				{
					tmpStr = tmpStr + "1";
					continue;
				}
				if (chr == 'B' || chr == 'b')
				{
					tmpStr = tmpStr + "2";
					continue;
				}
				if (chr == 'C' || chr == 'c')
				{
					tmpStr = tmpStr + "3";
					continue;
				}
				tmpStr = tmpStr + chr;
			}
			return tmpStr;
		}

		private string[] CreateVariableControlPGForController(string roomNumber, string vavAddress)
		{
			var temperatureSetpoint = _controllerAddresses[TemperatureSetpoint] + roomNumber;
			var currentTemperature = _controllerAddresses[CurrentTemperature] + roomNumber;
			var ventilationSetpoint = _controllerAddresses[VentilationSetpoint] + roomNumber;
			var currentVentilationLevel = _controllerAddresses[CurrentVentilationLevel] + roomNumber;
			var lightLevelSetpont = _controllerAddresses[LightLevelSetpoint] + roomNumber;
			var autoLightLevelSetpont = _controllerAddresses[AutoLightLevel] + roomNumber;
			var conditionerLevelSetpoint = _controllerAddresses[ConditionerLevelSetpoint] + roomNumber;
			var shutterLevelSetpoint = _controllerAddresses[ShutterLevelSetpoint] + roomNumber;
			var airQualityTransmitter = _controllerAddresses[AirQualityTransmitter] + roomNumber;
			var leakSensor = _controllerAddresses[LeakSensor] + roomNumber;

			var sb = new StringBuilder();
			sb.AppendLine("//Temperature");
			sb.AppendLine(temperatureSetpoint + " = " + vavAddress + "." + temperatureSetpoint);
			sb.AppendLine(currentTemperature + " = " + vavAddress + "." + currentTemperature);
			sb.AppendLine("");
			sb.AppendLine("//Ventelation");
			sb.AppendLine(ventilationSetpoint + " = " + vavAddress + "." + ventilationSetpoint);
			sb.AppendLine(currentVentilationLevel + " = " + vavAddress + "." + currentVentilationLevel);
			sb.AppendLine("");
			sb.AppendLine("//Light");
			sb.AppendLine(lightLevelSetpont + " = " + vavAddress + "." + lightLevelSetpont);
			sb.AppendLine(autoLightLevelSetpont + " = " + vavAddress + "." + autoLightLevelSetpont);
			sb.AppendLine("");
			sb.AppendLine("//Conditioner");
			sb.AppendLine(conditionerLevelSetpoint + " = " + vavAddress + "." + conditionerLevelSetpoint);
			sb.AppendLine(shutterLevelSetpoint + " = " + vavAddress + "." + shutterLevelSetpoint);
			sb.AppendLine("//Air quality");
			sb.AppendLine("//" + airQualityTransmitter + " = " + vavAddress + ".AI");
			sb.AppendLine("//Leak sensor");
			sb.AppendLine("//" + leakSensor + " = " + vavAddress + ".MI");
			return new[] {sb.ToString()};
		}

		private string[] CreateVariableControlPGForVAV(string roomNumber)
		{
			var temperatureSetpoint = _controllerAddresses[TemperatureSetpoint] + roomNumber;
			var ventilationSetpoint = _controllerAddresses[VentilationSetpoint] + roomNumber;
			var lightLevelSetpont = _controllerAddresses[LightLevelSetpoint] + roomNumber;
			var lightStateSetpont = _controllerAddresses[LightStateSetpoint] + roomNumber;
			var autoLightLevel = _controllerAddresses[AutoLightLevel] + roomNumber;
			var conditionerLevelSetpoint = _controllerAddresses[ConditionerLevelSetpoint] + roomNumber;
			var shutterLevelSetpoint = _controllerAddresses[ShutterLevelSetpoint] + roomNumber;
			var temperatureBacstatAllowed = _controllerAddresses[TemperatureBacstatAllowed] + roomNumber;
			var ventilationBacstatAllowed = _controllerAddresses[VentilationBacstatAllowed] + roomNumber;
			var conditionerBacstatAllowed = _controllerAddresses[ConditionerBacstatAllowed] + roomNumber;
			var controller = _controller.ToString(CultureInfo.InvariantCulture);
			var lightBacstatAllowed = _controllerAddresses[LightBacstatAllowed] + roomNumber;

			var sb = new StringBuilder();
			sb.AppendLine("//Temperature");
			sb.AppendLine(temperatureSetpoint + " = " + controller + "." + temperatureSetpoint);
			sb.AppendLine(temperatureBacstatAllowed + " = " + controller + "." + temperatureBacstatAllowed);
			sb.AppendLine("");
			sb.AppendLine("//Ventilation");
			sb.AppendLine(ventilationSetpoint + " = " + controller + "." + ventilationSetpoint);
			sb.AppendLine(ventilationBacstatAllowed + " = " + controller + "." + ventilationBacstatAllowed);
			sb.AppendLine("");
			sb.AppendLine("//Light");
			sb.AppendLine(lightStateSetpont + " = " + controller + "." + lightStateSetpont);
			sb.AppendLine(lightLevelSetpont + " = " + controller + "." + lightLevelSetpont);
			sb.AppendLine(autoLightLevel + " = " + controller + "." + autoLightLevel);
			sb.AppendLine(lightBacstatAllowed + " = " + controller + "." + lightBacstatAllowed);
			sb.AppendLine("");
			sb.AppendLine("//Conditioner");
			sb.AppendLine(conditionerLevelSetpoint + " = " + controller + "." + conditionerLevelSetpoint);
			sb.AppendLine(shutterLevelSetpoint + " = " + controller + "." + shutterLevelSetpoint);
			sb.AppendLine(conditionerBacstatAllowed + " = " + controller + "." + conditionerBacstatAllowed);
			return new[] {sb.ToString()};
		}

		public void CreateLCDPG(string room, string lcdAddress, string dir)
		{
			var roomNumber = GenCabNumber(room);
			dir = dir + "\\";

			var fileName = dir  + room + "LCD control.txt";
			var tmp = GenLCDPg(roomNumber, lcdAddress);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + room + "LCD temperature.txt";
			tmp = GenLCDTemperaturePg(roomNumber, lcdAddress);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + room + "LCD ventilation.txt";
			tmp = GenLCDVentilationPg(roomNumber, lcdAddress);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + room + "LCD light.txt";
			tmp = GenLCDLightPg(roomNumber, lcdAddress);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + room + "LCD condition.txt";
			tmp = GenLCDConditionPg(roomNumber, lcdAddress);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + room + "LCD conditioner shutter.txt";
			tmp = GenLCDConditionerShutterPg(roomNumber, lcdAddress);
			File.WriteAllLines(fileName, tmp);
		}

		private string[] GenLCDPg(string roomNumber, string lcdAddress)
		{
			var lcdCurrentPage = _vavAddresses[LCDCurrentPage] + roomNumber;
			var lcdTemperaturePG = "PG3" + roomNumber;
			var lcdVentilationPG = "PG4" + roomNumber;
			var lcdLightPG = "PG5" + roomNumber;
			var lcdConditionPG = "PG6" + roomNumber;
			var lcdConditionerShutterPG = "PG7" + roomNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("// LCDCurrentPage = 1 Temperature Control");
			sb.AppendLine("// LCDCurrentPage = 2 Vent Control");
			sb.AppendLine("// LCDCurrentPage = 3 Light Level Control");
			sb.AppendLine("// LCDCurrentPage = 4 Conditioner Level Control");
			sb.AppendLine("// LCDCurrentPage = 5 Conditioner Shutter Control");
			sb.AppendLine("");
			sb.AppendLine("If " + lcdCurrentPage + " < 1 Or " + lcdCurrentPage + " > 5 Then");
			sb.AppendLine("  " + lcdCurrentPage + " = 1");
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("IfOnce LCD" + lcdAddress + ".KeyPress = 2 Then");
			sb.AppendLine("	" + lcdCurrentPage + " = " + lcdCurrentPage + " + 1");
			sb.AppendLine("	If " + lcdCurrentPage + " > 5 Then");
			sb.AppendLine("		" + lcdCurrentPage + " = 1");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("//Temperature");
			sb.AppendLine("If " + lcdCurrentPage + " = 1 Then");
			sb.AppendLine("	Call " + lcdTemperaturePG);
			sb.AppendLine("");
			sb.AppendLine("//Ventilation");
			sb.AppendLine("ElseIf  " + lcdCurrentPage + " = 2 Then");
			sb.AppendLine("	Call " + lcdVentilationPG);
			sb.AppendLine("");
			sb.AppendLine("//Light");
			sb.AppendLine("ElseIf  " + lcdCurrentPage + " = 3 Then");
			sb.AppendLine("	Call " + lcdLightPG);
			sb.AppendLine("");
			sb.AppendLine("ElseIf " + lcdCurrentPage + " = 4 Then");
			sb.AppendLine("	Call " + lcdConditionPG);
			sb.AppendLine("");
			sb.AppendLine("ElseIf " + lcdCurrentPage + " = 5 Then");
			sb.AppendLine("	Call " + lcdConditionerShutterPG);
			sb.AppendLine("End If");
			return new[] { sb.ToString() };
		}

		private string[] GenLCDTemperaturePg(string roomNumber, string lcdAddress)
		{
			var temperatureSetpoint = _controllerAddresses[TemperatureSetpoint] + roomNumber;
			var currentTemperature = _controllerAddresses[CurrentTemperature] + roomNumber;
			var temperatureBacstatAllowed = _controllerAddresses[TemperatureBacstatAllowed] + roomNumber;
			var temperatureSetpointMin = _vavAddresses[TemperatureSetpointMin] + roomNumber;
			var temperatureSetpointMax = _vavAddresses[TemperatureSetpointMax] + roomNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable temperatureSetpoint As Real");
			sb.AppendLine("");
			sb.AppendLine("LCD" + lcdAddress + ".Line2 = " + "\" " + "\"");
			sb.AppendLine("LCD" + lcdAddress + ".Cooling = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Heating = 0");
			sb.AppendLine("temperatureSetpoint = " + temperatureSetpoint);
			sb.AppendLine("If " + temperatureBacstatAllowed + " On Then");
			sb.AppendLine("	IfOnce LCD" + lcdAddress + ".KeyPress = 3 Then");
			sb.AppendLine("		temperatureSetpoint = temperatureSetpoint - 0.5");
			sb.AppendLine("	End If");
			sb.AppendLine("	IfOnce LCD" + lcdAddress + ".KeyPress = 4 Then");
			sb.AppendLine("		temperatureSetpoint = temperatureSetpoint + 0.5");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine("temperatureSetpoint = Limit (temperatureSetpoint, " + temperatureSetpointMin + ", " + temperatureSetpointMax + ")");
			sb.AppendLine(temperatureSetpoint + " = temperatureSetpoint");
			sb.AppendLine("LCD" + lcdAddress + ".Line2 = " + currentTemperature);
			sb.AppendLine("LCD" + lcdAddress + ".Line3 = temperatureSetpoint & \"^C\"");
			sb.AppendLine("LCD" + lcdAddress + ".Fan = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Sun = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Line2Units = 1");
			sb.AppendLine("LCD" + lcdAddress + ".Line3Units = 0");
			return new[] { sb.ToString() };
		}

		private string[] GenLCDVentilationPg(string roomNumber, string lcdAddress)
		{
			var ventilationSetpoint = _controllerAddresses[VentilationSetpoint] + roomNumber;
			var ventilationBacstatAllowed = _controllerAddresses[VentilationBacstatAllowed] + roomNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable ventilationSetpoint As Integer");
			sb.AppendLine("");
			sb.AppendLine("LCD" + lcdAddress + ".Line2 = " + "\"BEH\"");
			sb.AppendLine("LCD" + lcdAddress + ".Cooling = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Heating = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Line2Units = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Line3Units = 0");
			sb.AppendLine("ventilationSetpoint = " + ventilationSetpoint);
			sb.AppendLine("If " + ventilationBacstatAllowed + " On Then");
			sb.AppendLine("	IfOnce LCD" + lcdAddress + ".KeyPress = 3 Then");
			sb.AppendLine("		ventilationSetpoint = ventilationSetpoint - 1");
			sb.AppendLine("	End If");
			sb.AppendLine("	IfOnce LCD" + lcdAddress + ".KeyPress = 4 Then");
			sb.AppendLine("		ventilationSetpoint = ventilationSetpoint + 1");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine("ventilationSetpoint = Limit (ventilationSetpoint, - 3, 3)");
			sb.AppendLine(ventilationSetpoint + " = ventilationSetpoint");
			sb.AppendLine("If ventilationSetpoint = 0 Then");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = \"auto\"");
			sb.AppendLine("Else");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = ventilationSetpoint");
			sb.AppendLine("End If");
			return new[] { sb.ToString() };
		}

		private string[] GenLCDLightPg(string roomNumber, string lcdAddress)
		{
			var lightLevelLCDSetpont = _vavAddresses[LightLevelLCDSetpont] + roomNumber;
			var lightLevel = _controllerAddresses[LightLevelSetpoint] + roomNumber;
			var autoLightLevelSetpont = _controllerAddresses[AutoLightLevel] + roomNumber;
			var lightBacstatAllowed = _controllerAddresses[LightBacstatAllowed] + roomNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable lightLevelLCDSetpoint As Integer");
			sb.AppendLine("Variable lightLevelSetpoint As Integer");
			sb.AppendLine("Variable autoLightLevelSetpoint As Integer");
			sb.AppendLine("");
			sb.AppendLine("	LCD" + lcdAddress + ".Line2 = \"CBE\"");
			sb.AppendLine("	LCD" + lcdAddress + ".Cooling = 0");
			sb.AppendLine("	LCD" + lcdAddress + ".Heating = 0");
			sb.AppendLine("	LCD" + lcdAddress + ".Line2Units = 0");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3Units = 0");
			sb.AppendLine("	lightLevelLCDSetpoint = " + lightLevelLCDSetpont);
			sb.AppendLine("	lightLevelSetpoint = " + lightLevel);
			sb.AppendLine("	AutoLightLevelSetpoint = " + autoLightLevelSetpont);
			sb.AppendLine("	If " + lightBacstatAllowed + " On Then");
			sb.AppendLine("		IfOnce LCD" + lcdAddress + ".KeyPress = 3 Then");
			sb.AppendLine("			lightLevelLCDSetpoint = lightLevelLCDSetpoint - 1");
			sb.AppendLine("		End If");
			sb.AppendLine("		IfOnce LCD" + lcdAddress + ".KeyPress = 4 Then");
			sb.AppendLine("			lightLevelLCDSetpoint = lightLevelLCDSetpoint + 1");
			sb.AppendLine("		End If");
			sb.AppendLine("		lightLevelLCDSetpoint = Limit (lightLevelLCDSetpoint, - 3, 3)");
			sb.AppendLine("		If lightLevelLCDSetpoint = - 3 Then");
			sb.AppendLine("			autoLightLevelSetpoint = 0");
			sb.AppendLine("			lightLevelSetpoint = 1");
			sb.AppendLine("		ElseIf  lightLevelLCDSetpoint = - 2 Then");
			sb.AppendLine("			autoLightLevelSetpoint = 0");
			sb.AppendLine("			lightLevelSetpoint = 20");
			sb.AppendLine("		ElseIf  lightLevelLCDSetpoint = - 1 Then");
			sb.AppendLine("			autoLightLevelSetpoint = 0");
			sb.AppendLine("			lightLevelSetpoint = 40");
			sb.AppendLine("		ElseIf  lightLevelLCDSetpoint = 0 Then");
			sb.AppendLine("			autoLightLevelSetpoint = 1");
			sb.AppendLine("		ElseIf  lightLevelLCDSetpoint = 1 Then");
			sb.AppendLine("			autoLightLevelSetpoint = 0");
			sb.AppendLine("			lightLevelSetpoint = 60");
			sb.AppendLine("		ElseIf  lightLevelLCDSetpoint = 2 Then");
			sb.AppendLine("			autoLightLevelSetpoint = 0");
			sb.AppendLine("			lightLevelSetpoint = 80");
			sb.AppendLine("		ElseIf  lightLevelLCDSetpoint = 3 Then");
			sb.AppendLine("			autoLightLevelSetpoint = 0");
			sb.AppendLine("			lightLevelSetpoint = 100");
			sb.AppendLine("		End If");
			sb.AppendLine("	End If");
			sb.AppendLine("	" + lightLevelLCDSetpont + " = lightLevelLCDSetpoint");
			sb.AppendLine("	" + lightLevel + " = lightLevelSetpoint");
			sb.AppendLine("	" + autoLightLevelSetpont + " = autoLightLevelSetpoint");
			sb.AppendLine("	If lightLevelLCDSetpoint = 0 Then");
			sb.AppendLine("		LCD" + lcdAddress + ".Line3 = \"auto\"");
			sb.AppendLine("	Else");
			sb.AppendLine("		LCD" + lcdAddress + ".Line3 = lightLevelLCDSetpoint");
			sb.AppendLine("	End If");
			sb.AppendLine(" LCD" + lcdAddress + ".Line2Units = 0");
			return new[] { sb.ToString() };
		}

		private string[] GenLCDConditionPg(string roomNumber, string lcdAddress)
		{
			var conditionerLevelSetpoint = _controllerAddresses[ConditionerLevelSetpoint] + roomNumber;
			var conditionerBacstatAllowed = _controllerAddresses[ConditionerBacstatAllowed] + roomNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable conditionerSetpoint As Integer");
			sb.AppendLine("");
			sb.AppendLine("LCD" + lcdAddress + ".Line2 = \"KOH\"");
			sb.AppendLine("LCD" + lcdAddress + ".Line2Units = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Line3Units = 0");
			sb.AppendLine("conditionerSetpoint = " + conditionerLevelSetpoint);
			sb.AppendLine("If " + conditionerBacstatAllowed + " Then");
			sb.AppendLine("	IfOnce LCD" + lcdAddress + ".KeyPress = 3 Then");
			sb.AppendLine("		conditionerSetpoint = Limit (conditionerSetpoint - 1, 0, 4)");
			sb.AppendLine("	End If");
			sb.AppendLine("	IfOnce LCD" + lcdAddress + ".KeyPress = 4 Then");
			sb.AppendLine("		conditionerSetpoint = Limit (conditionerSetpoint + 1, 0, 4)");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine(conditionerLevelSetpoint + " = conditionerSetpoint");
			sb.AppendLine("If conditionerSetpoint = 0 Then");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = Off");
			sb.AppendLine("Else");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = conditionerSetpoint");
			sb.AppendLine("End If");
			sb.AppendLine("LCD" + lcdAddress + ".Fan = conditionerSetpoint");
			return new[] {sb.ToString()};
		}

		private string[] GenLCDConditionerShutterPg(string roomNumber, string lcdAddress)
		{
			var shutterLevelSetpoint = _controllerAddresses[ShutterLevelSetpoint] + roomNumber;
			var conditionerBacstatAllowed = _controllerAddresses[ConditionerBacstatAllowed] + roomNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable shutterLevel As Integer");
			sb.AppendLine("");
			sb.AppendLine("LCD" + lcdAddress + ".Line2 = \"KO2\"");
			sb.AppendLine("LCD" + lcdAddress + ".Line2Units = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Line3Units = 4");
			sb.AppendLine("LCD" + lcdAddress + ".Cooling = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Heating = 0");
			sb.AppendLine("LCD" + lcdAddress + ".Fan = 0");
			sb.AppendLine("shutterLevel = " + shutterLevelSetpoint);
			sb.AppendLine("If " + conditionerBacstatAllowed + " Then");
			sb.AppendLine("	IfOnce LCD" + lcdAddress + ".KeyPress = 3 Then");
			sb.AppendLine("		shutterLevel = Limit (shutterLevel - 1, 1, 5)");
			sb.AppendLine("	End If");
			sb.AppendLine("	IfOnce LCD" + lcdAddress + ".KeyPress = 4 Then");
			sb.AppendLine("		shutterLevel = Limit (shutterLevel + 1, 1, 5)");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine(shutterLevelSetpoint + " = shutterLevel");
			sb.AppendLine("If shutterLevel = 1 Then");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3Units = 0");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = \"Hor\"");
			sb.AppendLine("End If");
			sb.AppendLine("If shutterLevel = 2 Then");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = 60");
			sb.AppendLine("End If");
			sb.AppendLine("If shutterLevel = 3 Then");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = 80");
			sb.AppendLine("End If");
			sb.AppendLine("If shutterLevel = 4 Then");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = 100");
			sb.AppendLine("End If");
			sb.AppendLine("If shutterLevel = 5 Then");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3Units = 0");
			sb.AppendLine("	LCD" + lcdAddress + ".Line3 = \"Sw\"");
			sb.AppendLine("End If");
			return new[] {sb.ToString()};
		}
	}
}
