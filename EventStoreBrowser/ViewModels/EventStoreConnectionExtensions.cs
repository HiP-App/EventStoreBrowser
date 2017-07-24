using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventStoreBrowser.ViewModels
{
    public static class EventStoreConnectionExtensions
    {
        public static async Task<IReadOnlyList<ResolvedEvent>> ReadEventsAsync(this IEventStoreConnection connection, string streamName, long maxEventNumber = long.MaxValue)
        {
            const int pageSize = 4096; // only 4096 events can be retrieved in one call

            // read all events (from the beginning to the end)
            var start = 0;
            StreamEventsSlice readResult;

            var events = new List<ResolvedEvent>();

            do
            {
                readResult = await connection.ReadStreamEventsForwardAsync(streamName, start, pageSize, false);
                var evs = readResult.Events.Where(e => e.Event.EventNumber <= maxEventNumber);
                events.AddRange(evs);
                start += pageSize;
            }
            while (!readResult.IsEndOfStream);

            return events;
        }

        public static async Task<IReadOnlyList<ResolvedEvent>> ReadEventsFromAbsoluteStartAsync(this IEventStoreConnection connection, string streamName)
        {
            // Temporarily set $tb to 0
            var meta = await connection.GetStreamMetadataAsync(streamName);
            var tempMeta = meta.StreamMetadata.Copy().SetTruncateBefore(0).Build();
            await connection.SetStreamMetadataAsync(streamName, ExpectedVersion.Any, tempMeta);

            // Read all events
            var events = await connection.ReadEventsAsync(streamName);

            // Reset $tb
            await connection.SetStreamMetadataAsync(streamName, ExpectedVersion.Any, meta.StreamMetadata);

            return events;
        }

        public static async Task<string> GetStreamMetadataJsonAsync(this IEventStoreConnection connection, string streamName)
        {
            var meta = await connection.GetStreamMetadataAsync(streamName);
            return meta.StreamMetadata.AsJsonString();
        }

        public static async Task SetStreamMetadataJsonAsync(this IEventStoreConnection connection, string streamName, string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await connection.SetStreamMetadataAsync(streamName, ExpectedVersion.Any, bytes);
        }

        public static IEnumerable<EventData> ToEventData(this IEnumerable<ResolvedEvent> resolvedEvents)
        {
            return resolvedEvents.Select(e => new EventData(
                Guid.NewGuid(), e.Event.EventType, e.Event.IsJson,
                e.Event.Data, e.Event.Metadata));
        }
    }
}
