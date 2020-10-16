using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// Clear 3.0 Top Level Interface
    /// </summary>
    /// 
    public interface IClear30ServiceAPI
    {
        ResourceService ResourceService { get; }

        SMTService SMTService { get; }

        IAutoAlignmentService AutoAlignmentService { get; }

        IDataService Data { get; }

        IPhraseService PhraseService { get; }

        ZoneService ZoneService { get; }
    }
}
