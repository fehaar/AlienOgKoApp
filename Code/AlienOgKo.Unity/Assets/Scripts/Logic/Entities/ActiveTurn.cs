using Gosuman.EntitySystem;
using Gosuman.TBF.Shared.Entities;

namespace Gosuman.TBF.Logic.Entities;

public class ActiveTurn : Entity
{
    public Player? ActivePlayer { get; set; }
}