namespace Gosuman.TBF.Shared
{
    internal class TestAction : IClientGameAction
    {
        public int Value { get; set; } = 0;
    }

    internal class TestActionWithDelegate : IClientGameAction
    {
        public Action? Delegate { get; set; } = null;
    }
}
