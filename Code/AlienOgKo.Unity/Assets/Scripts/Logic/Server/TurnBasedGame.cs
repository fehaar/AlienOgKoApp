using Gosuman.EntitySystem;
using Gosuman.EntitySystem.Database;
using Gosuman.TBF.Logic.Entities;
using Gosuman.TBF.Logic.Server.Actions;
using Gosuman.TBF;
using Gosuman.TBF.Interfaces;
using Gosuman.TBF.Shared.Components;
using Gosuman.TBF.Shared.Entities;

namespace Gosuman.TBF.Logic.Server
{
    /// <summary>
    /// This is the turn based system that encompasses a game where two players take turns to compete against each other
    /// </summary>
    public class TurnBasedGame : TurnBasedSystem
    {
        public TurnBasedGame() : base()
        {
            StrictActionExistsValidation = false;
        }

        public TurnBasedGame(EntityDatabase db) : base(db)
        {
            StrictActionExistsValidation = false;
        }

        protected override bool ValidateStartEntities(IEnumerable<Entity>? entities)
        {
            return (entities != null) && (entities.Any(e => e is Player) && entities.Any(e => e is GameSettings));
        }

        protected override IEnumerable<IServerGameAction> GetStartActions()
        {
            yield return new JoinGameAction();
        }

        protected override IEnumerable<IServerGameAction> GetLoopActions()
        {
            var gameState = Database.GetSingle<GameState>();
            Player activePlayer = default!;
            Player otherPlayer = default!;
            foreach (var player in Database.GetAll<Player>())
            {
                if (player.HasComponent<ActivePlayerComponent>())
                {
                    activePlayer = player;
                }
                else
                {
                    otherPlayer = player;
                }
            }
            if (!Database.Has<ActiveTurn>())
            {
                yield return new StartTurnAction { Player = activePlayer };
            }
            else
            {
                // Here we send the actions for the player
                yield return new SwitchTurnAction();
            }
        }

        protected override IEnumerable<IServerGameAction> GetEndActions()
        {
            yield break;
        }


    }
}
