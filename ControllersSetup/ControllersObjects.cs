using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;

namespace ControllersSetup
{
	class ControllersObjects
	{
		private readonly Dictionary<string, string> _controllerAddresses;
		private readonly Dictionary<string, string> _vavAddresses; 


		public Dictionary<string, uint?> Cabinetes { get; set; }
		private readonly uint _controller;
		public uint Controller { get { return _controller; } }

		public ControllersObjects(uint controller)
		{
			_controllerAddresses = InitializeControllerAddresses();
			_vavAddresses = InitializeVAVAddresses();
			_controller = controller;
			
		}

		public ControllersObjects(uint controller, Dictionary<string, uint?> cabinetes)
		{
			_controllerAddresses = InitializeControllerAddresses();
			_vavAddresses = InitializeVAVAddresses();
			_controller = controller;
			Cabinetes = cabinetes;
		}

		private static Dictionary<string, string> InitializeControllerAddresses()
		{
			var res = new Dictionary<string, string>
				          {
					          {"TemperatureSetpoint", "AV11"},
					          {"CurrentTemperature", "AV12"},
					          {"TemperatureBacstatAllowed", "BV19"},
					          {"VentilationSetpoint", "AV21"},
					          {"CurrentVentilationLevel", "AV22"},
					          {"VentilationBacstatAllowed", "BV29"},
					          {"LightStateSetpoint", "BV31"},
					          {"LightLevelSetpoint", "AV31"},
					          {"AutoLightLevel", "BV32"},
					          {"LightBacstatAllowed", "BV39"},
					          {"ConditionerStateSetpoint", "BV41"},
					          {"ConditionerLevelSetpoint", "AV41"},
					          {"ShutterLevelSetpoint", "AV42"},
					          {"ConditionerBacstatAllowed", "BV49"}
				          };

			return res;
		}

		private static Dictionary<string, string> InitializeVAVAddresses()
		{
			var res = new Dictionary<string, string>
				          {
					          {"TemperatureSetpointMin", "AV13"},
					          {"TemperatureSetpointMax", "AV14"},
					          {"TActuator", "AV15"},

							  {"VentilationLCDSetpoint", "AV23"},

					          {"CurrentLightLevel", "AV32"},
					          {"MinLightLevel", "AV33"},
					          {"MaxLightLevel", "AV34"},
					          {"LightLevelLCDSetpont", "AV35"},

					          {"WaitLightSensorResponse", "BV91"},
					          {"LCDCurrentPage", "AV91"}
				          };

			return res;
		}

		public void CreateAllObjects()
		{
			foreach (var cabinete in Cabinetes)
			{
				CreateCabineteObjects(cabinete);
			}
		}

		public void WriteAllUnitsAndCovToObjects()
		{
			foreach (var cabinete in Cabinetes)
			{
				CreateCabineteObjects(cabinete, false);
			}
		}

		public void CreateAllPG(string dir = @"C:\ControllersPrograms")
		{
			dir = dir + "\\" + Controller.ToString(CultureInfo.InvariantCulture);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			foreach (var cab in Cabinetes)
			{
				var cabDir = dir + "\\" + cab.Key;
				if (!Directory.Exists(cabDir))
					Directory.CreateDirectory(cabDir);
				var controllerPGDir = cabDir + "\\ControllerPrograms";
				var controllerVAVPGDir = cabDir + "\\VAVPrograms(" + cab.Value.ToString() + ")";
				if (!Directory.Exists(controllerPGDir))
					Directory.CreateDirectory(controllerPGDir);
				if (!Directory.Exists(controllerVAVPGDir))
					Directory.CreateDirectory(controllerVAVPGDir);

				if (cab.Value != null)
				{
					var cabNumber = GenCabNumber(cab.Key);
					var fileName = controllerPGDir + "\\" + cab.Key + "VariableControl.txt";
					var tmp = CreateVariableControlPGForController(cabNumber, cab.Value.ToString());
					File.WriteAllLines(fileName, tmp);

					fileName = controllerVAVPGDir + "\\" + cab.Key + "VariableControl.txt";
					tmp = CreateVariableControlPGForVAV(cabNumber);
					File.WriteAllLines(fileName, tmp);

					CreateLCDPG(cab.Key, controllerVAVPGDir);
				}
			}
		}

