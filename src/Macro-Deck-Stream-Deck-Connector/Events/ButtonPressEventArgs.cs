using System;
using MacroDeck.StreamDeckConnector.DataTypes.Internal.Enums;

namespace MacroDeck.StreamDeckConnector.Events;

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