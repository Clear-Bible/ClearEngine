using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public class AlignmentModel
    {
        public Dictionary<Tuple<SourceID, TargetID>, Score> Inner { get; }

        public AlignmentModel(
            Dictionary<Tuple<SourceID, TargetID>, Score> inner)
        {
            Inner = inner;
        }
    }
}
