// Decompiled with JetBrains decompiler
// Type: Models.Alignments
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System.Collections;

namespace Models
{
  public class Alignments : IEnumerable
  {
    private ModelBuilder m_modelBuilder;
    private int m_index;

    public Alignments(ModelBuilder modelBuilder, int index)
    {
      this.m_modelBuilder = modelBuilder;
      this.m_index = index;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return (IEnumerator) this.GetEnumerator();
    }

    public AlignmentEnumerator GetEnumerator()
    {
      return new AlignmentEnumerator(this.m_modelBuilder, this.m_index);
    }
  }
}
