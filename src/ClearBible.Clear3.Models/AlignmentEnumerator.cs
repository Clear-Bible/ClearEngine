// Decompiled with JetBrains decompiler
// Type: Models.AlignmentEnumerator
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;
using System.Collections;
using System.Collections.Generic;

namespace Models
{
  public class AlignmentEnumerator : IEnumerator
  {
    private ModelBuilder m_modelBuilder;
    private int m_index;
    private IEnumerator<int[]> m_sourceEnumerator;
    private IEnumerator<int[]> m_targetEnumerator;
    private IEnumerator<Alignment[]> m_prealignEnumerator;

    public AlignmentEnumerator(ModelBuilder modelBuilder, int index)
    {
      this.m_modelBuilder = modelBuilder;
      this.m_index = index;
      this.m_sourceEnumerator = (IEnumerator<int[]>) null;
      this.m_targetEnumerator = (IEnumerator<int[]>) null;
      this.m_prealignEnumerator = (IEnumerator<Alignment[]>) null;
    }

    public bool MoveNext()
    {
      if (this.m_sourceEnumerator == null)
      {
        this.m_sourceEnumerator = this.m_modelBuilder.GetSourceSegments(this.m_index).GetEnumerator();
        this.m_targetEnumerator = this.m_modelBuilder.GetTargetSegments(this.m_index).GetEnumerator();
        if (this.m_modelBuilder.GetPrealignments(this.m_index) != null)
          this.m_prealignEnumerator = this.m_modelBuilder.GetPrealignments(this.m_index).GetEnumerator();
      }
      return this.m_prealignEnumerator == null ? this.m_sourceEnumerator.MoveNext() && this.m_targetEnumerator.MoveNext() : this.m_sourceEnumerator.MoveNext() && this.m_targetEnumerator.MoveNext() && this.m_prealignEnumerator.MoveNext();
    }

    public void Reset()
    {
      if (this.m_sourceEnumerator != null)
      {
        this.m_sourceEnumerator.Dispose();
        this.m_sourceEnumerator = (IEnumerator<int[]>) null;
      }
      if (this.m_targetEnumerator != null)
      {
        this.m_targetEnumerator.Dispose();
        this.m_targetEnumerator = (IEnumerator<int[]>) null;
      }
      if (this.m_prealignEnumerator == null)
        return;
      this.m_prealignEnumerator.Dispose();
      this.m_prealignEnumerator = (IEnumerator<Alignment[]>) null;
    }

    object IEnumerator.Current
    {
      get
      {
        return (object) this.Current;
      }
    }

    public List<Alignment> Current
    {
      get
      {
        if (this.m_sourceEnumerator == null || this.m_targetEnumerator == null)
          throw new InvalidOperationException();
        return this.m_modelBuilder.GetAlignments(this.m_sourceEnumerator.Current, this.m_targetEnumerator.Current, this.m_modelBuilder.ConvertAlignmentsToArray(this.m_sourceEnumerator.Current, this.m_targetEnumerator.Current, this.m_prealignEnumerator != null ? this.m_prealignEnumerator.Current : (Alignment[]) null));
      }
    }
  }
}
