namespace Akka.Persistence.Marten.Snapshot
{
    /// <summary>
    /// Class used for storing a Snapshot
    /// </summary>
    internal class SnapshotEntry
    {
        public string Id { get; set; }

        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }

        public bool IsDeleted { get; set; }

        public long Timestamp { get; set; }

        public object Snapshot { get; set; }
    }
}
