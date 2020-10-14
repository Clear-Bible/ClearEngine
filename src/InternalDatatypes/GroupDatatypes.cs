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
        : Dictionary<string, List<TargetGroup>>
    {

    }
}
