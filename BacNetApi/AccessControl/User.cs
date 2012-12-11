using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BACsharp;
using BACsharp.Tools;
using BACsharp.Types;
using BACsharp.Types.Primitive;

namespace BacNetApi.AccessControl
{
    public enum CardStatuses
    {
        Valid = 0,
        Disabled = 1,
        Lost = 2
    }

    public class Card
    {
        public uint SiteCode { get; set; }
        public uint Number { get; set; }
        public CardStatuses Status { get; set; }
    }

    public class User : BacNetObject
    {
        private List<Card> _cards; 
        public List<Card> Cards
        {
            get
            {
                var data = (_device.ReadProperty(this, BacnetPropertyId.CardList) as List<BACnetDataType>).Cast<BACnetUknownData>().ToList();
                if (data == null || data.Count % 3 != 0) return null;
                var cards = new List<Card>();
                for (int i = 0; i < data.Count/3; i++)
                {
                    var card = new Card();
                    card.SiteCode = BytesConverter.DecodeUnsigned(data[0 + i * 3].Value, 0, data[0 + i * 3].Value.Length);
                    card.Number = BytesConverter.DecodeUnsigned(data[1 + i * 3].Value, 0, data[1 + i * 3].Value.Length);
                    card.Status = (CardStatuses)BytesConverter.DecodeUnsigned(data[2 + i * 3].Value, 0, data[2 + i * 3].Value.Length);
                    cards.Add(card);
                }
                _cards = cards;
                return _cards;
            }
            set
            {
                
            }
        }

        private List<uint> _accessGroups;
        public List<uint> AccessGroups
        {
            get
            {
                var data = _device.ReadProperty(this, BacnetPropertyId.AccessGroups);
                var groups = data.Cast<BACnetObjectId>().Select(o => (uint)o.Instance).ToList();
                _accessGroups = groups;
                return _accessGroups;
            }
            set
            {

            }
        }

        public User(BacNetDevice device, uint id)
        {
            _device = device;
            Id = "CU" + id;
            _synchronizationContext = SynchronizationContext.Current;
        }

        public void WriteCard()
        {
            var c = new Card();
            c.SiteCode = 12;
            c.Number = 242242;
            c.Status = 0;
            _device.WriteProperty(this , BacnetPropertyId.CardList, new List<Card> {c});
        }
    }
}
