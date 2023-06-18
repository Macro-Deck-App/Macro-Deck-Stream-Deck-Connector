using System;

namespace MacroDeck.StreamDeckConnector;

public class ButtonPressEventArgs : EventArgs
{
    public ButtonPressEventArgs(int id, ButtonEventKind kind)
    {
        Id = id;
        Kind = kind;
    }

    public int Id { get; }
    public ButtonEventKind Kind { get; }
}