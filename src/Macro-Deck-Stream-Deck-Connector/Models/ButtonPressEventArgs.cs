using System;
using MacroDeck.StreamDeckConnector.Enums;

namespace MacroDeck.StreamDeckConnector.Models;

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