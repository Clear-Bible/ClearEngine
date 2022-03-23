using SIL.Machine.Corpora;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClearBible.Engine.Corpora
{
    public interface IEngineText
    {
        bool LeaveTextRaw { get; }

        /// <summary>
        /// Should never be called while iterating GetSegments()
        /// </summary>
        bool ToggleLeaveTextRawOn();


        //IEnumerable<TextSegment> GetSegments(bool includeText, bool keepRawText, IText? basedOn = null);
    }
}
