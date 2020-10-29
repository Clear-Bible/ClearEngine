using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface IGroupTranslationsTable
    {
        void AddEntry(
            string sourceGroupLemmas,
            string targetGroupAsText,
            int primaryPosition);
    }



    public class GroupTranslationsTable
    {
        public
            Dictionary<
                SourceLemmasAsText,
                List<Tuple<TargetGroupAsText, PrimaryPosition>>>
            Inner { get; }

        public GroupTranslationsTable(
            Dictionary<
                SourceLemmasAsText,
                List<Tuple<TargetGroupAsText, PrimaryPosition>>>
            inner)
        {
            Inner = inner;
        }
    }


    public readonly struct SourceLemmasAsText
    {
        public readonly string Text;

        public SourceLemmasAsText(string text)
        {
            Text = text;
        }
    }


    public readonly struct TargetGroupAsText
    {
        public readonly string Text;

        public TargetGroupAsText(string text)
        {
            Text = text;
        }
    }


    public readonly struct PrimaryPosition
    {
        public readonly int Int;

        public PrimaryPosition(int primaryPosition)
        {
            Int = primaryPosition;
        }
    }
}
