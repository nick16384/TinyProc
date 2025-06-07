using System;
using Avalonia.Threading;
using AvaloniaHex.Document;

namespace TinyProcVisualizer.Views;

public class RealTimeFixedSizeBinaryDocument : MemoryBinaryDocument
{
    public RealTimeFixedSizeBinaryDocument(byte[] bytes, TimeSpan updateInterval) : base(bytes)
    {
        var timer = new DispatcherTimer(updateInterval, DispatcherPriority.Background, UpdateDocument);
        timer.Start();
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