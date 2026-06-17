namespace Gosuman.TBF.Entities
{
    public class CardCount<T, T1> where T : Blueprint<T1> where T1 : Entity
    {
        public int Count { get; set; }
        public T? Entity { get; set; }
    }
}
