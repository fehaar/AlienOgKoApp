using Gosuman.EntitySystem;

namespace Gosuman.TBF.Shared.Entities
{
    public class Player : Entity
    {
        public Player()
        {
        }

        public Player(string name)
        {
            Name = name;
        }

        public string Name { get; set; } = string.Empty;

        public static readonly Player DummyPlayer = new Player("Dummy") { Id = "Dummy" };
    }
}