using System;
using System.Collections.Generic;
using System.Linq;

namespace BacNetApi.Schedules
{
	public class ScheduleIndexer
	{
		private readonly List<Schedule> _scheduleList = new List<Schedule>();
		private readonly BacNetDevice _device;

		public ScheduleIndexer(BacNetDevice device)
        {
            _device = device;
        }

		public List<uint> Get()
		{
			return _device.ObjectList.Where(o => o.Contains("SCH")).Select(s => Convert.ToUInt32(s.Replace("SCH", ""))).ToList();
		}

		public Schedule this[uint i]
		{
			get
			{
				int index = _scheduleList.FindIndex(d => d.Id == "SCH" + i);
				if (index < 0)
				{
					var schedule = new Schedule(_device, i);
					_scheduleList.Add(schedule);
					index = _scheduleList.FindIndex(d => d.Id == "SCH" + i);
				}
				return _scheduleList[index];
			}
			set
			{
				int index = _scheduleList.FindIndex(d => d.Id == "SCH" + i);
				if (index < 0)
					_scheduleList.Add(value);
				else
					_scheduleList[index] = value;
			}
		}
	}
}
