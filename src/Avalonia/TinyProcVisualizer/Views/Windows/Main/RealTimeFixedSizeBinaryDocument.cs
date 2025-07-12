using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Threading;
using AvaloniaHex.Document;

namespace TinyProcVisualizer.Views.Windows.Main;

public class RealTimeFixedSizeBinaryDocument : MemoryBinaryDocument
{
    private static readonly TimeSpan DEFAULT_UPDATE_INTERVAL = TimeSpan.FromMilliseconds(100);
    public RealTimeFixedSizeBinaryDocument(ReadOnlySpan<byte> bytes, TimeSpan? updateInterval) : base(bytes.ToArray())
    {
        updateInterval ??= DEFAULT_UPDATE_INTERVAL;
        var updateTimer = new DispatcherTimer(updateInterval.Value, DispatcherPriority.Background, UpdateDocument);
        ResetUpdateRanges();
        updateTimer.Start();
    }

    private List<BitRange> _updatingBitRanges = [];
    public List<BitRange> UpdatingBitRanges
    {
        get => _updatingBitRanges;
        set
        {
            _updatingBitRanges = value;
            CleanupUpdateRanges();
        }
    }

    // Sort, remove redundant, and merge ranges next to each other
    private void CleanupUpdateRanges()
    {
        // Sort the list of ranges
        _updatingBitRanges.Sort();

        for (int i = 0; i < UpdatingBitRanges.Count; i++)
        {
            for (int j = 0; j < UpdatingBitRanges.Count; j++)
            {
                if (i == j) continue;
                ulong start1 = (ulong)UpdatingBitRanges[i].Start.BitIndex;
                ulong start2 = (ulong)UpdatingBitRanges[i].End.BitIndex;
                ulong end1 = (ulong)UpdatingBitRanges[j].Start.BitIndex;
                ulong end2 = (ulong)UpdatingBitRanges[j].End.BitIndex;

                // Remove redundant ranges (ranges fully enclosed by other ranges)

                // 1: |-----|
                // 2: |-----|
                // -> |-----|
                // start1 == start2 && end1 == end2
                if (start1 == start2 && end1 == end2)
                {
                    UpdatingBitRanges.RemoveAt(j);
                    j -= 1;
                    continue;
                }
                // 1: |-----|
                // 2:   |--|
                // -> |-----|
                // start1 < start2 < end2 < end1
                if (start1 < start2 && start2 < end2 && end2 < end1)
                {
                    UpdatingBitRanges.RemoveAt(j);
                    j -= 1;
                    continue;
                }
                // 1:   |--|
                // 2: |-----|
                // -> |-----|
                // start2 < start1 < end1 < end2
                if (start2 < start1 && start1 < end1 && end1 < end2)
                {
                    UpdatingBitRanges.RemoveAt(i);
                    i -= 1;
                    break;
                }

                // Resize overlapping ranges so they are next to each other

                // 1: |-----|
                // 2:    |-----|
                // -> |-----||-|
                // start1 < start2 <= end1 < end2
                if (start1 < start2 && start2 <= end1 && end1 < end2)
                {
                    UpdatingBitRanges[j] = new BitRange(end1 + 1, end2);
                }
                // 1:    |-----|
                // 2: |-----|
                // -> |-----||-|
                // start2 < start1 <= end2 < end1
                if (start2 < start1 && start1 <= end2 && end2 < end1)
                {
                    UpdatingBitRanges[i] = new BitRange(end2 + 1, end1);
                }

                // Merge ranges that are next to each other
                // 1: |-----|
                // 2:        |--|
                // -> |---------|
                // end1 + 1 == start2
                if (end1 + 1 == start2)
                {
                    UpdatingBitRanges.RemoveAt(j);
                    UpdatingBitRanges[i] = new BitRange(start1, end2);
                    j -= 1;
                    continue;
                }
            }
        }
    }

    public void ResetUpdateRanges()
    {
        UpdatingBitRanges = [new BitRange(0ul, (ulong)Memory.Length * 8)];
    }

    public void WriteNewDataToLiveBuffer(ReadOnlySpan<byte> bytes)
    {
        foreach (BitRange updateRange in UpdatingBitRanges)
        {
            Console.WriteLine($"Range {updateRange.Start.ByteIndex:X8} to {updateRange.End.ByteIndex:X8}");
            byte[] span = new byte[updateRange.ByteLength];
            Array.Copy(bytes.ToArray(), (int)updateRange.Start.ByteIndex, span, 0, span.Length);
            span.CopyTo(Memory.Span[(int)updateRange.Start.ByteIndex..]);
        }
    }

    private void UpdateDocument(object? sender, EventArgs? e)
    {
        // TODO: For future implementation, only change the affected memory regions (only if performance improves)
        BitRange changedMemoryRange = new(0, (ulong)Memory.Length - 1);
        // var span = Memory.ToArray();
        // span.CopyTo(Memory.Span);
        foreach (BitRange updateRange in UpdatingBitRanges)
            OnChanged(new BinaryDocumentChange(BinaryDocumentChangeType.Modify, updateRange));
    }

    public void ForceReload() => UpdateDocument(this, null);
}