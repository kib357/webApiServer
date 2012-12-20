using System;
using System.Collections.Generic;
using System.ServiceModel;
using LigtService.Common;

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

		public void UpdateControlledObjects(LightZones lightZone)
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
