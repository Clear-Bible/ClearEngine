using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public class CountedLinks
    {
        public Tuple<
            Dictionary<Tuple<SourceID, TargetID>, Count>,
            CountThreshold>
            Inner
        { get; }

        public CountedLinks(
            Tuple<
                Dictionary<Tuple<SourceID, TargetID>, Count>,
                CountThreshold>
            inner)
        {
            Inner = inner;
        }

        public Dictionary<Tuple<SourceID, TargetID>, Count> Dictionary =>
            Inner.Item1;

        public CountThreshold CountThreshold =>
            Inner.Item2;
    }
}
