﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Tokenization
{
    public class SetTrainingBySurfaceLowercase : SetTrainingTokensTextRowProcessor
    {
        protected override string GetTrainingText(string surfaceText, string targetText)
        {
            return surfaceText.ToLowerInvariant();
        }
    }
}
