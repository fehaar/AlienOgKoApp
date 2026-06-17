namespace Gosuman.EntitySystem
{
    public abstract class OwnedEntity : Entity
    {
        public string PlayerId { get; set; } = string.Empty;
    }
}
