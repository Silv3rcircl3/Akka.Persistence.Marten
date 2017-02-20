using System;

namespace Akka.Persistence.Marten.Journal
{
    /// <summary>
    /// Class used for storing a journal Metadata
    /// </summary>
    internal class MetadataEntry
    {
        public string Id { get; set; }

        public Guid StreamId { get; set; }
    }
}
