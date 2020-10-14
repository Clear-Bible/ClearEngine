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

    public class GroupInfo
    {
        private Dictionary<string, List<TargetGroup>> _inner =
            new Dictionary<string, List<TargetGroup>>();

        public bool ContainsKey(string key) =>
            _inner.ContainsKey(key);

        public List<TargetGroup> this[string key] =>
            _inner[key];

        public IEnumerable<List<TargetGroup>> AllValues =>
            _inner.Values;

        public void Add(string key, List<TargetGroup> value)
        {
            _inner.Add(key, value);
        }
    }
}
