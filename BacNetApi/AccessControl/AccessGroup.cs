using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BACsharp;
using BACsharp.Tools;
using BACsharp.Types;
using BACsharp.Types.Primitive;

namespace BacNetApi.AccessControl
{
    public class AccessArea
    {
        public uint InstanceNumber { get; set; }
        public BacnetObjectType Type { get; set; }
        public uint ScheduleId { get; set; }
    }

    public class AccessGroup : BacNetObject
    {
        public AccessGroup(BacNetDevice device, uint id)
        {
            _device = device;
            Id = "AG" + id;
            _synchronizationContext = SynchronizationContext.Current;
        }

        private List<AccessArea> _areas;
        public List<AccessArea> Areas
        {
            get
            {
                if (_areas == null) Refresh(out _areas, BacnetPropertyId.AccessAreas);
                return _areas;
            }
            set { _areas = value; }
        }

        public void SubmitAreas()
        {
            if (_areas != null)
            {
                WriteUsingWpm(BacnetPropertyId.AccessAreas, _areas);
            }
            else
                throw new Exception("Cannot submit - area list is null");
        }

        private List<AccessArea> _exceptions;
        public List<AccessArea> Exceptions
        {
            get
            {
                if (_exceptions == null) Refresh(out _exceptions, BacnetPropertyId.AccessAreasExceptions);
                return _exceptions;
            }
            set { _exceptions = value; }
        }

        public void SubmitExceptions()
        {
            if (_areas != null)
            {
                WriteUsingWpm(BacnetPropertyId.AccessAreasExceptions, _exceptions);
            }
            else
                throw new Exception("Cannot submit - exception list is null");
        }

        private void Refresh(out List<AccessArea> refreshList, BacnetPropertyId propertyId)
        {
            try
            {
                var data = _device.ReadProperty(this, propertyId);
                var areas = new List<AccessArea>();
                for (int i = 0; i < data.Count; i += 2)
                {
                    var area = new AccessArea();
                    var item = data[i] as Sequence;
                    var obj = item.Values[0] as BACnetUknownData;
                    uint value = BytesConverter.DecodeUnsigned(obj.Value, 0, 4);
                    area.Type = (BacnetObjectType)(value >> 22);
                    area.InstanceNumber = value << 10 >> 10;
                    obj = (data[i + 1] as Sequence).Values[0] as BACnetUknownData;
                    uint schedule = BytesConverter.DecodeUnsigned(obj.Value, 0, 4);
                    if (schedule != 0xFFFFFFFF && schedule >> 22 == 17)
                        area.ScheduleId = schedule << 10 >> 10;
                    areas.Add(area);                    
                }
                refreshList = areas;
            }
            catch
            {
                refreshList = null;
            }
        }        
    }
}
