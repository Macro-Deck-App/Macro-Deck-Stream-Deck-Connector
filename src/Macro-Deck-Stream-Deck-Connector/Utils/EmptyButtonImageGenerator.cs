using System.Collections.Generic;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MacroDeck.StreamDeckConnector.Utils;

public static class EmptyButtonImageGenerator
{
    private static readonly Dictionary<int, KeyBitmap> CachedButtonImages = new();
    
    public static KeyBitmap GetEmptyButton(int size)
    {
        if (CachedButtonImages.TryGetValue(size, out var result))
        {
            return result;
        }
        using var tmp = new Image<Rgba32>(size, size);
        tmp.Mutate(x => x.BackgroundColor(Color.FromRgb(32, 32, 32)));
        result = KeyBitmap.Create.FromImageSharpImage(tmp);
        CachedButtonImages.TryAdd(size, result);
        return result;
    }
}