using System;
using Avalonia.Threading;
using AvaloniaHex.Document;

namespace TinyProcVisualizer.Views;

public class RealTimeFixedSizeBinaryDocument : MemoryBinaryDocument
{
    private static readonly TimeSpan DEFAULT_UPDATE_INTERVAL = TimeSpan.FromMilliseconds(100);
    public RealTimeFixedSizeBinaryDocument(ReadOnlySpan<byte> bytes, TimeSpan? updateInterval) : base(bytes.ToArray())
    {
        updateInterval ??= DEFAULT_UPDATE_INTERVAL;
        var updateTimer = new DispatcherTimer(updateInterval.Value, DispatcherPriority.Background, UpdateDocument);
        updateTimer.Start();
    }

    public void SetLiveUpdateRanges(BitRange[] ranges)
    {
        // Not implemented
        // Used when e.g. the user changes some bytes and doesnt want
        // them to be overriden back when the thread updates from real memory
    }

    public void WriteNewDataToLiveBuffer(ReadOnlySpan<byte> bytes)
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