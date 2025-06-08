using System;
using Avalonia.Threading;
using AvaloniaHex.Document;

namespace TinyProcVisualizer.Views;

public class RealTimeFixedSizeBinaryDocument : MemoryBinaryDocument
{
    private static readonly TimeSpan DEFAULT_UPDATE_INTERVAL = TimeSpan.FromMilliseconds(100);
    public RealTimeFixedSizeBinaryDocument(byte[] bytes, TimeSpan? updateInterval) : base(bytes)
    {
        updateInterval ??= DEFAULT_UPDATE_INTERVAL;
        var updateTimer = new DispatcherTimer(updateInterval.Value, DispatcherPriority.Background, UpdateDocument);
        updateTimer.Start();
    }

    public void WriteNewDataToLiveBuffer(byte[] bytes)
    {
        bytes.CopyTo(Memory.Span);
    }

    private void UpdateDocument(object? sender, EventArgs e)
    {
        // TODO: For future implementation, only change the affected memory regions (only if performance improves)
        BitRange changedMemoryRange = new(0, (ulong)Memory.Length - 1);
        // var span = Memory.ToArray();
        // span.CopyTo(Memory.Span);
        OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Modify, changedMemoryRange));
    }

    public void ForceReload() => UpdateDocument(this, null);
}