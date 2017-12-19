using EventStore.ClientAPI;
using Newtonsoft.Json;
using System;
using System.Text;

namespace EventStoreBrowser.ViewModels
{
    public class EventViewModel
    {
        private readonly byte[] _data;
        private readonly byte[] _metadata;

        public Guid Id { get; }

        public string Type { get; }

        public DateTime CreationDate { get; }

        public long Number { get; }

        public string Data { get; }

        public string Metadata { get; }

        public EventViewModel(Guid id, string type, DateTime creationDate, long number, string data, string metadata)
        {
            Id = id;
            Type = type;
            CreationDate = creationDate;
            Number = number;
            Data = data;
            Metadata = metadata;
        }

        public EventViewModel(RecordedEvent ev)
        {
            Id = ev.EventId;
            Type = ev.EventType;
            CreationDate = ev.Created;
            Number = ev.EventNumber;
            _data = ev.Data;
            _metadata = ev.Metadata;

            Metadata = Encoding.UTF8.GetString(ev.Metadata);

            dynamic parsedJson = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(ev.Data));
            Data = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);

            parsedJson = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(ev.Metadata));
            Metadata = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);

        }

        public EventData ToEventData()
        {
            return new EventData(Guid.NewGuid(), Type, true, _data, _metadata);
        }
    }
}
