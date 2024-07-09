using System;
using System.Net.WebSockets;

namespace MacroDeck.StreamDeckConnector.Utils;

public class WebSocketClientFactory
{
    public static Func<ClientWebSocket> GetWebSocket()
    {
        return () => new ClientWebSocket
        {
            Options =
            {
                KeepAliveInterval = TimeSpan.FromSeconds(5),
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            }
        };
    }
}