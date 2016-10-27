using System;
using System.Configuration;
using Akka.Persistence.TestKit.Journal;
using Xunit;

namespace Akka.Persistence.Marten.Tests
{
    [Collection("MartenSpec")]
    public class MartenJournalSpec : JournalSpec
    {
        private static string _connectionString = ConfigurationManager.ConnectionStrings["TestDB"].ConnectionString;
        private static string SpecConfig = @"
            akka.test.single-expect-default = 3s
            akka.persistence {
                publish-plugin-commands = on
                journal {
                    plugin = ""akka.persistence.journal.marten""
                    marten {
                        class = ""Akka.Persistence.Marten.Journal.MartenJournal, Akka.Persistence.Marten""
                        connection-string = ""<ConnectionString>""
                        use-soft-delete = false
                    }
                }
            }";

        public MartenJournalSpec() : base(CreateSpecConfig(), "MartenJournalSpec")
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