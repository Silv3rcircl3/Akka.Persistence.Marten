using System;
using Akka.Configuration;

namespace Akka.Persistence.Marten
{
    /// <summary>
    /// Settings for the Marten persistence implementation, parsed from HOCON configuration.
    /// </summary>
    public abstract class MartenSettings
    {
        /// <summary>
        /// Connection string used to access the Marten, also specifies the database.
        /// </summary>
        public string ConnectionString { get; private set; }

        /// <summary>
        /// Use soft deletes in the database for the event journal or snapshots
        /// </summary>
        public bool UseSoftDelete { get; set; }

        protected MartenSettings(Config config)
        {
            ConnectionString = config.GetString("connection-string");
            UseSoftDelete = config.GetBoolean("use-soft-delete");
        }
    }


    /// <summary>
    /// Settings for the Marten journal implementation, parsed from HOCON configuration.
    /// </summary>
    public class MartenJournalSettings : MartenSettings
    {
        public MartenJournalSettings(Config config) : base(config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config),
                    "Marten journal settings cannot be initialized, because required HOCON section couldn't been found");
        }
    }


    /// <summary>
    /// Settings for the Marten snapshot implementation, parsed from HOCON configuration.
    /// </summary>
    public class MartenSnapshotSettings : MartenSettings
    {
        public MartenSnapshotSettings(Config config) : base(config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config),
                    "Marten snapshot settings cannot be initialized, because required HOCON section couldn't been found");
        }
    }
}
