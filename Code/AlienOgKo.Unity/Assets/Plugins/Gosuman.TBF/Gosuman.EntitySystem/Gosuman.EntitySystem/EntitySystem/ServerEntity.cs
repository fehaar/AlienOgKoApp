namespace Gosuman.EntitySystem
{
    /// <summary>
    /// A server entity is an entity that is never sent to the client.
    /// Derive from this if you want to have entities that are only on the server.
    /// </summary>
    public abstract class ServerEntity : Entity
    {
    }
}
