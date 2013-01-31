using System.Xml.Serialization;

namespace AstoriaControlService.Common.AirCondition
{
	public class ACZone
	{
		[XmlAttribute]
		public string OnOffAddress { get; set; }
		[XmlAttribute]
		public string OnOffValue { get; set; }
		[XmlAttribute]
		public string OnOffOutputAddress { get; set; }

		[XmlAttribute]
		public string SetpointAddress { get; set; }
		[XmlAttribute]
		public string SetpointValue { get; set; }
		[XmlAttribute]
		public string SetpointOutputAddress { get; set; }

		[XmlAttribute]
		public string TemperatureSetpointAddress { get; set; }
		[XmlAttribute]
		public string TemperatureSetpointValue { get; set; }
		[XmlAttribute]
		public string TemperatureSetpointOutputAddress { get; set; }

		[XmlAttribute]
		public string ShutterSetpointAddress { get; set; }
		[XmlAttribute]
		public string ShutterSetpointValue { get; set; }
		[XmlAttribute]
		public string ShutterSetpointOutputAddress { get; set; }

		public override bool Equals(object obj)
		{
			if (obj is ACZone)
			{
				var p = (ACZone)obj;
				return
					SetpointAddress == p.SetpointAddress &&
					SetpointValue == p.SetpointValue &&
					TemperatureSetpointAddress == p.TemperatureSetpointAddress &&
					TemperatureSetpointValue == p.TemperatureSetpointValue &&
					ShutterSetpointAddress == p.ShutterSetpointAddress &&
					ShutterSetpointValue == p.ShutterSetpointValue;
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return (
				(SetpointAddress ?? "") +
				(SetpointValue ?? "") +
				(TemperatureSetpointAddress ?? "") +
				(TemperatureSetpointValue ?? "") +
				(ShutterSetpointAddress ?? "") +
				(ShutterSetpointValue ?? "")
			).GetHashCode();
		}
	}
}
