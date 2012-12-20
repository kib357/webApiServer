using System.Collections.Generic;
using System.Linq;

namespace LigtService.Common
{
	public class LightZone
	{
		public string InputAddress { get; set; }
		public string InputValue { get; set; }
		public List<string> OutputAddresses { get; set; }
		public List<string> OutputAlarmAddresses { get; set; }
		public string SetPointAddress { get; set; }
		public string SetPointValue { get; set; }
		//		public string LastSettedValue { get; set; }

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
			return (InputAddress + InputValue + OutputAddresses.Aggregate((a, b) => a + b) + OutputAlarmAddresses.Aggregate((a, b) => a + b) + SetPointAddress + SetPointValue).GetHashCode();
		}
	}
}
