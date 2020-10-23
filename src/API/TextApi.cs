using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClearBible.Clear3.API
{
    public interface ITextService
    {
        ILemma ILemma(string text);

        IMorph IMorph(string text);
    }


    public interface ILemma
        : IEquatable<ILemma>, IComparable<ILemma>
    {
        string Text { get; }
    }


    public interface IMorph
        : IEquatable<IMorph>, IComparable<IMorph>
    {
        string Text { get; }
    }
}
