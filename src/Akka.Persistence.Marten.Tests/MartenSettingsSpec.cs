using FluentAssertions;
using Xunit;

namespace Akka.Persistence.Marten.Tests
{
    [Collection("MartenSpec")]
    public class MartenSettingsSpec : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public void Marten_JournalSettings_must_have_default_values()
        {
            var MartenPersistence = Marten.MartenPersistence.Get(Sys);

            MartenPersistence.JournalSettings.ConnectionString.Should().Be("host=localhost;database=marten_test;password=mypassword;username=someuser");
            MartenPersistence.JournalSettings.UseSoftDelete.Should().BeFalse();
        }

        [Fact]
        public void Marten_SnapshotStoreSettingsSettings_must_have_default_values()
        {
            var MartenPersistence = Marten.MartenPersistence.Get(Sys);

            MartenPersistence.SnapshotStoreSettings.ConnectionString.Should().Be("host=localhost;database=marten_test;password=mypassword;username=someuser");
            MartenPersistence.SnapshotStoreSettings.UseSoftDelete.Should().BeFalse();
        }
    }
}