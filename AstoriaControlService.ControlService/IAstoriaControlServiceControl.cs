using System.Collections.Generic;
using System.ServiceModel;
using AstoriaControlService.Common.AirCondition;
using AstoriaControlService.Common.Light;

namespace AstoriaControlService.ControlService
{
	[ServiceContract]
	public interface IAstoriaControlServiceControl
	{
		[OperationContract]
		void UpdateAllControlledObjects(List<LightZone> lightZones, List<ACZone> acZones);

		[OperationContract]
		void UpdateLightControlledObjects(List<LightZone> lightZones);

		[OperationContract]
		void UpdateACControlledObjects(List<ACZone> acZones);

		[OperationContract]
		List<LightZone> GetLightControlledObjects();

		[OperationContract]
		List<ACZone> GetACControlledObjects();
	}
}
