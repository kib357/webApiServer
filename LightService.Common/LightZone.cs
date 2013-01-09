using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;

namespace LightService.Common
{
	public class LightZone
	{
		[XmlAttribute]
		public string InputAddress { get; set; }
		[XmlAttribute]
		public string InputValue { get; set; }
		[XmlArray]
		public ObservableCollection<string> OutputAddresses { get; set; }
		[XmlArray]
		public ObservableCollection<string> OutputAlarmAddresses { get; set; }
		[XmlAttribute]
		public string SetPointAddress { get; set; }
		[XmlAttribute]
		public string SetPointValue { get; set; }
		//		public string LastSettedValue { get; set; }

		public LightZone()
		{
			OutputAddresses = new ObservableCollection<string>();
			OutputAlarmAddresses = new ObservableCollection<string>();
		}

		public override bool Equals(object obj)
		{
			if (obj is LightZone)
			{
				var p = (LightZone)obj;
				return
					InputAddress == p.InputAddress &&
					InputValue == p.InputValue &&
					SetPointAddress == p.SetPointAddress &&
					SetPointValue == p.SetPointValue &&
					OutputAddresses.Count == p.OutputAddresses.Count && OutputAddresses.Except(p.OutputAddresses).Any() &&
					OutputAlarmAddresses.Count == p.OutputAlarmAddresses.Count && OutputAlarmAddresses.Except(p.OutputAlarmAddresses).Any();
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return (
				(InputAddress ?? "") +
				(InputValue ?? "") +
				(OutputAddresses.Any() ? OutputAddresses.Aggregate((a, b) => a + (b ?? "")) : "") +
				(OutputAlarmAddresses.Any() ? OutputAlarmAddresses.Aggregate((a, b) => a + (b ?? "")) : "") +
				(SetPointAddress ?? "") +
				(SetPointValue ?? "")

			).GetHashCode();
		}
	}
}
