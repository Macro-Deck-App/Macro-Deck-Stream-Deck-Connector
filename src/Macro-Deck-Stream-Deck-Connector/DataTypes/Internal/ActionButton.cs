using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MacroDeck.StreamDeckConnector.Utils;
using OpenMacroBoard.SDK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MacroDeck.StreamDeckConnector.DataTypes.Internal;

public class ActionButton
{
    private readonly int _size;
    private readonly Color? _backgroundColor;
    private readonly string? _iconImageBase64;
    private readonly string? _labelImageBase64;
    public int Row { get; }

    public int Column { get; }
    
    public ActionButtonFrame? CurrentFrame => Frames.Count -1 >= CurrentFrameIndex ? Frames[CurrentFrameIndex] : null;

    private List<ActionButtonFrame> Frames { get; set; } = new();

    private int CurrentFrameIndex { get; set; }
    
    private DateTime LastFrameUpdate { get; set; } = DateTime.MinValue;

    public ActionButton(
        int row,
        int column,
        int size,
        Color? backgroundColor,
        string? iconImageBase64,
        string? labelImageBase64)
    {
        _size = size;
        _backgroundColor = backgroundColor;
        _iconImageBase64 = iconImageBase64;
        _labelImageBase64 = labelImageBase64;
        Row = row;
        Column = column;
    }

    public void Update()
    {
        if (Frames.Count < 2)
        {
            return;
        }

        var currentFrameDelay = CurrentFrame?.Delay;
        if (currentFrameDelay is null
            || DateTime.Now - LastFrameUpdate < TimeSpan.FromMilliseconds(currentFrameDelay.Value * 10))
        {
            return;
        }

        if (CurrentFrameIndex >= Frames.Count - 1)
        {
            CurrentFrameIndex = 0;
        }
        else
        {
            CurrentFrameIndex++;
        }

        LastFrameUpdate = DateTime.Now;
    }
    
    public async Task RenderButtonImage(CancellationToken cancellationToken)
    {
        if (FrameCache.TryGetFromCache(_backgroundColor, _iconImageBase64, _labelImageBase64, out var cachedFrames))
        {
            Frames = cachedFrames;
            CurrentFrameIndex = 0;
        }
        
        var labelImage = !string.IsNullOrWhiteSpace(_labelImageBase64)
            ? Image.Load(Convert.FromBase64String(_labelImageBase64))
            : new Image<Rgba32>(_size, _size);
        var iconImage = !string.IsNullOrWhiteSpace(_iconImageBase64)
            ? Image.Load(Convert.FromBase64String(_iconImageBase64))
            : new Image<Rgba32>(_size, _size);
        
        labelImage?.Mutate(x => x.Resize(_size, _size));
        iconImage.Mutate(x => x.Resize(_size, _size));
        
        var point = new Point(0, 0);
        
        var frames = new ConcurrentDictionary<int, ActionButtonFrame>();
        await Task.WhenAll(Enumerable.Range(0, iconImage.Frames.Count).Select(index =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var tmp = new Image<Rgba32>(_size, _size);
            tmp.Mutate(x => x.BackgroundColor(_backgroundColor ?? Color.FromRgb(35, 35, 35)));

            var frame = iconImage.Frames.CloneFrame(index);
            tmp.Mutate(x => x.DrawImage(frame, point, 1));
            if (labelImage is not null)
            {
                tmp.Mutate(x => x.DrawImage(labelImage, point, 1));
            }

            var delay = frame.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay;
            var keyBitmap = KeyBitmap.Create.FromImageSharpImage(tmp);
            var actionButtonFrame = new ActionButtonFrame(delay, keyBitmap);
            frames[index] = actionButtonFrame;
            
            if (index == 0)
            {
                CurrentFrameIndex = 0;
                Frames = new List<ActionButtonFrame> { actionButtonFrame };
            } 
            
            return Task.CompletedTask;
        }));
        
        cancellationToken.ThrowIfCancellationRequested();
        
        Frames = frames.OrderBy(x => x.Key).Select(x => x.Value).ToList();
        FrameCache.AddToCache(_backgroundColor, _iconImageBase64, _labelImageBase64, Frames);
        CurrentFrameIndex = 0;
    }
}