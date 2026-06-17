namespace Gosuman.TBF.Entities
{
    public class WeightedEntry<T> where T : Entity
    {
        public float Weight { get; set; }
        public T? Entity { get; set; }
    }
}
