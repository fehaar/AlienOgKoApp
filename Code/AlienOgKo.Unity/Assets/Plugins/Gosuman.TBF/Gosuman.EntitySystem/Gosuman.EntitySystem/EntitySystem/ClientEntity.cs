namespace Gosuman.EntitySystem
{
    /// <summary>
    /// A client entity is an entity that can be seen by the client.
    /// It does implement a secret property that can be used to hide the entity from the client until at some point when it is revealed.
    /// </summary>
    public abstract class ClientEntity : Entity
    {
        public bool Secret { get; set; }
    }
}
