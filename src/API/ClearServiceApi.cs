﻿using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    /// <summary>
    /// Clear 3.0 Top Level Interface
    /// </summary>
    /// 
    public interface IClear30ServiceAPI
    {
        IResourceService ResourceService { get; }

        IImportExportService ImportExportService { get; }

        SMTService SMTService { get; }

        IAutoAlignmentService AutoAlignmentService { get; }

        IOutputService OutputService { get; }
    }
}
