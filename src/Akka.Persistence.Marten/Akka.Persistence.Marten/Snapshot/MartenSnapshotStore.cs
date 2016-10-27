using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Persistence.Snapshot;
using Akka.Util.Internal;
using Marten;

namespace Akka.Persistence.Marten.Snapshot
{
    public class MartenSnapshotStore : SnapshotStore
    {
        private readonly MartenSnapshotSettings _settings;
        private Lazy<IDocumentStore> _documentStore;
        public MartenSnapshotStore()
        {
            _settings = MartenPersistence.Get(Context.System).SnapshotStoreSettings;
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

        protected override async Task<SelectedSnapshot> LoadAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var item = await session.Query<SnapshotEntry>()
                .Where(x => x.IsDeleted == false)
                .Where(x => x.PersistenceId.Equals(persistenceId))
                .Where(x => x.SequenceNr <= criteria.MaxSequenceNr)
                .Where(x => x.Timestamp <= criteria.MaxTimeStamp.Ticks)
                .OrderByDescending(x => x.SequenceNr)
                .FirstOrDefaultAsync();

                return item == null ? null : new SelectedSnapshot(new SnapshotMetadata(item.PersistenceId, item.SequenceNr, new DateTime(item.Timestamp)), item.Snapshot);
            }
        }

        protected override async Task SaveAsync(SnapshotMetadata metadata, object snapshot)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var item = new SnapshotEntry
                {
                    Id = metadata.PersistenceId + "_" + metadata.SequenceNr,
                    PersistenceId = metadata.PersistenceId,
                    SequenceNr = metadata.SequenceNr,
                    IsDeleted = false,
                    Snapshot = snapshot,
                    Timestamp = metadata.Timestamp.Ticks
                };

                session.Store(item);
                await session.SaveChangesAsync();
            }
        }

        protected override async Task DeleteAsync(SnapshotMetadata metadata)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var item = await session.Query<SnapshotEntry>()
                    .Where(x => x.PersistenceId.Equals(metadata.PersistenceId))
                    .Where(x => x.SequenceNr.Equals(metadata.SequenceNr))
                    .FirstOrDefaultAsync();

                if (item != null)
                {
                    if (_settings.UseSoftDelete)
                    {
                        item.IsDeleted = true;
                        session.Store(item);
                    }
                    else
                    {
                        session.Delete(item);
                    }

                    await session.SaveChangesAsync();
                }
            }
        }

        protected override async Task DeleteAsync(string persistenceId, SnapshotSelectionCriteria criteria)
        {
            using (var session = _documentStore.Value.LightweightSession())
            {
                var entries = await session.Query<SnapshotEntry>()
                    .Where(x => x.PersistenceId.Equals(persistenceId))
                    .Where(x => x.SequenceNr <= criteria.MaxSequenceNr)
                    .Where(x => x.Timestamp <= criteria.MaxTimeStamp.Ticks)
                    .ToListAsync();

                if (_settings.UseSoftDelete)
                {
                    entries.ForEach(x => x.IsDeleted = true);
                }

                session.StoreObjects(entries);

                await session.SaveChangesAsync();
            }
        }
    }
}