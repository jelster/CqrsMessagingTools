namespace MIL.Visitors
{
    public enum MilTokenType
    {
        Indeterminate = 0,
        Command,
        CommandHandler,
        Event,
        EventHandler,
        AggregateRoot,
        StateObject,
        Publisher,
        Scope,
        Delay,
        LanguageElement,
        ScopeContinuation,
        Association
    }
}