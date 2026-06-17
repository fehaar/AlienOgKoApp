namespace Gosuman.TBF.Interfaces
{
    /// <summary>
    /// These are actions that have a timeout associated with them and are handled solely on the server, as you should never trust the client to handle timeouts.
    /// </summary>
    public interface ITimedAction : IServerGameAction
    {
        public TimeSpan Timeout {get; }
        /// <summary>
        /// Check if this timer should be cancelled if the given action is executed.
        /// </summary>
        /// <param name="action">The action that is about to get executed</param>
        /// <returns>True if the timer should be canceled</returns>
        public bool CancelOnAction(IServerGameAction action);
    }
}