		private void CreateCabineteObjects(KeyValuePair<string, uint?> cabinete, bool create = true)
		{
			foreach (var controllerAddress in _controllerAddresses)
			{
				if (create)
					CreateObject(_controller, controllerAddress.Key, cabinete.Key, controllerAddress.Value);
				else
					WriteObject(_controller, controllerAddress.Key, cabinete.Key, controllerAddress.Value);
			}
			if (cabinete.Value == null) return;
			foreach (var controllerAddress in _controllerAddresses)
			{
				if (create)
					CreateObject((uint) cabinete.Value, controllerAddress.Key, cabinete.Key, controllerAddress.Value);
				else
					WriteObject((uint) cabinete.Value, controllerAddress.Key, cabinete.Key, controllerAddress.Value);
			}
			foreach (var vavAddress in _vavAddresses)
			{
				if (create)
					CreateObject((uint) cabinete.Value, vavAddress.Key, cabinete.Key, vavAddress.Value);
				else
					WriteObject((uint) cabinete.Value, vavAddress.Key, cabinete.Key, vavAddress.Value);
			}
		}

		private void CreateObject(uint controller, string objectDescription, string cabinete, string writedObject)
		{
			var objProperties = new List<BACnetPropertyValue>
				                    {
					                    new BACnetPropertyValue((int) BacnetPropertyId.ObjectName,
					                                            new List<BACnetDataType>
						                                            {new BACnetCharacterString(cabinete + objectDescription)})
				                    };
			if (objectDescription.Contains("Temperature") && writedObject.Contains("AV"))
			{
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.Units,
				                                          new List<BACnetDataType> {new BACnetEnumerated(62)}));
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.COVIncrement,
				                                          new List<BACnetDataType> {new BACnetReal((float) 0.5)}));
			}
			if (objectDescription == "TemperatureSetpointMin")
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.PresentValue,
				                                          new List<BACnetDataType> {new BACnetReal(16)}));
			if (objectDescription == "TemperatureSetpointMax")
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.PresentValue,
				                                          new List<BACnetDataType> {new BACnetReal(26)}));

			if (objectDescription == "VentilationSetpoint" || objectDescription == "VentilationLCDSetpoint"
			    || objectDescription == "LightLevelLCDSetpont" || objectDescription == "LCDCurrentPage"
			    || objectDescription == "ConditionerLevelSetpoint" || objectDescription == "ShutterLevelSetpoint")
			{
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.Units,
				                                          new List<BACnetDataType> {new BACnetEnumerated(95)}));
			}
			if (objectDescription == "MinLightLevel" || objectDescription == "MaxLightLevel"
			    || objectDescription == "CurrentLightLevel")
			{
				objProperties.Add(new BACnetPropertyValue((int) BacnetPropertyId.Units,
				                                          new List<BACnetDataType> {new BACnetEnumerated(37)}));
			}

			string tmpStr = GenCabNumber(cabinete);
			string createdObject = writedObject + tmpStr;
			ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].Create(objProperties);
		}

		private void WriteObject(uint controller, string objectDescription, string cabinete, string writedObject)
		{
			string tmpStr = GenCabNumber(cabinete);
			string createdObject = writedObject + tmpStr;

			if (objectDescription.Contains("Temperature") && writedObject.Contains("AV"))
			{
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet(0.5, BacnetPropertyId.COVIncrement);
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet("°C", BacnetPropertyId.Units);
			}
			if (objectDescription == "TemperatureSetpointMin")
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet(16);
			if (objectDescription == "TemperatureSetpointMax")
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet(26);
			if (objectDescription == "VentilationSetpoint" || objectDescription == "VentilationLCDSetpoint"
				|| objectDescription == "LightLevelLCDSetpont" || objectDescription == "LCDCurrentPage"
				|| objectDescription == "ConditionerLevelSetpoint" || objectDescription == "ShutterLevelSetpoint")
			{
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet("", BacnetPropertyId.Units);
			}
			if (objectDescription == "MinLightLevel" || objectDescription == "MaxLightLevel"
				|| objectDescription == "CurrentLightLevel")
			{
				ControllerSetupViewModel.Bacnet[controller].Objects[createdObject].BeginSet("lx", BacnetPropertyId.Units);
			}
		}

		private string GenCabNumber(string cabinete)
		{
			var start = cabinete.IndexOf('(');
			var end = cabinete.IndexOf(')');
			var tmp = new char[0];
			Array.Resize(ref tmp, end - start - 1);
			cabinete.CopyTo(start + 1, tmp, 0, end - start - 1);
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

		private string[] CreateVariableControlPGForController(string cabNumber, string vavAddress)
		{
			var temperatureSetpoint = _controllerAddresses["TemperatureSetpoint"] + cabNumber;
			var currentTemperature = _controllerAddresses["CurrentTemperature"] + cabNumber;
			var ventilationSetpoint = _controllerAddresses["VentilationSetpoint"] + cabNumber;
			var currentVentilationLevel = _controllerAddresses["CurrentVentilationLevel"] + cabNumber;
			var lightLevelSetpont = _controllerAddresses["LightLevelSetpoint"] + cabNumber;
			var autoLightLevelSetpont = _controllerAddresses["AutoLightLevel"] + cabNumber;
			var conditionerLevelSetpoint = _controllerAddresses["ConditionerLevelSetpoint"] + cabNumber;
			var shutterLevelSetpoint = _controllerAddresses["ShutterLevelSetpoint"] + cabNumber;

			var sb = new StringBuilder();
			sb.AppendLine("//Temperature");
			sb.AppendLine("If " + vavAddress + "." + temperatureSetpoint + " Changed Then");
			sb.AppendLine("  " + temperatureSetpoint + " = " + vavAddress + "." + temperatureSetpoint);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + vavAddress + "." + currentTemperature + " Changed Then");
			sb.AppendLine("  " + currentTemperature + " = " + vavAddress + "." + currentTemperature);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("//Ventelation");
			sb.AppendLine("If " + vavAddress + "." + ventilationSetpoint + " Changed Then");
			sb.AppendLine("  " + ventilationSetpoint + " = " + vavAddress + "." + ventilationSetpoint);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + vavAddress + "." + currentVentilationLevel + " Changed Then");
			sb.AppendLine("  " + currentVentilationLevel + " = " + vavAddress + "." + currentVentilationLevel);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("//Light");
			sb.AppendLine("If " + vavAddress + "." + lightLevelSetpont + " Changed Then");
			sb.AppendLine("  " + lightLevelSetpont + " = " + vavAddress + "." + lightLevelSetpont);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + vavAddress + "." + autoLightLevelSetpont + " Changed Then");
			sb.AppendLine("  " + autoLightLevelSetpont + " = " + vavAddress + "." + autoLightLevelSetpont);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("//Conditioner");
			sb.AppendLine("If " + vavAddress + "." + conditionerLevelSetpoint + " Changed Then");
			sb.AppendLine("  " + conditionerLevelSetpoint + " = " + vavAddress + "." + conditionerLevelSetpoint);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + vavAddress + "." + shutterLevelSetpoint + " Changed Then");
			sb.AppendLine("  " + shutterLevelSetpoint + " = " + vavAddress + "." + shutterLevelSetpoint);
			sb.AppendLine("End If");
			return new[] {sb.ToString()};
		}

		private string[] CreateVariableControlPGForVAV(string cabNumber)
		{
			var temperatureSetpoint = _controllerAddresses["TemperatureSetpoint"] + cabNumber;
			var ventilationSetpoint = _controllerAddresses["VentilationSetpoint"] + cabNumber;
			var lightLevelSetpont = _controllerAddresses["LightLevelSetpoint"] + cabNumber;
			var autoLightLevel = _controllerAddresses["AutoLightLevel"] + cabNumber;
			var conditionerLevelSetpoint = _controllerAddresses["ConditionerLevelSetpoint"] + cabNumber;
			var shutterLevelSetpoint = _controllerAddresses["ShutterLevelSetpoint"] + cabNumber;
			var temperatureBacstatAllowed = _controllerAddresses["TemperatureBacstatAllowed"] + cabNumber;
			var ventilationBacstatAllowed = _controllerAddresses["VentilationBacstatAllowed"] + cabNumber;
			var conditionerBacstatAllowed = _controllerAddresses["ConditionerBacstatAllowed"] + cabNumber;
			var controller = _controller.ToString(CultureInfo.InvariantCulture);
			var lightBacstatAllowed = _controllerAddresses["LightBacstatAllowed"] + cabNumber;

			var sb = new StringBuilder();
			sb.AppendLine("If " + controller + "." + temperatureSetpoint + " Changed Then");
			sb.AppendLine("   " + temperatureSetpoint + " = " + controller + "." + temperatureSetpoint);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + temperatureBacstatAllowed + " Changed Then");
			sb.AppendLine("   " + temperatureBacstatAllowed + " = " + controller + "." + temperatureBacstatAllowed);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + ventilationSetpoint + " Changed Then");
			sb.AppendLine("   " + ventilationSetpoint + " = " + controller + "." + ventilationSetpoint);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + ventilationBacstatAllowed + " Changed Then");
			sb.AppendLine("   " + ventilationBacstatAllowed + " = " + controller + "." + ventilationBacstatAllowed);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + lightLevelSetpont + " Changed Then");
			sb.AppendLine("   " + lightLevelSetpont + " = " + controller + "." + lightLevelSetpont);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + autoLightLevel + " Changed Then");
			sb.AppendLine("   " + autoLightLevel + " = " + controller + "." + autoLightLevel);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + lightBacstatAllowed + " Changed Then");
			sb.AppendLine("   " + lightBacstatAllowed + " = " + controller + "." + lightBacstatAllowed);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + conditionerLevelSetpoint + " Changed Then");
			sb.AppendLine("   " + conditionerLevelSetpoint + " = " + controller + "." + conditionerLevelSetpoint);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + shutterLevelSetpoint + " Changed Then");
			sb.AppendLine("   " + shutterLevelSetpoint + " = " + controller + "." + shutterLevelSetpoint);
			sb.AppendLine("End If");
			sb.AppendLine("");
			sb.AppendLine("If " + controller + "." + conditionerBacstatAllowed + " Changed Then");
			sb.AppendLine("   " + conditionerBacstatAllowed + " = " + controller + "." + conditionerBacstatAllowed);
			sb.AppendLine("End If");
			return new[] {sb.ToString()};
		}

		public void CreateLCDPG(string cabinete, string dir)
		{
			var cabNumber = GenCabNumber(cabinete);
			dir = dir + "\\";

			var fileName = dir  + cabinete + "LCDControl.txt";
			var tmp = GenLCDPg(cabNumber);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + cabinete + "LCDTemperature.txt";
			tmp = GenLCDTemperaturePg(cabNumber);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + cabinete + "LCDVentilation.txt";
			tmp = GenLCDVentilationPg(cabNumber);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + cabinete + "LCDLight.txt";
			tmp = GenLCDLightPg(cabNumber);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + cabinete + "LCDCondition.txt";
			tmp = GenLCDConditionPg(cabNumber);
			File.WriteAllLines(fileName, tmp);

			fileName = dir + cabinete + "LCDConditionerShutter.txt";
			tmp = GenLCDConditionerShutterPg(cabNumber);
			File.WriteAllLines(fileName, tmp);
		}

		private string[] GenLCDPg(string cabNumber)
		{
			var lcdCurrentPage = _vavAddresses["LCDCurrentPage"] + cabNumber;
			var lcdTemperaturePG = "PG3" + cabNumber;
			var lcdVentilationPG = "PG4" + cabNumber;
			var lcdLightPG = "PG5" + cabNumber;
			var lcdConditionPG = "PG6" + cabNumber;
			var lcdConditionerShutterPG = "PG7" + cabNumber;

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
			sb.AppendLine("IfOnce LinkLCD1.KeyPress = 2 Then");
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

		private string[] GenLCDTemperaturePg(string cabNumber)
		{
			var temperatureSetpoint = _controllerAddresses["TemperatureSetpoint"] + cabNumber;
			var currentTemperature = _controllerAddresses["CurrentTemperature"] + cabNumber;
			var temperatureBacstatAllowed = _controllerAddresses["TemperatureBacstatAllowed"] + cabNumber;
			var temperatureSetpointMin = _vavAddresses["TemperatureSetpointMin"] + cabNumber;
			var temperatureSetpointMax = _vavAddresses["TemperatureSetpointMax"] + cabNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable temperatureSetpoint As Real");
			sb.AppendLine("");
			sb.AppendLine("LinkLCD1.Line2 = " + "\" " + "\"");
			sb.AppendLine("LinkLCD1.Cooling = 0");
			sb.AppendLine("LinkLCD1.Heating = 0");
			sb.AppendLine("temperatureSetpoint = " + temperatureSetpoint);
			sb.AppendLine("If " + temperatureBacstatAllowed + " On Then");
			sb.AppendLine("	IfOnce LinkLCD1.KeyPress = 3 Then");
			sb.AppendLine("		temperatureSetpoint = temperatureSetpoint - 0.5");
			sb.AppendLine("	End If");
			sb.AppendLine("	IfOnce LinkLCD1.KeyPress = 4 Then");
			sb.AppendLine("		temperatureSetpoint = temperatureSetpoint + 0.5");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine("temperatureSetpoint = Limit (temperatureSetpoint, " + temperatureSetpointMin + ", " + temperatureSetpointMax + ")");
			sb.AppendLine(temperatureSetpoint + " = temperatureSetpoint");
			sb.AppendLine("LinkLCD1.Line2 = " + currentTemperature);
			sb.AppendLine("LinkLCD1.Line3 = temperatureSetpoint & \"^C\"");
			sb.AppendLine("LinkLCD1.Fan = 0");
			sb.AppendLine("LinkLCD1.Sun = 0");
			sb.AppendLine("LinkLCD1.Line2Units = 1");
			sb.AppendLine("LinkLCD1.Line3Units = 0");
			return new[] { sb.ToString() };
		}

		private string[] GenLCDVentilationPg(string cabNumber)
		{
			var ventilationSetpoint = _controllerAddresses["VentilationSetpoint"] + cabNumber;
			var ventilationBacstatAllowed = _controllerAddresses["VentilationBacstatAllowed"] + cabNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable ventilationSetpoint As Integer");
			sb.AppendLine("");
			sb.AppendLine("LinkLCD1.Line2 = " + "\"BEH\"");
			sb.AppendLine("LinkLCD1.Cooling = 0");
			sb.AppendLine("LinkLCD1.Heating = 0");
			sb.AppendLine("LinkLCD1.Line2Units = 0");
			sb.AppendLine("LinkLCD1.Line3Units = 0");
			sb.AppendLine("ventilationSetpoint = " + ventilationSetpoint);
			sb.AppendLine("If " + ventilationBacstatAllowed + " On Then");
			sb.AppendLine("	IfOnce LinkLCD1.KeyPress = 3 Then");
			sb.AppendLine("		ventilationSetpoint = ventilationSetpoint - 1");
			sb.AppendLine("	End If");
			sb.AppendLine("	IfOnce LinkLCD1.KeyPress = 4 Then");
			sb.AppendLine("		ventilationSetpoint = ventilationSetpoint + 1");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine("ventilationSetpoint = Limit (ventilationSetpoint, - 3, 3)");
			sb.AppendLine(ventilationSetpoint + " = ventilationSetpoint");
			sb.AppendLine("If ventilationSetpoint = 0 Then");
			sb.AppendLine("	LinkLCD1.Line3 = \"auto\"");
			sb.AppendLine("Else");
			sb.AppendLine("	LinkLCD1.Line3 = ventilationSetpoint");
			sb.AppendLine("End If");
			return new[] { sb.ToString() };
		}

		private string[] GenLCDLightPg(string cabNumber)
		{
			var lightLevelLCDSetpont = _vavAddresses["LightLevelLCDSetpont"] + cabNumber;
			var lightLevel = _controllerAddresses["LightLevelSetpoint"] + cabNumber;
			var autoLightLevelSetpont = _controllerAddresses["AutoLightLevel"] + cabNumber;
			var lightBacstatAllowed = _controllerAddresses["LightBacstatAllowed"] + cabNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable lightLevelLCDSetpoint As Integer");
			sb.AppendLine("Variable lightLevelSetpoint As Integer");
			sb.AppendLine("Variable autoLightLevelSetpoint As Integer");
			sb.AppendLine("");
			sb.AppendLine("	LinkLCD1.Line2 = \"CBE\"");
			sb.AppendLine("	LinkLCD1.Cooling = 0");
			sb.AppendLine("	LinkLCD1.Heating = 0");
			sb.AppendLine("	LinkLCD1.Line2Units = 0");
			sb.AppendLine("	LinkLCD1.Line3Units = 0");
			sb.AppendLine("	lightLevelLCDSetpoint = " + lightLevelLCDSetpont);
			sb.AppendLine("	lightLevelSetpoint = " + lightLevel);
			sb.AppendLine("	AutoLightLevelSetpoint = " + autoLightLevelSetpont);
			sb.AppendLine("	If " + lightBacstatAllowed + " On Then");
			sb.AppendLine("		IfOnce LinkLCD1.KeyPress = 3 Then");
			sb.AppendLine("			lightLevelLCDSetpoint = lightLevelLCDSetpoint - 1");
			sb.AppendLine("		End If");
			sb.AppendLine("		IfOnce LinkLCD1.KeyPress = 4 Then");
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
			sb.AppendLine("		LinkLCD1.Line3 = \"auto\"");
			sb.AppendLine("	Else");
			sb.AppendLine("		LinkLCD1.Line3 = lightLevelLCDSetpoint");
			sb.AppendLine("	End If");
			sb.AppendLine(" LinkLCD1.Line2Units = 0");
			return new[] { sb.ToString() };
		}

		private string[] GenLCDConditionPg(string cabNumber)
		{
			var conditionerLevelSetpoint = _controllerAddresses["ConditionerLevelSetpoint"] + cabNumber;
			var conditionerBacstatAllowed = _controllerAddresses["ConditionerBacstatAllowed"] + cabNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable conditionerSetpoint As Integer");
			sb.AppendLine("");
			sb.AppendLine("LinkLCD1.Line2 = \"KOH\"");
			sb.AppendLine("LinkLCD1.Line2Units = 0");
			sb.AppendLine("LinkLCD1.Line3Units = 0");
			sb.AppendLine("conditionerSetpoint = " + conditionerLevelSetpoint);
			sb.AppendLine("If " + conditionerBacstatAllowed + " Then");
			sb.AppendLine("	IfOnce LinkLCD1.KeyPress = 3 Then");
			sb.AppendLine("		conditionerSetpoint = Limit (conditionerSetpoint - 1, 0, 4)");
			sb.AppendLine("	End If");
			sb.AppendLine("	IfOnce LinkLCD1.KeyPress = 4 Then");
			sb.AppendLine("		conditionerSetpoint = Limit (conditionerSetpoint + 1, 0, 4)");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine(conditionerLevelSetpoint + " = conditionerSetpoint");
			sb.AppendLine("If conditionerSetpoint = 0 Then");
			sb.AppendLine("	LinkLCD1.Line3 = Off");
			sb.AppendLine("Else");
			sb.AppendLine("	LinkLCD1.Line3 = conditionerSetpoint");
			sb.AppendLine("End If");
			sb.AppendLine("LinkLCD1.Fan = conditionerSetpoint");
			return new[] {sb.ToString()};
		}

		private string[] GenLCDConditionerShutterPg(string cabNumber)
		{
			var shutterLevelSetpoint = _controllerAddresses["ShutterLevelSetpoint"] + cabNumber;
			var conditionerBacstatAllowed = _controllerAddresses["ConditionerBacstatAllowed"] + cabNumber;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Variable shutterLevel As Integer");
			sb.AppendLine("");
			sb.AppendLine("LinkLCD1.Line2 = \"KO2\"");
			sb.AppendLine("LinkLCD1.Line2Units = 0");
			sb.AppendLine("LinkLCD1.Line3Units = 4");
			sb.AppendLine("LinkLCD1.Cooling = 0");
			sb.AppendLine("LinkLCD1.Heating = 0");
			sb.AppendLine("LinkLCD1.Fan = 0");
			sb.AppendLine("shutterLevel = " + shutterLevelSetpoint);
			sb.AppendLine("If " + conditionerBacstatAllowed + " Then");
			sb.AppendLine("	IfOnce LinkLCD1.KeyPress = 3 Then");
			sb.AppendLine("		shutterLevel = Limit (shutterLevel - 1, 1, 5)");
			sb.AppendLine("	End If");
			sb.AppendLine("	IfOnce LinkLCD1.KeyPress = 4 Then");
			sb.AppendLine("		shutterLevel = Limit (shutterLevel + 1, 1, 5)");
			sb.AppendLine("	End If");
			sb.AppendLine("End If");
			sb.AppendLine(shutterLevelSetpoint + " = shutterLevel");
			sb.AppendLine("If shutterLevel = 1 Then");
			sb.AppendLine("	LinkLCD1.Line3Units = 0");
			sb.AppendLine("	LinkLCD1.Line3 = \"Hor\"");
			sb.AppendLine("End If");
			sb.AppendLine("If shutterLevel = 2 Then");
			sb.AppendLine("	LinkLCD1.Line3 = 60");
			sb.AppendLine("End If");
			sb.AppendLine("If shutterLevel = 3 Then");
			sb.AppendLine("	LinkLCD1.Line3 = 80");
			sb.AppendLine("End If");
			sb.AppendLine("If shutterLevel = 4 Then");
			sb.AppendLine("	LinkLCD1.Line3 = 100");
			sb.AppendLine("End If");
			sb.AppendLine("If shutterLevel = 5 Then");
			sb.AppendLine("	LinkLCD1.Line3Units = 0");
			sb.AppendLine("	LinkLCD1.Line3 = \"Sw\"");
			sb.AppendLine("End If");
			return new[] {sb.ToString()};
		}
	}
}
