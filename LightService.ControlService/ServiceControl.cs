using System;
using System.Collections.Generic;
using System.ServiceModel;
using LightService.Common;

namespace LightService.ControlService
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
	public class ServiceControl : IAstoriaLightServiceControl
	{
		private readonly LightControl _control;


		public ServiceControl(LightControl control)
		{
			_control = control;
		}

		public void UpdateControlledObjects(List<LightZone> lightZone)
		{
			_control.Resubscribe(lightZone);
		}

		public List<LightZone> GetControlledObjects()
		{
			if (_control != null)
				return _control.Subscriptions();

			return new List<LightZone>();
		}
	}
}
