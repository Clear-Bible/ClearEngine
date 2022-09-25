using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Tokenization
{
    public class SetTrainingBySurfaceLowercase : SetTrainingBySurfaceTokensTextRowProcessor
    {
        protected override string GetTrainingText(string surfaceText)
        {
            return surfaceText.ToLowerInvariant();
        }
    }
}
