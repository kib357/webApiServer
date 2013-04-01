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
        public User(BacNetDevice device, uint id)
        {
            _device = device;
            Id = "CU" + id;
            SynchronizationContext = SynchronizationContext.Current;
        }

        private List<Card> _cards; 
        public List<Card> Cards
        {
            get
            {
                if (_cards == null) RefreshCards();
                return _cards;
            }
            set { _cards = value; }
        }

        public void RefreshCards()
        {
            var data = _device.ReadProperty(this, BacnetPropertyId.CardList).Cast<BACnetUknownData>().ToList();
            if (data.Count % 3 != 0) throw new Exception("Invalid card data on controller");
            var cards = new List<Card>();
            for (int i = 0; i < data.Count / 3; i++)
            {
                var card = new Card();
                card.SiteCode = BytesConverter.DecodeUnsigned(data[0 + i * 3].Value, 0, data[0 + i * 3].Value.Length);
                card.Number = BytesConverter.DecodeUnsigned(data[1 + i * 3].Value, 0, data[1 + i * 3].Value.Length);
                card.Status = (CardStatuses)BytesConverter.DecodeUnsigned(data[2 + i * 3].Value, 0, data[2 + i * 3].Value.Length);
                cards.Add(card);
            }
            _cards = cards;
        }

        public void SubmitCards()
        {
            if (_cards != null)
            {
                WriteUsingWpm(BacnetPropertyId.CardList, _cards);
            }
            else
                throw new Exception("Cannot submit - card list is null");
        }

        private List<uint> _accessGroups;
        public List<uint> AccessGroups
        {
            get
            {
                if (_accessGroups == null) RefreshAccessGroups();
                return _accessGroups;
            }
            set { _accessGroups = value; }
        }

        public void RefreshAccessGroups()
        {
            var data = _device.ReadProperty(this, BacnetPropertyId.AccessGroups);
            var groups = data.Cast<BACnetObjectId>().Select(o => (uint)o.Instance).ToList();
            _accessGroups = groups;
        }

        public void SubmitAccessGroups()
        {
            if (_accessGroups != null)
            {                
                WriteUsingWpm(BacnetPropertyId.AccessGroups, _accessGroups);
            }
            else
                throw new Exception("Cannot submit - access group list is null");
        }
    }
}
