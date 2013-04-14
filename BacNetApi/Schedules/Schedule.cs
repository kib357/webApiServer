using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
		    _objectType = BacnetObjectType.Schedule;
            Id = "SCH" + id;
            SynchronizationContext = SynchronizationContext.Current;
        }

	    private string _name;
	    public override string Name
        {
	        get
	        {
	            if (_name == null) Update();
	            return _name;
	        }
	        set { _name = value; }
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
		        if (_weeklySchedule == null) Update();
		        return _weeklySchedule;
		    }
		    set { _weeklySchedule = value; }
		}

        private List<BACnetDeviceObjectPropertyReference> _listOfObjectPropertyReferences;
		public List<BACnetDeviceObjectPropertyReference> ListOfObjectPropertyReferences
		{
		    get
		    {
		        if (_listOfObjectPropertyReferences == null) Update();
		        return _listOfObjectPropertyReferences;
		    }
		    set { _listOfObjectPropertyReferences = value; }
		}

		public bool Submit()
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

        public void Update()
        {
            if (_name == null)
            {
                var name = _device.ReadProperty(this, BacnetPropertyId.ObjectName);
                if (name != null && name.Count > 0)
                    _name = name[0].ToString();
            }
            if (_weeklySchedule == null)
            {
                var weeklySchedule = _device.ReadProperty(this, BacnetPropertyId.WeeklySchedule);
                if (weeklySchedule != null)
                    _weeklySchedule = weeklySchedule.Cast<BACnetDailySchedule>().ToList();
            }
            if (_listOfObjectPropertyReferences == null)
            {
                var listOfObjectPropertyReferences = _device.ReadProperty(this, BacnetPropertyId.ListOfObjectPropertyReferences);
                if (listOfObjectPropertyReferences != null)
                    _listOfObjectPropertyReferences = listOfObjectPropertyReferences.Cast<BACnetDeviceObjectPropertyReference>().ToList();
            }
        }
	}
}
