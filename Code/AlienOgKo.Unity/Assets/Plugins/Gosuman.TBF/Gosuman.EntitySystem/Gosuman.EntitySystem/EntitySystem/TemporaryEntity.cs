using System;

namespace Gosuman.EntitySystem
{
    /// <summary>
    /// A temporary entity is an entity that is not stored in the database.
    /// It can be used for things that need to go to the client afer a round but should not persist.
    /// </summary>
    [Serializable]
    public class TemporaryEntity : Entity
    {
    }
}
