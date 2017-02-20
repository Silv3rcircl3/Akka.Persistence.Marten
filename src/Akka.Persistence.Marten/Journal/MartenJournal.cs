using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Journal;
using Akka.Util.Internal;
using Marten;
using Marten.Events;

namespace Akka.Persistence.Marten.Journal
{
    public class MartenJournal : AsyncWriteJournal
    {
        private readonly MartenJournalSettings _settings;
        private Lazy<IDocumentStore> _documentStore;
        public MartenJournal()
        {
            _settings = MartenPersistence.Get(Context.System).JournalSettings;
        }

        protected override void PreStart()
        {
            base.PreStart();
            _documentStore = new Lazy<IDocumentStore>(CreateStore);
        }

        private IDocumentStore CreateStore()
        {           
            return DocumentStore.For(_settings.ConnectionString);
        }

        public override async Task ReplayMessagesAsync(IActorContext context, string persistenceId, long fromSequenceNr, long toSequenceNr, long max, Action<IPersistentRepresentation> recoveryCallback)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var streamId = await session.Query<MetadataEntry>()
                    .Where(m => m.Id == persistenceId)
                    .Select(m => m.StreamId)
                    .FirstAsync();

                var docs = await session.Events.QueryAllRawEvents()
                    .Where(e => e.StreamId == streamId && e.Version >= fromSequenceNr && e.Version <= toSequenceNr)
                    .Take(max > int.MaxValue ? int.MaxValue : (int) max)
                    .ToListAsync();

                docs
                    .ForEach(e =>
                            recoveryCallback(new Persistent(e.Data, e.Version, persistenceId, null, false,
                                context.Sender)));
            }
        }

        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var streamId = await session.Query<MetadataEntry>()
                    .Where(m => m.Id == persistenceId)
                    .Select(m => m.StreamId)
                    .FirstOrDefaultAsync();

                if (streamId == default(Guid))
                    return 0;

                var cmd = session.Connection.CreateCommand();
                cmd.CommandText = $"SELECT version FROM public.mt_streams where id = '{streamId}'";
                return (int) await cmd.ExecuteScalarAsync();
            }   
        }

        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<AtomicWrite> messages)
        {
            var msg = messages.ToList();
            var tasks = msg.GroupBy(a => a.PersistenceId)
                .Select(async writes =>
                {
                    using (var session = _documentStore.Value.LightweightSession())
                    {
                        var events = writes
                            .SelectMany(c => (IImmutableList<IPersistentRepresentation>) c.Payload)
                            .Select(c => c.Payload)
                            .ToArray();

                        var metadataEntry = await session.Query<MetadataEntry>()
                            .Where(m => m.Id == writes.Key)
                            .FirstOrDefaultAsync();

                        if (metadataEntry == null)
                        {
                            metadataEntry = new MetadataEntry
                            {
                                Id = writes.Key,
                                StreamId = Guid.NewGuid()
                            };
                            session.Store(metadataEntry);
                            session.Events.StartStream<object>(metadataEntry.StreamId, events);
                        }
                        else
                            session.Events.Append(metadataEntry.StreamId, events);

                        await session.SaveChangesAsync();
                    }
                }).Select(task => task.ContinueWith(t => t.IsFaulted ? TryUnwrapException(t.Exception) : null));

            var result = await Task.WhenAll(tasks).ContinueWith(t => (IImmutableList<Exception>) t.Result.ToImmutableList());
            return msg.Count == 1
                ? result
                : result.AddRange(Enumerable.Range(1, msg.Count-1).Select(_ => null as Exception));
        }

        protected override async Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var streamId = await session.Query<MetadataEntry>()
                    .Where(m => m.Id == persistenceId)
                    .Select(m => m.StreamId)
                    .FirstAsync();

                var deleteCmd = session.Connection.CreateCommand();
                deleteCmd.CommandText = $"DELETE FROM public.mt_events where stream_id = '{streamId}' and version <= {toSequenceNr}";
                await deleteCmd.ExecuteNonQueryAsync();
            }
        }
    }
}
