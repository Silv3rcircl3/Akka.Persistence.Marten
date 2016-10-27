namespace Akka.Persistence.Marten.Journal
{
    /// <summary>
    /// Class used for storing a journal Metadata
    /// </summary>
    internal class MetadataEntry
    {
        public string Id { get; set; }

        public string PersistenceId { get; set; }

        public long SequenceNr { get; set; }
    }
}
