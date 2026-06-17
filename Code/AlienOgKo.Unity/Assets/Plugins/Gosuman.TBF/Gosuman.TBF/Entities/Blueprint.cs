namespace Gosuman.TBF.Entities
{
    public abstract class Blueprint<T> : ServerEntity where T: Entity
    {
        public Blueprint()
        {
        }

        public string Name = string.Empty;

        public abstract T Create(Random rnd);
    }
}
