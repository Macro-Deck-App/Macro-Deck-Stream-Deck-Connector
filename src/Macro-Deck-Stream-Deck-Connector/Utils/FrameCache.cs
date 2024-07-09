using System.Collections.Concurrent;
using System.Collections.Generic;
using MacroDeck.StreamDeckConnector.DataTypes.Internal;
using SixLabors.ImageSharp;

namespace MacroDeck.StreamDeckConnector.Utils;

public class FrameCache
{
    private static readonly ConcurrentDictionary<int, List<ActionButtonFrame>> Cache = new();

    public static void AddToCache(
        Color? backgroundColor,
        string? iconBase64,
        string? labelBase64,
        List<ActionButtonFrame> frames)
    {
        var hash = $"{backgroundColor?.ToHex()}{iconBase64}{labelBase64}".GetHashCode();
        Cache.TryAdd(hash, frames);
    }

    public static bool TryGetFromCache(
        Color? backgroundColor,
        string? iconBase64,
        string? labelBase64,
        out List<ActionButtonFrame> frames)
    {
        frames = new List<ActionButtonFrame>();
        var hash = $"{backgroundColor?.ToHex()}{iconBase64}{labelBase64}".GetHashCode();
        var result = Cache.TryGetValue(hash, out var framesResult);
        if (!result || framesResult is null)
        {
            return false;
        }

        frames = framesResult;
        return true;
    }
}