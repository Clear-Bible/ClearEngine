// Decompiled with JetBrains decompiler
// Type: Models.Symmetrizer
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;
using System.Collections.Generic;

namespace Models
{
  public class Symmetrizer : IAlignmentModel
  {
    private IAlignmentModel m_modelFwd;
    private IAlignmentModel m_modelRev;
    private SymmetrizationType m_symType;

    public Symmetrizer(
      IAlignmentModel modelFwd,
      IAlignmentModel modelRev,
      SymmetrizationType symType)
    {
      this.m_modelFwd = modelFwd;
      this.m_modelRev = modelRev;
      this.m_symType = symType;
    }

    public int MaxIterations
    {
      get
      {
        return this.m_modelFwd.MaxIterations;
      }
    }

    public bool NeedsMoreTraining
    {
      get
      {
        return this.m_modelFwd.NeedsMoreTraining || this.m_modelRev.NeedsMoreTraining;
      }
    }

    public string Phase
    {
      get
      {
        return this.m_modelFwd.Phase + "/" + this.m_modelRev.Phase;
      }
    }

    public double MaxDelta
    {
      get
      {
        return Math.Max(this.m_modelFwd.MaxDelta, this.m_modelRev.MaxDelta);
      }
    }

    public string Summary
    {
      get
      {
        return this.m_modelFwd.Summary + "/" + this.m_modelRev.Summary;
      }
    }

    public bool HasNullSourceWord
    {
      get
      {
        return false;
      }
    }

    public double T(int e, int f)
    {
      return Math.Max(this.m_modelFwd.T(e, f), this.m_modelRev.T(f, e));
    }

    public double T_NULL(int f)
    {
      return -1.0;
    }

    public virtual bool ConfigureRun(List<ModelSpec> runList)
    {
      return this.m_modelFwd.ConfigureRun(runList) && this.m_modelRev.ConfigureRun(runList);
    }

    public void AddSegmentPair(int[] source, int[] target, int[] fixedAlignments)
    {
      try
      {
        int[] fixedAlignments1 = new int[source.Length];
        for (int index = 0; index < source.Length; ++index)
          fixedAlignments1[index] = -1;
        for (int index = 0; index < target.Length; ++index)
        {
          if (fixedAlignments[index] != -1)
            fixedAlignments1[fixedAlignments[index]] = index;
        }
        this.m_modelFwd.AddSegmentPair(source, target, fixedAlignments);
        this.m_modelRev.AddSegmentPair(target, source, fixedAlignments1);
      }
      catch
      {
      }
    }

    public void EndPass()
    {
      this.m_modelFwd.EndPass();
      this.m_modelRev.EndPass();
    }

    public List<Alignment> GetAlignments(
      int[] source,
      int[] target,
      int[] fixedAlignments)
    {
      int[] fixedAlignments1 = new int[source.Length];
      for (int index = 0; index < source.Length; ++index)
        fixedAlignments1[index] = -1;
      for (int index = 0; index < target.Length; ++index)
      {
        if (fixedAlignments[index] != -1)
          fixedAlignments1[fixedAlignments[index]] = index;
      }
      List<Alignment> alignments1 = this.m_modelFwd.GetAlignments(source, target, fixedAlignments);
      List<Alignment> alignments2 = this.m_modelRev.GetAlignments(target, source, fixedAlignments1);
      List<Alignment> alignmentList = new List<Alignment>();
      if (this.m_symType == SymmetrizationType.Min)
      {
        foreach (Alignment alignment1 in alignments1)
        {
          foreach (Alignment alignment2 in alignments2)
          {
            if (alignment1.Source == alignment2.Target && alignment2.Source == alignment1.Target)
            {
              alignmentList.Add(new Alignment(alignment1.Source, alignment1.Target, Math.Min(alignment1.WordProb, alignment2.WordProb), Math.Min(alignment1.AlignProb, alignment2.AlignProb)));
              break;
            }
          }
        }
      }
      else
      {
        Tabulator[][] tabulatorArray = new Tabulator[target.Length][];
        for (int index1 = 0; index1 < target.Length; ++index1)
        {
          tabulatorArray[index1] = new Tabulator[source.Length];
          for (int index2 = 0; index2 < source.Length; ++index2)
            tabulatorArray[index1][index2] = new Tabulator(this.m_symType == SymmetrizationType.Max);
        }
        foreach (Alignment alignment in alignments1)
          tabulatorArray[alignment.Target][alignment.Source].AddToUnion(alignment.WordProb, alignment.AlignProb);
        foreach (Alignment alignment in alignments2)
          tabulatorArray[alignment.Source][alignment.Target].AddToUnion(alignment.WordProb, alignment.AlignProb);
        if (this.m_symType == SymmetrizationType.Diag)
        {
          foreach (Alignment alignment1 in alignments1)
          {
            foreach (Alignment alignment2 in alignments2)
            {
              if (alignment1.Source == alignment2.Target && alignment2.Source == alignment1.Target)
              {
                tabulatorArray[alignment1.Target][alignment1.Source].MarkInSet();
                break;
              }
            }
          }
          bool flag1;
          do
          {
            flag1 = false;
            for (int j = 0; j < target.Length; ++j)
            {
              for (int i = 0; i < source.Length; ++i)
              {
                if (tabulatorArray[j][i].InSet)
                {
                  foreach (Pair neighbor in Symmetrizer.Neighbors(j, i, target.Length, source.Length))
                  {
                    if (tabulatorArray[neighbor.e][neighbor.f].InUnion)
                    {
                      bool flag2 = false;
                      for (int index = 0; index < source.Length; ++index)
                      {
                        if (tabulatorArray[neighbor.e][index].InSet)
                          flag2 = true;
                      }
                      if (flag2)
                      {
                        flag2 = false;
                        for (int index = 0; index < target.Length; ++index)
                        {
                          if (tabulatorArray[index][neighbor.f].InSet)
                            flag2 = true;
                        }
                      }
                      if (!flag2)
                      {
                        flag1 = true;
                        tabulatorArray[neighbor.e][neighbor.f].MarkInSet();
                      }
                    }
                  }
                }
              }
            }
          }
          while (flag1);
        }
        for (int target1 = 0; target1 < target.Length; ++target1)
        {
          for (int source1 = 0; source1 < source.Length; ++source1)
          {
            if (tabulatorArray[target1][source1].InSet)
              alignmentList.Add(new Alignment(source1, target1, tabulatorArray[target1][source1].WordProb, tabulatorArray[target1][source1].AlignProb));
          }
        }
      }
      return alignmentList;
    }

    private static IEnumerable<Pair> Neighbors(int j, int i, int jMax, int iMax)
    {
      if (j > 0)
      {
        if (i > 0)
          yield return new Pair(j - 1, i - 1);
        yield return new Pair(j - 1, i);
        if (i < iMax - 1)
          yield return new Pair(j - 1, i + 1);
      }
      if (i > 0)
        yield return new Pair(j, i - 1);
      if (i < iMax - 1)
        yield return new Pair(j, i + 1);
      if (j < jMax - 1)
      {
        if (i > 0)
          yield return new Pair(j + 1, i - 1);
        yield return new Pair(j + 1, i);
        if (i < iMax - 1)
          yield return new Pair(j + 1, i + 1);
      }
    }
  }
}
