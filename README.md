# Akka.Persistence.Marten
Akka Persistence journal and snapshot store runned by Marten and stored in a Postgresql 9.5+ database.

[Download NuGet Package](https://www.nuget.org/packages/Akka.Persistence.Marten)

HOCON Configuration Usage 

    akka.persistence {
        journal {
            plugin = "akka.persistence.journal.marten"
            marten {
                # qualified type name of the Marten persistence journal actor
                class = "Akka.Persistence.Marten.Journal.MartenJournal, Akka.Persistence.Marten"

                # connection string used for database access
                connection-string = "host=localhost;database=marten_test;password=mypassword;username=someuser"

                # set journal entry to IsDeleted instead of permanent delete of journal entry
                use-soft-delete = false

                # dispatcher used to drive journal actor
                plugin-dispatcher = "akka.actor.default-dispatcher"
            }
        }

        snapshot-store {
            plugin = "akka.persistence.snapshot-store.marten"
            marten {
                # qualified type name of the Marten persistence snapshot actor
                class = "Akka.Persistence.Marten.Snapshot.MartenSnapshotStore, Akka.Persistence.Marten"

                # connection string used for database access
                connection-string = "host=localhost;database=marten_test;password=mypassword;username=someuser"

                # set snapshot entry to IsDeleted instead of permanent delete of snapshot entry
                use-soft-delete = false

                # dispatcher used to drive snapshot storage actor
                plugin-dispatcher = "akka.actor.default-dispatcher"
            }
        }
    }
