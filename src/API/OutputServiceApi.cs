using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface IOutputService
    {
        Line GetLine(
            ZoneMultiAlignment zoneMultiAlignment,
            Dictionary<string, Gloss> glossTable,
            Dictionary<string, int> primaryPositions);
    }
}
