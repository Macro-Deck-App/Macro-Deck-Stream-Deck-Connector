using System.Collections.Generic;
using MacroDeck.StreamDeckConnector.Models;

namespace MacroDeck.StreamDeckConnector.Services;

public class ButtonService
{
    private List<ActionButtonModel> _buttons = new();

    private object _buttonsLock = new ();

    public void SetButtons(List<ActionButtonModel> buttons)
    {
        lock (_buttonsLock)
        {
            _buttons = buttons;
        }
    }
}