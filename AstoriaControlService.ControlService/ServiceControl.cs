using System.Collections.Generic;
using System.ServiceModel;
using AstoriaControlService.Common.AirCondition;
using AstoriaControlService.Common.Light;

namespace AstoriaControlService.ControlService
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
	public class ServiceControl : IAstoriaControlServiceControl
	{
		private readonly LightControl _lightControl;
		private readonly ACControl _acControl;

		public ServiceControl()
		{}

		public ServiceControl(LightControl lightControl, ACControl acControl)
		{
			_lightControl = lightControl;
			_acControl = acControl;
		}
		
		public void UpdateAllControlledObjects(List<LightZone> lightZones, List<ACZone> acZones)
		{
			_lightControl.Resubscribe(lightZones);
			_acControl.Resubscribe(acZones);
		}

		public void UpdateLightControlledObjects(List<LightZone> lightZones)
		{
			_lightControl.Resubscribe(lightZones);
		}

		public void UpdateACControlledObjects(List<ACZone> acZones)
		{
			_acControl.Resubscribe(acZones);
		}

		public List<LightZone> GetLightControlledObjects()
		{
			if (_lightControl != null)
				return _lightControl.Subscriptions();

			return new List<LightZone>();
		}

		public List<ACZone> GetACControlledObjects()
		{
			return _acControl != null ? _acControl.Subscriptions() : new List<ACZone>();
		}
	}
}
