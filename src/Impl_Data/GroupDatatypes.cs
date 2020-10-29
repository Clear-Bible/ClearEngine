using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Data
{
    public class GroupTranslation_Old
    {
        public string TargetGroupAsText;
        public int PrimaryPosition;

        public GroupTranslation_Old(
            string targetGroupAsText,
            int primaryPosition)
        {
            TargetGroupAsText = targetGroupAsText;
            PrimaryPosition = primaryPosition;
        }

        public GroupTranslation_Old() { }
    }


    public class GroupTranslations_Old
    {
        private List<GroupTranslation_Old> _inner =
            new List<GroupTranslation_Old>();

        public IEnumerable<GroupTranslation_Old> AllTranslations =>
            _inner;

        public void Add(GroupTranslation_Old targetGroupTranslation)
        {
            _inner.Add(targetGroupTranslation);
        }
    }


    public class GroupTranslationsTable_Old : IGroupTranslationsTable
    {
        private Dictionary<string, GroupTranslations_Old> _inner =
            new Dictionary<string, GroupTranslations_Old>();

        public bool ContainsSourceGroupKey(string sourceGroupLemmas) =>
            _inner.ContainsKey(sourceGroupLemmas);

        public GroupTranslations_Old TranslationsForSourceGroup(
            string sourceGroupLemmas) =>
                _inner[sourceGroupLemmas];

        public IEnumerable<KeyValuePair<string, GroupTranslations_Old>>
            AllEntries =>
                _inner;

        public void Add(
            string sourceGroupLemmas,
            GroupTranslations_Old translations)
        {
            _inner.Add(sourceGroupLemmas, translations);
        }

        public void AddEntry(
            string sourceGroupLemmas,
            string targetGroupAsText,
            int primaryPosition)
        {
            if (!_inner.TryGetValue(sourceGroupLemmas,
                out GroupTranslations_Old groupTranslations))
            {
                groupTranslations = new GroupTranslations_Old();
                _inner.Add(sourceGroupLemmas, groupTranslations);
            }
            groupTranslations.Add(new GroupTranslation_Old(
                targetGroupAsText,
                primaryPosition));
        }
    }

    // Dictionary<SourceLemmasAsText, List<Tuple<TargetGroupAsText, PrimaryPosition>>>
}
