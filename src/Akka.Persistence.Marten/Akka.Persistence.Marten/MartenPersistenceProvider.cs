using Akka.Actor;

namespace Akka.Persistence.Marten
{
    /// <summary>
    /// Extension Id provider for the Marten Persistence extension.
    /// </summary>
    public class MartenPersistenceProvider : ExtensionIdProvider<MartenPersistence>
    {
        /// <summary>
        /// Creates an actor system extension for akka persistence Marten support.
        /// </summary>
        /// <param name="system"></param>
        /// <returns></returns>
        public override MartenPersistence CreateExtension(ExtendedActorSystem system)
        {
            return new MartenPersistence(system);
        }
    }
}
