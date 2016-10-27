using System;
using System.Configuration;
using Akka.Persistence.TestKit.Snapshot;
using Xunit;

namespace Akka.Persistence.Marten.Tests
{
    [Collection("MartenSpec")]
    public class MartenSnapshotStoreSpec : SnapshotStoreSpec
    {
        private static string _connectionString = ConfigurationManager.ConnectionStrings["TestDB"].ConnectionString;
        private static string SpecConfig = @"
            akka.test.single-expect-default = 3s
            akka.persistence {
                publish-plugin-commands = on
                snapshot-store {
                    plugin = ""akka.persistence.snapshot-store.marten""
                    marten {
                        class = ""Akka.Persistence.Marten.Snapshot.MartenSnapshotStore, Akka.Persistence.Marten""
                        connection-string = ""<ConnectionString>""
                        use-soft-delete = false
                    }
                }
            }";

        public MartenSnapshotStoreSpec() : base(CreateSpecConfig(), "MartenSnapshotStoreSpec")
        {
            AppDomain.CurrentDomain.DomainUnload += (_, __) =>
            {
                try
                {
                    Dispose();
                }
                catch { }
            };
            
            Initialize();
        }

        private static string CreateSpecConfig()
        {
            SpecConfig = SpecConfig.Replace("<ConnectionString>", _connectionString);
            return SpecConfig;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}