using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types;
using BACsharp.Types.Constructed;
using BACsharp.Types.Primitive;
using BacNetApi;

namespace WPFBacNetApiSample
{
	class ObjectAddresses
	{

		private readonly Dictionary<string, string> _controllerAddresses;
		private readonly Dictionary<string, string> _vavAddresses; 


		public Dictionary<string, uint?> Cabinetes { get; set; }
		private readonly uint _controller;
		public uint Controller { get { return _controller; } }

		public ObjectAddresses(uint controller)
		{
			_controllerAddresses = InitializeControllerAddresses();
			_vavAddresses = InitializeVAVAddresses();
			_controller = controller;
			
		}

		public ObjectAddresses(uint controller, Dictionary<string, uint?> cabinetes)
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

		public void WriteAllObjectsToController()
		{
			foreach (var cabinete in Cabinetes)
			{
				WriteCabineteObjects(cabinete);
			}
		}

		private void WriteCabineteObjects(KeyValuePair<string, uint?> cabinete)
		{
			foreach (var controllerAddress in _controllerAddresses)
			{
				WriteObject(_controller, controllerAddress.Key, cabinete.Key, controllerAddress.Value);
			}
			if(cabinete.Value == null) return;
			foreach (var controllerAddress in _controllerAddresses)
			{
				WriteObject((uint)cabinete.Value, controllerAddress.Key, cabinete.Key, controllerAddress.Value);
			}
			foreach (var vavAddress in _vavAddresses)
			{
				WriteObject((uint)cabinete.Value, vavAddress.Key, cabinete.Key, vavAddress.Value);
			}
		}

		private void WriteObject(uint controller, string objectDescription, string cabinete, string writedObject)
		{
			var objName = new List<BACnetPropertyValue>
				              {
					              new BACnetPropertyValue((int) BacnetPropertyId.ObjectName,
					                                      new List<BACnetDataType>
						                                      {new BACnetCharacterString(cabinete + objectDescription)})
				              };
			int start = cabinete.IndexOf('(');
			int end = cabinete.IndexOf(')');
			char[] tmp = new char[0];
			Array.Resize(ref tmp, end - start - 1);
			cabinete.CopyTo(start + 1, tmp, 0, end - start - 1);
			string tmpStr = string.Empty;
			foreach (var chr in tmp)
			{
				if (chr == 'A' || chr == 'a')
					tmpStr = tmpStr + "1";
				if (chr == 'B' || chr == 'b')
					tmpStr = tmpStr + "2";
				tmpStr = tmpStr + chr;
			}
			string createdObject = writedObject + tmpStr;
			MyViewModel.Bacnet[controller].Objects[createdObject].Create(objName);
			if (objectDescription.Contains("Temperature") && writedObject.Contains("AV"))
				MyViewModel.Bacnet[controller].Objects[createdObject].BeginSet(0.5, BacnetPropertyId.COVIncrement);
		}
	}
}
