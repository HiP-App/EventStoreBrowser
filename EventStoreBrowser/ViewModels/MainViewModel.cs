using EventStore.ClientAPI;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EventStoreBrowser.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private static readonly ConnectionSettings DefaultSettings =
            ConnectionSettings.Create().EnableVerboseLogging().Build();

        private string _connectionUri = "tcp://localhost:1113";
        private string _streamName = "develop";
        private string _streamMetadata = "";

        private List<EventViewModel> _events = new List<EventViewModel>
        {
            new EventViewModel(Guid.Empty, "DummyCreated", DateTime.Now, 1, "{\r\n    \"foo\": \"bar\"\r\n}", "{\r\n    \"foo\": \"meta\"\r\n}"),
            new EventViewModel(Guid.Empty, "DummyDeleted", DateTime.Now, 2, "{\r\n    \"foo\": \"bar\"\r\n}", "{\r\n    \"foo\": \"meta\"\r\n}")
        };

        public string ConnectionUri
        {
            get => _connectionUri;
            set => Set(ref _connectionUri, value);
        }

        public string StreamName
        {
            get => _streamName;
            set => Set(ref _streamName, value);
        }

        public string StreamMetadata
        {
            get => _streamMetadata;
            private set => Set(ref _streamMetadata, value);
        }

        public List<EventViewModel> Events
        {
            get => _events;
            private set => Set(ref _events, value);
        }

        public async Task ConnectAndReadAsync()
        {
            try
            {
                var connection = EventStoreConnection.Create(DefaultSettings, new Uri(ConnectionUri));
                await connection.ConnectAsync();

                var meta = await connection.GetStreamMetadataAsync(_streamName);
                StreamMetadata = meta.StreamMetadata.AsJsonString();

                const int pageSize = 4096; // only 4096 events can be retrieved in one call

                // read all events (from the beginning to the end)
                var events = new List<EventViewModel>();
                var start = 0;
                StreamEventsSlice readResult;

                do
                {
                    readResult = connection.ReadStreamEventsForwardAsync(_streamName, start, pageSize, false).Result;

                    foreach (var eventData in readResult.Events)
                        events.Insert(0, new EventViewModel(eventData.Event));

                    start += pageSize;
                }
                while (!readResult.IsEndOfStream);

                Events = events;
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "An error occurred while connecting to the Event Store or reading from it:\r\n" + e,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void CopyToClipboard()
        {
            var s = string.Join("\r\n", Events.Select(ev =>
                $"{ev.Number.ToString().PadLeft(6, '0')}  {ev.CreationDate.ToString("dd.MM.yyyy HH:mm:ss")}  {ev.Type}\r\n" +
                $"{ev.Data}\r\n" +
                "".PadLeft(80, '-')));

            Clipboard.SetText(s);
        }

        public async Task WriteTo(string targetConnectionUri, string targetStream)
        {
            try
            {
                var targetConnection = EventStoreConnection.Create(DefaultSettings, new Uri(targetConnectionUri));
                await targetConnection.ConnectAsync();

                var events = Events.Reverse<EventViewModel>().Select(ev => ev.ToEventData());
                await targetConnection.DeleteStreamAsync(targetStream, ExpectedVersion.Any);
                await targetConnection.AppendToStreamAsync(targetStream, ExpectedVersion.Any, events);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    "An error occurred while copying the events to the target stream:\r\n" + e,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task UndoLastSoftDeleteAsync()
        {
            var connection = EventStoreConnection.Create(DefaultSettings, new Uri(_connectionUri));
            await connection.ConnectAsync();

            // 1) Note current truncation number (point of last soft-delete), and reset it to 0
            var meta = await connection.GetStreamMetadataAsync(_streamName);

            var positionOfLastSoftDelete = meta.StreamMetadata.TruncateBefore;

            var warningDialog = MessageBox.Show(
                $"You are about to undo the last soft-deletion of the stream. Currently the stream starts at event #{positionOfLastSoftDelete}. This operation will soft-delete all events from #{positionOfLastSoftDelete} upwards, and then restore the events #0-#{positionOfLastSoftDelete - 1}. Are you sure you want to continue?\r\n\r\n" +
                "Warning: If the stream has already been soft-deleted multiple times, e.g. at #100, #200 and #300, all events from #0 to #299 will be restored, which might not be your intention!",
                "Undo last soft-delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (warningDialog != MessageBoxResult.Yes)
                return;

            var newMeta = meta.StreamMetadata.Copy()
                .SetTruncateBefore(0)
                .Build();

            await connection.SetStreamMetadataAsync(_streamName, meta.MetastreamVersion, newMeta);


            // 2) Read events from 0 to maxEvent

            const int pageSize = 4096; // only 4096 events can be retrieved in one call

            // read all events (from the beginning to the end)
            var start = 0;
            StreamEventsSlice readResult;

            var newEvents = new List<EventData>();

            do
            {
                readResult = connection.ReadStreamEventsForwardAsync(_streamName, start, pageSize, false).Result;

                var evs = readResult.Events
                    .Where(e => e.Event.EventNumber < positionOfLastSoftDelete)
                    .Select(e => new EventData(
                        Guid.NewGuid(), e.Event.EventType, e.Event.IsJson,
                        e.Event.Data, e.Event.Metadata));

                newEvents.AddRange(evs);


                start += pageSize;
            }
            while (!readResult.IsEndOfStream);

            // 3) Soft-delete stream
            await connection.DeleteStreamAsync(_streamName, ExpectedVersion.Any);

            // 4) Re-create stream with old events
            await connection.AppendToStreamAsync(_streamName, ExpectedVersion.Any, newEvents);
        }

        public async Task SetBeginningOfStreamAsync()
        {
            var connection = EventStoreConnection.Create(DefaultSettings, new Uri(_connectionUri));
            await connection.ConnectAsync();

            var meta = await connection.GetStreamMetadataAsync(_streamName);
            var positionOfLastSoftDelete = meta.StreamMetadata.TruncateBefore ?? 0;

            var lastEvent = await connection.ReadStreamEventsBackwardAsync(_streamName, StreamPosition.End, 1, false);

            var dialog = new EventNumberInputDialog
            {
                CurrentBeginningOfStream = (int)positionOfLastSoftDelete,
                SelectedBeginningOfStream = (int)positionOfLastSoftDelete,
                EndOfStream = (int)lastEvent.LastEventNumber
            };

            dialog.ShowDialog();

            if (dialog.DialogResult != true)
                return;

            // Set new truncation number
            var newMeta = meta.StreamMetadata.Copy()
                .SetTruncateBefore(dialog.SelectedBeginningOfStream)
                .Build();

            await connection.SetStreamMetadataAsync(_streamName, ExpectedVersion.Any, newMeta);
        }
    }
}
