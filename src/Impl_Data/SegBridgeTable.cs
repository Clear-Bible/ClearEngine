using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Data
{
    public class SegBridgeEntry
    {
        public readonly string SourceID;
        public readonly string TargetID;
        public readonly double Score;

        public SegBridgeEntry(string sourceID, string targetID, double score)
        {
            SourceID = sourceID;
            TargetID = targetID;
            Score = score;
        }
    }


    public class SegBridgeTable
    {
        private Dictionary<string, SegBridgeEntry> _bySourceId;
        private Dictionary<string, SegBridgeEntry> _byTargetId;

        public SegBridgeTable()
        {
            _bySourceId = new Dictionary<string, SegBridgeEntry>();
            _byTargetId = new Dictionary<string, SegBridgeEntry>();
        }

        public IEnumerable<string> SourceIDs => _bySourceId.Keys;

        public IEnumerable<string> TargetIDs => _byTargetId.Keys;

        public IEnumerable<SegBridgeEntry> AllEntries =>
            _bySourceId.Values;

        public SegBridgeEntry GetEntryForSource(string sourceID) =>
            _bySourceId[sourceID];

        public SegBridgeEntry GetEntryForTarget(string targetID) =>
            _byTargetId[targetID];

        public void AddEntry(string sourceID, string targetID, double score)
        {
            SegBridgeEntry entry =
                new SegBridgeEntry(sourceID, targetID, score);
            _bySourceId[sourceID] = entry;
            _byTargetId[targetID] = entry;
        }
    }






    public class SegSet
    {
        private HashSet<string> _segmentIDs;

        public override int GetHashCode() => _segmentIDs.GetHashCode();

        public override bool Equals(object obj) => _segmentIDs.Equals(obj);

        public SegSet(IEnumerable<string> members)
        {
            _segmentIDs = new HashSet<string>(members);
        }
    }

    public class SegSetBridgeTable
    {
        private Dictionary<SegSet, SegSet> _segBridgeTable;

        private SegSetBridgeTable()
        {
            _segBridgeTable = new Dictionary<SegSet, SegSet>();
        }
    }
}
