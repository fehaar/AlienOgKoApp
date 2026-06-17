namespace Gosuman.EntitySystem
{
    /// <summary>
    /// This is a special entity that is used to indicate that another entity should be removed from the database
    /// when it is returned from executing an action.
    /// It will never live inside the database itself.
    /// The Id of the RemoveEntity telss us what entity to remove.
    /// </summary>
    [Serializable]
    public class RemoveEntity : TemporaryEntity
    {
        public Entity? Entity { get; internal set; }


        public RemoveEntity()
        {
        }

        public RemoveEntity(Entity entity)
        {
            Entity = entity;
            Id = entity.Id;
        }
    }
}
