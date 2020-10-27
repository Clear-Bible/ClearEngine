using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Data
{
    public readonly struct Lemma_Bak: ILemma,
        IEquatable<ILemma>, IComparable<ILemma>
    {
        public string Text { get; }

        public Lemma_Bak(string text) { Text = text; }

        public bool Equals(ILemma x) => Text.Equals(x.Text);

        public int CompareTo(ILemma x) => Text.CompareTo(x.Text);
    }


    public readonly struct Morph_Bak: IMorph,
        IEquatable<IMorph>, IComparable<IMorph>
    {
        public string Text { get; }

        public Morph_Bak(string text) { Text = text; }

        public bool Equals(IMorph x) => Text.Equals(x.Text);

        public int CompareTo(IMorph x) => Text.CompareTo(x.Text);
    }
}
