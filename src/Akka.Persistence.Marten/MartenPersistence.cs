using Akka.Actor;
using Akka.Configuration;
using System;

namespace Akka.Persistence.Marten
{
    /// <summary>
    /// An actor system extension initializing support for Marten persistence layer.
    /// </summary>
    public class MartenPersistence : IExtension
    {
        /// <summary>
        /// Returns a default configuration for akka persistence Marten journal and snapshot store.
        /// </summary>
        /// <returns></returns>
        public static Config DefaultConfiguration()
        {
            var hoconConfig = @"akka.persistence {
	                                journal {
		                                marten {
			                                # qualified type name of the Marten persistence journal actor
			                                class = ""Akka.Persistence.Marten.Journal.MartenJournal, Akka.Persistence.Marten""

                                            # connection string used for database access
                                            connection-string = ""host=localhost;database=marten_test;password=mypassword;username=someuser""

                                            # set journal entry to IsDeleted instead of permanent delete of journal entry
                                            use-soft-delete = ""false""

                                            # dispatcher used to drive journal actor
                                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                                        }
                                    }

                                    snapshot-store {
		                                marten {
			                                # qualified type name of the Marten persistence snapshot actor
			                                class = ""Akka.Persistence.Marten.Snapshot.MartenSnapshotStore, Akka.Persistence.Marten""

                                            # connection string used for database access
                                            connection-string = ""host=localhost;database=marten_test;password=mypassword;username=someuser""

                                            # set snapshot entry to IsDeleted instead of permanent delete of snapshot entry
                                            use-soft-delete = ""false""

                                            # dispatcher used to drive snapshot storage actor
                                            plugin-dispatcher = ""akka.actor.default-dispatcher""
                                        }
	                                }
                                }";
            return ConfigurationFactory.ParseString(hoconConfig);
        }

        public static MartenPersistence Get(ActorSystem system)
        {
            return system.WithExtension<MartenPersistence, MartenPersistenceProvider>();
        }

        /// <summary>
        /// The settings for the Marten journal.
        /// </summary>
        public MartenJournalSettings JournalSettings { get; }

        /// <summary>
        /// The settings for the Marten snapshot store.
        /// </summary>
        public MartenSnapshotSettings SnapshotStoreSettings { get; }

        public MartenPersistence(ExtendedActorSystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            // Initialize fallback configuration defaults
            system.Settings.InjectTopLevelFallback(DefaultConfiguration());

            // Read config
            var journalConfig = system.Settings.Config.GetConfig("akka.persistence.journal.marten");
            JournalSettings = new MartenJournalSettings(journalConfig);

            var snapshotConfig = system.Settings.Config.GetConfig("akka.persistence.snapshot-store.marten");
            SnapshotStoreSettings = new MartenSnapshotSettings(snapshotConfig);
        }
    }
}
