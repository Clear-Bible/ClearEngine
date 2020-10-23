using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClearBible.Clear3.API;

namespace ClearBible.Clear3.Impl.Data
{
    public readonly struct Lemma: ILemma,
        IEquatable<ILemma>, IComparable<ILemma>
    {
        public string Text { get; }

        public Lemma(string text) { Text = text; }

        public bool Equals(ILemma x) => Text.Equals(x.Text);

        public int CompareTo(ILemma x) => Text.CompareTo(x.Text);
    }


    public readonly struct Morph: IMorph,
        IEquatable<IMorph>, IComparable<IMorph>
    {
        public string Text { get; }

        public Morph(string text) { Text = text; }

        public bool Equals(IMorph x) => Text.Equals(x.Text);

        public int CompareTo(IMorph x) => Text.CompareTo(x.Text);
    }
}
