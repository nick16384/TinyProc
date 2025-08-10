using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaHex.Document;

namespace TinyProcVisualizer.Views.Windows.Main;

public class RealTimeFixedSizeExternalSourceBinaryDocument : MemoryBinaryDocument
{
    private readonly Func<ReadOnlySpan<byte>> _backingSource;
    private readonly TimeSpan _updateInterval;
    private List<BitRange> _updatingBitRanges = [];

    public RealTimeFixedSizeExternalSourceBinaryDocument(Func<ReadOnlySpan<byte>> backingSource, TimeSpan updateInterval)
        : base(backingSource().ToArray())
    {
        ResetUpdateRanges();
        _backingSource = backingSource;
        _updateInterval = updateInterval;

        Task.Run(() =>
        {
            while (true)
            {
                Thread.Sleep(_updateInterval);
                ReadOnlySpan<byte> newData = _backingSource();
                if (newData.Length != Memory.Length)
                {
                    Console.Error.WriteLine($"Backing source length {newData.Length:N0} mismatches internal length {Memory.Length:N0}");
                    return;
                }
                WriteNewDataToLiveBuffer(newData);
            }
        });
    }

    public void AddUpdatingRange(BitRange newRange)
    {
        lock (_updatingBitRanges)
        {
            _updatingBitRanges.Add(newRange);
            CleanupUpdateRanges();
        }
    }

    public void ResetUpdateRanges()
    {
        lock (_updatingBitRanges)
        { _updatingBitRanges = [new BitRange(0ul, (ulong)Memory.Length - 1)]; }
    }

    // Locked bit range are bit ranges that do not get updated
    public List<BitRange> GetLockedBitRanges()
    {
        lock (_updatingBitRanges)
        {
            List<BitRange> lockedBitRanges = [];
            for (int i = 1; i < _updatingBitRanges.Count; i++)
            {
                ulong endPrev = _updatingBitRanges[i - 1].End.ByteIndex;
                ulong start = _updatingBitRanges[i].Start.ByteIndex;

                if (endPrev + 1 < start)
                    lockedBitRanges.Add(new BitRange(endPrev + 1, start - 1));
            }
            if (_updatingBitRanges.Last().End.ByteIndex + 1 < (ulong)Memory.Length)
                lockedBitRanges.Add(new BitRange(_updatingBitRanges.Last().End.ByteIndex + 1, (ulong)Memory.Length - 1));
            return lockedBitRanges;
        }
    }

    // Locked bit range are bit ranges that do not get updated
    public void AddLockedRange(BitRange newLockedRange)
    {
        lock (_updatingBitRanges)
        {
            List<BitRange> newUpdatingBitRanges = [];

            ulong startLocked = newLockedRange.Start.ByteIndex;
            ulong endLocked = newLockedRange.End.ByteIndex;

            foreach (BitRange updatingRange in _updatingBitRanges)
            {
                ulong start = updatingRange.Start.ByteIndex;
                ulong end = updatingRange.End.ByteIndex;

                // Cut out at the end or inside updating ranges
                // start < startLocked < end
                if (start < startLocked && startLocked <= end)
                {
                    newUpdatingBitRanges.Add(new BitRange(start, startLocked - 1));
                    // start < startLocked < endLocked < end
                    if (endLocked < end)
                        newUpdatingBitRanges.Add(new BitRange(endLocked + 1, end));
                }

                // Cut out the beginning of updating ranges
                // startLocked <= start < endLocked
                else if (startLocked <= start && start < endLocked)
                {
                    // If end <= endLocked, do nothing, since the updating range is fully covered by the locked range
                    if (endLocked < end)
                        newUpdatingBitRanges.Add(new BitRange(endLocked + 1, end));
                }
                else
                {
                    // Completely unaffected updating range
                    newUpdatingBitRanges.Add(updatingRange);
                }
            }

            _updatingBitRanges = newUpdatingBitRanges;
            CleanupUpdateRanges();
        }
    }

    // Sort, remove redundant, and merge ranges next to each other
    private void CleanupUpdateRanges()
    {
        // Sort the list of ranges
        _updatingBitRanges.Sort((a, b) =>
        {
            int difference = (int)((long)a.Start.ByteIndex - (long)b.Start.ByteIndex);
            return difference switch
            {
                var _ when difference <  0 => -1, // A < B
                var _ when difference == 0 => 0,  // A == B
                var _ when difference >  0 => 1   // A > B
            };
        });

        for (int i = 0; i < _updatingBitRanges.Count; i++)
        {
            for (int j = 0; j < _updatingBitRanges.Count; j++)
            {
                if (i == j) continue;
                ulong start1 = _updatingBitRanges[i].Start.ByteIndex;
                ulong start2 = _updatingBitRanges[i].End.ByteIndex;
                ulong end1 = _updatingBitRanges[j].Start.ByteIndex;
                ulong end2 = _updatingBitRanges[j].End.ByteIndex;

                // Remove redundant ranges (ranges fully enclosed by other ranges)

                // 1: |-----|
                // 2: |-----|
                // -> |-----|
                // start1 == start2 && end1 == end2
                if (start1 == start2 && end1 == end2)
                {
                    _updatingBitRanges.RemoveAt(j);
                    j -= 1;
                    continue;
                }
                // 1: |-----|
                // 2:   |--|
                // -> |-----|
                // start1 < start2 < end2 < end1
                if (start1 < start2 && start2 < end2 && end2 < end1)
                {
                    _updatingBitRanges.RemoveAt(j);
                    j -= 1;
                    continue;
                }
                // 1:   |--|
                // 2: |-----|
                // -> |-----|
                // start2 < start1 < end1 < end2
                if (start2 < start1 && start1 < end1 && end1 < end2)
                {
                    _updatingBitRanges.RemoveAt(i);
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
                    _updatingBitRanges[j] = new BitRange(end1 + 1, end2);
                }
                // 1:    |-----|
                // 2: |-----|
                // -> |-----||-|
                // start2 < start1 <= end2 < end1
                if (start2 < start1 && start1 <= end2 && end2 < end1)
                {
                    _updatingBitRanges[i] = new BitRange(end2 + 1, end1);
                }

                // Merge ranges that are next to each other
                // 1: |-----|
                // 2:        |--|
                // -> |---------|
                // end1 + 1 == start2
                if (end1 + 1 == start2)
                {
                    _updatingBitRanges.RemoveAt(j);
                    _updatingBitRanges[i] = new BitRange(start1, end2);
                    j -= 1;
                    continue;
                }
            }
        }
    }

    public void WriteNewDataToLiveBuffer(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != Memory.Length)
            throw new ArgumentOutOfRangeException(
                $"Cannot override hexview data: New size {bytes.Length} does not match internal size {Memory.Length}");
        lock (_updatingBitRanges)
        {
            foreach (BitRange updateRange in _updatingBitRanges)
            {
                // Create a temporary buffer, which contains the new updated data for this update range
                byte[] updateRangeNewBytes = new byte[updateRange.ByteLength];
                // Fill the buffer with the new data (update range section only)
                Array.Copy(bytes.ToArray(), (int)updateRange.Start.ByteIndex, updateRangeNewBytes, 0, updateRangeNewBytes.Length);
                // Copy the new updated section to the update range on the internal memory
                updateRangeNewBytes.CopyTo(Memory.Span[(int)updateRange.Start.ByteIndex..]);
            }
        }
    }
}