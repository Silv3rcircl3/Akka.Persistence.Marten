using System;
using System.Configuration;
using Akka.Persistence.TestKit.Journal;
using Baseline.Reflection;
using Marten;
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
            using (var connection = new Npgsql.NpgsqlConnection(_connectionString))
            {
                connection.Open();

                var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM public.mt_streams";
                deleteCmd.ExecuteNonQuery();


                deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = "DELETE FROM public.mt_doc_metadataentry";
                deleteCmd.ExecuteNonQuery();

                connection.Close();
            }
        }
    }
}