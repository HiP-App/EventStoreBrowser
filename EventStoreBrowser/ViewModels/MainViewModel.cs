using EventStore.ClientAPI;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private string _metadata = "";

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
            get => _metadata;
            private set => Set(ref _metadata, value);
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

                StreamMetadata = await connection.GetStreamMetadataJsonAsync(_streamName);

                Events = (await connection.ReadEventsAsync(_streamName))
                    .Select(ev => new EventViewModel(ev.Event))
                    .Reverse()
                    .ToList();
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

        public async Task CloneTo(CloneArgs args)
        {
            try
            {
                var sourceConnection = EventStoreConnection.Create(DefaultSettings, new Uri(_connectionUri));
                var targetConnection = EventStoreConnection.Create(DefaultSettings, new Uri(args.TargetConnectionUri));

                await sourceConnection.ConnectAsync();
                await targetConnection.ConnectAsync();

                // 1) Read metadata & events from source
                var sourceMetadata = await sourceConnection.GetStreamMetadataAsync(_streamName);
                var targetMetadata = await targetConnection.GetStreamMetadataAsync(args.TargetStreamName);

                var resolvedEvents = args.IncludeEventsBeforeLastSoftDelete
                    ? await sourceConnection.ReadEventsFromAbsoluteStartAsync(_streamName)
                    : await sourceConnection.ReadEventsAsync(_streamName);

                var events = resolvedEvents.ToEventData().ToList();

                // 2) Soft-delete target stream
                await targetConnection.DeleteStreamAsync(args.TargetStreamName, ExpectedVersion.Any);

                // 3) Write metadata & events to target
                await targetConnection.AppendToStreamAsync(args.TargetStreamName, ExpectedVersion.Any, events);

                // 4) Update target metadata so that $tb corresponds to source's $tb
                var targetMetadataAfterDeletion = await targetConnection.GetStreamMetadataAsync(args.TargetStreamName);

                var newMetadata = args.IncludeMetadata
                    ? sourceMetadata.StreamMetadata.Copy()
                    : targetMetadata.StreamMetadata.Copy();
                    
                if (args.IncludeEventsBeforeLastSoftDelete)
                {
                    var targetTb =
                        targetMetadataAfterDeletion.StreamMetadata.TruncateBefore.GetValueOrDefault() +
                        sourceMetadata.StreamMetadata.TruncateBefore.GetValueOrDefault();

                    newMetadata.SetTruncateBefore(targetTb);
                }
                else
                {
                    newMetadata.SetTruncateBefore(targetMetadataAfterDeletion.StreamMetadata.TruncateBefore.GetValueOrDefault());
                }
                
                await targetConnection.SetStreamMetadataAsync(args.TargetStreamName, ExpectedVersion.Any, newMetadata);
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
            var newEvents = (await connection.ReadEventsAsync(_streamName, positionOfLastSoftDelete.GetValueOrDefault() - 1))
                .ToEventData()
                .ToList();

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

        public async Task EditMetadataAsync(Window owner)
        {
            var connection = EventStoreConnection.Create(DefaultSettings, new Uri(_connectionUri));
            await connection.ConnectAsync();

            var meta = await connection.GetStreamMetadataJsonAsync(_streamName);

            var dialog = new MetadataWindow
            {
                Owner = owner,
                MetadataJsonString = meta
            };

            if (dialog.ShowDialog() == true)
            {
                await connection.SetStreamMetadataJsonAsync(_streamName, dialog.MetadataJsonString);
            }
        }
    }
}
