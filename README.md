# Macro Deck Stream Deck Connector
This software bridges the gap between Macro Deck and the Stream Deck.
It connects via websocket to Macro Deck and directly communicates with the Stream Deck over USB, independent of the Stream Deck software.

## Usage
- Install the [Stream Deck Connector plugin](https://github.com/Macro-Deck-org/Stream-Deck-Connector-Plugin) in Macro Deck (Recommended way)
- Or run this program separately

## Start parameters
| Parameter | Description |
| --- | --- |
| --host [127.0.0.1:8191] | Sets the host and port |
| --long-press-ms [1000] | Sets the delay for the long press in milliseconds |

## Important
- You need to own a Stream Deck to use this plugin
- The official Stream Deck software must be closed
- This software is independent of the official Stream Deck software. You don't even have to install it.

## Features
- Independent
- Hot-plugging
- Multiple devices connected at once
- Animated icons
- Short press/long press

## Compatibility
- Stream Deck original (Not tested)
- Stream Deck mk.2 (Tested, works)
- Stream Deck Mini (Not tested)
- Stream Deck XL (Not tested)


## Third party licenses
This software uses some awesome 3rd party libraries:
- [DeckSurf SDK (MIT)](https://github.com/dend/decksurf-sdk)
- [HidSharp (Apache 2.0)](https://www.zer7.com/software/hidsharp)
- [Newtonsoft.Json (MIT)](https://www.newtonsoft.com/json)
- [Usb.Events (MIT)](https://github.com/Jinjinov/Usb.Events)
- [Websocket.Client (MIT)](https://github.com/Marfusios/websocket-client))
- [Dotnet Core (MIT)](https://github.com/dotnet/core)
