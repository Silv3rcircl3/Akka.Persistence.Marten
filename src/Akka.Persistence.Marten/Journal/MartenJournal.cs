using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Persistence.Journal;
using Akka.Util.Internal;
using Marten;

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
                var documents = await session.Query<JournalEntry>()
                .Where(x => x.IsDeleted == false)
                .Where(x => x.PersistenceId.Equals(persistenceId))
                .Where(x => x.SequenceNr >= fromSequenceNr)
                .Where(x => x.SequenceNr <= toSequenceNr)
                .OrderBy(x => x.SequenceNr)
                .ToListAsync();

                documents.ForEach(doc =>
                {
                    recoveryCallback(new Persistent(doc.Payload, doc.SequenceNr, doc.PersistenceId, doc.Manifest, doc.IsDeleted, context.Sender));
                });
            }
        }

        public override async Task<long> ReadHighestSequenceNrAsync(string persistenceId, long fromSequenceNr)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var highSeqNr = await session.Query<MetadataEntry>()
               .Where(x => x.PersistenceId.Equals(persistenceId))
               .OrderByDescending(x => x.SequenceNr)
               .Select(x => x.SequenceNr)
               .FirstOrDefaultAsync();

                return highSeqNr;
            }            
        }

        protected override async Task<IImmutableList<Exception>> WriteMessagesAsync(IEnumerable<global::Akka.Persistence.AtomicWrite> messages)
        {
            
            var messageList = messages.ToList();
            var writeTasks = messageList.Select(async message =>
            {
                var persistentMessages = ((IImmutableList<IPersistentRepresentation>) message.Payload).ToArray();

                var journalEntries = persistentMessages.ToList();

                var entries = journalEntries.Select(x => new JournalEntry()
                {
                    Id = x.PersistenceId + "_" + x.SequenceNr,
                    IsDeleted = x.IsDeleted,
                    Payload = x.Payload,
                    PersistenceId = x.PersistenceId,
                    SequenceNr = x.SequenceNr,
                    Manifest = x.Manifest
                });

                using (var session = _documentStore.Value.LightweightSession())
                {
                    foreach (var entry in entries)
                    {
                        session.Store(entry);
                    }

                    await session.SaveChangesAsync();
                }
            });

            await UpdatedHighestSeqNr(messageList);

            return await Task<IImmutableList<Exception>>
                .Factory
                .ContinueWhenAll(writeTasks.ToArray(),
                    tasks => tasks.Select(t => t.IsFaulted ? TryUnwrapException(t.Exception) : null).ToImmutableList());
        }

        protected override async Task DeleteMessagesToAsync(string persistenceId, long toSequenceNr)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var entries = await session.Query<JournalEntry>()
                    .Where(x => x.PersistenceId.Equals(persistenceId))
                    .Where(x => x.SequenceNr <= toSequenceNr)
                    .ToListAsync();

                if (_settings.UseSoftDelete)
                {
                    entries.ForEach(x => x.IsDeleted = true);
                }

                session.StoreObjects(entries);
                
                await session.SaveChangesAsync();
            }
        }

        private async Task UpdatedHighestSeqNr(List<global::Akka.Persistence.AtomicWrite> messageList)
        {
            var persistenceId = messageList.Select(c => c.PersistenceId).First();
            var highSequenceId = messageList.Max(c => c.HighestSequenceNr);

            var metadataEntry = new MetadataEntry
            {
                Id = persistenceId,
                PersistenceId = persistenceId,
                SequenceNr = highSequenceId
            };

            using (var session = _documentStore.Value.LightweightSession())
            {
                session.Store(metadataEntry);
                await session.SaveChangesAsync();
            } 
        }
    }
}
