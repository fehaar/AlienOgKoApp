using Gosuman.EntitySystem;

namespace Gosuman.TBF.Shared.Entities
{
    public enum GamePhases
    {
        NotStarted,
        Starting,
        Looping,
        Ending,
        Ended
    }

    public class GameState : Entity
    {
        public GamePhases Phase { get; set; } = GamePhases.NotStarted;
    }
}