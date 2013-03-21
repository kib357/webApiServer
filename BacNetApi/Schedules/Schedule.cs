using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BACsharp;
using BACsharp.Types;
using BACsharp.Types.Constructed;

namespace BacNetApi.Schedules
{
	public class Schedule : BacNetObject
	{
		public Schedule(BacNetDevice device, uint id)
        {
            _device = device;
            Id = "SCH" + id;
            SynchronizationContext = SynchronizationContext.Current;
        }

		private BacnetObjectType _objectType;
		public BacnetObjectType ObjectType
		{
			get
			{
				_objectType = _device.ReadProperty(this, BacnetPropertyId.ObjectType).Cast<BacnetObjectType>().ToList()[0];
				return _objectType;
			}
			set { _objectType = value; }
		}

		private string _presentValue;
		public string PresentValue
		{
			get
			{
				_presentValue = _device.ReadProperty(this, BacnetPropertyId.PresentValue)[0].ToString();
				return _presentValue;
			}
			set { _presentValue = value; }
		}

		private List<BACnetDailySchedule> _weeklySchedule;
		public List<BACnetDailySchedule> WeeklySchedule
		{
			get
			{
				_weeklySchedule = _device.ReadProperty(this, BacnetPropertyId.WeeklySchedule).Cast<BACnetDailySchedule>().ToList();
				return _weeklySchedule;
			}
			set { _weeklySchedule = value; }
		}

		private List<BACnetDataType> _listOfObjectPropertyReferences;
		public List<BACnetDataType> ListOfObjectPropertyReferences
		{
			get
			{
				_listOfObjectPropertyReferences = _device.ReadProperty(this, BacnetPropertyId.ListOfObjectPropertyReferences);
				return _listOfObjectPropertyReferences;
			}
			set { _listOfObjectPropertyReferences = value; }
		}

		public bool SubmitSchedule()
		{
			var values = new Dictionary<string, Dictionary<BacnetPropertyId, object>>();
			var val = new Dictionary<BacnetPropertyId, object>
				          {
					          {BacnetPropertyId.WeeklySchedule, _weeklySchedule},
					          {BacnetPropertyId.ObjectName, Name},
					          {BacnetPropertyId.ListOfObjectPropertyReferences, _listOfObjectPropertyReferences}
				          };
			values.Add(Id, val);
			return _device.WritePropertyMultiple(values);
		}
	}
}
