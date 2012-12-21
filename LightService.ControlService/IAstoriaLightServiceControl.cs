using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using LightService.Common;

namespace LightService.ControlService
{
	[ServiceContract]
	public interface IAstoriaLightServiceControl
	{
		[OperationContract]
		void UpdateControlledObjects(List<LightZone> lightZone);

		[OperationContract]
		List<LightZone> GetControlledObjects();
	}
}
