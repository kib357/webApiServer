using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using LigtService.Common;

namespace LightService.ControlService
{
	[ServiceContract]
	public interface IAstoriaLightServiceControl
	{
		[OperationContract]
		void UpdateControlledObjects(LightZones lightZone);

		[OperationContract]
		List<LightZone> GetControlledObjects();
	}
}
