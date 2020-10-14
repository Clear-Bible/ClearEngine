using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ClearBible.Clear3.InternalDatatypes
{
    public class TargetGroup
    {
        public string Text;
        public int PrimaryPosition;
    }


    public class TargetGroups
    {
        private List<TargetGroup> _inner =
            new List<TargetGroup>();

        public IEnumerable<TargetGroup> AllMembers =>
            _inner;

        public void Add(TargetGroup targetGroup)
        {
            _inner.Add(targetGroup);
        }
    }


    public class GroupInfo
    {
        private Dictionary<string, TargetGroups> _inner =
            new Dictionary<string, TargetGroups>();

        public bool ContainsKey(string key) =>
            _inner.ContainsKey(key);

        public TargetGroups this[string key] =>
            _inner[key];

        public IEnumerable<TargetGroups> AllValues =>
            _inner.Values;

        public void Add(string key, TargetGroups value)
        {
            _inner.Add(key, value);
        }
    }
}
