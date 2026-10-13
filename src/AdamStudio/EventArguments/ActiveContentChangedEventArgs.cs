using System;

namespace AdamStudio.EventArguments
{
    public sealed class ActiveContentChangedEventArgs : EventArgs
    {
        public object? OldContent { get; init; }
        public object? NewContent { get; init; }
    }
}
