using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;



namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Miscellaneous;

    public class AutoAlignUtility
    {
 


 





        /// <summary>
        /// Create a MaybeTargetPoint that does not really have a
        /// target point inside of it.
        /// </summary>
        /// 
        public static MaybeTargetPoint CreateFakeTargetWord()
        {
            return new MaybeTargetPoint(TargetPoint: null);
        }
    }
}
