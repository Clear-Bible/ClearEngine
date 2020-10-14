using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ClearBible.Clear3.InternalDatatypes
{
    public class GroupTranslation
    {
        public string TargetGroupAsText;
        public int PrimaryPosition;
    }


    public class GroupTranslations
    {
        private List<GroupTranslation> _inner =
            new List<GroupTranslation>();

        public IEnumerable<GroupTranslation> AllTranslations =>
            _inner;

        public void Add(GroupTranslation targetGroupTranslation)
        {
            _inner.Add(targetGroupTranslation);
        }
    }


    public class GroupTranslationsTable
    {
        private Dictionary<string, GroupTranslations> _inner =
            new Dictionary<string, GroupTranslations>();

        public bool ContainsSourceGroupKey(string sourceGroupLemmas) =>
            _inner.ContainsKey(sourceGroupLemmas);

        public GroupTranslations TranslationsForSourceGroup(
            string sourceGroupLemmas) =>
                _inner[sourceGroupLemmas];

        public IEnumerable<KeyValuePair<string, GroupTranslations>>
            AllEntries =>
                _inner;

        public void Add(
            string sourceGroupLemmas,
            GroupTranslations translations)
        {
            _inner.Add(sourceGroupLemmas, translations);
        }
    }
}
