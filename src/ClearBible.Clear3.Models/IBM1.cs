// Decompiled with JetBrains decompiler
// Type: Models.IBM1
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Models
{
  public class IBM1 : IAlignmentModel
  {
    protected double[][] m_t;
    protected double[][] m_ctEF;
    protected double[] m_ctF;
    protected int m_sourceWordCount;
    protected int m_targetWordCount;
    protected double m_delta;
    protected bool m_needsMoreTraining;
    protected int m_citerModel1;
    protected int m_citerModel1Max;
    private double[][] m_tPrev;

    public IBM1(int sourceWordCount, int targetWordCount)
    {
      this.m_sourceWordCount = sourceWordCount + 1;
      this.m_targetWordCount = targetWordCount;
      this.m_t = new double[this.m_targetWordCount][];
      this.m_tPrev = new double[this.m_targetWordCount][];
      this.m_ctEF = new double[this.m_targetWordCount][];
      for (int index1 = 0; index1 < this.m_targetWordCount; ++index1)
      {
        this.m_t[index1] = new double[this.m_sourceWordCount];
        this.m_tPrev[index1] = new double[this.m_sourceWordCount];
        for (int index2 = 0; index2 < this.m_sourceWordCount; ++index2)
          this.m_t[index1][index2] = 1.0;
        this.m_ctEF[index1] = new double[this.m_sourceWordCount];
      }
      this.m_ctF = new double[this.m_sourceWordCount];
      this.m_needsMoreTraining = true;
      this.m_citerModel1 = 0;
      this.m_delta = 0.0;
    }

    public int SourceWordCount
    {
      get
      {
        return this.m_sourceWordCount;
      }
    }

    public int TargetWordCount
    {
      get
      {
        return this.m_targetWordCount;
      }
    }

    public virtual int MaxIterations
    {
      get
      {
        return this.m_citerModel1Max;
      }
    }

    public bool NeedsMoreTraining
    {
      get
      {
        return this.m_needsMoreTraining;
      }
    }

    public virtual string Phase
    {
      get
      {
        return "IBM Model 1";
      }
    }

    public double MaxDelta
    {
      get
      {
        return this.m_delta;
      }
    }

    public virtual string Summary
    {
      get
      {
        return this.m_citerModel1 > 1 ? string.Format("Iterations: Model1={0}; MaxDelta: {1}", (object) this.m_citerModel1, (object) this.m_delta) : string.Format("Iterations: Model1={0}", (object) this.m_citerModel1);
      }
    }

    public bool HasNullSourceWord
    {
      get
      {
        return true;
      }
    }

    public double T(int e, int f)
    {
      return this.m_t[f][e + 1];
    }

    public double T_NULL(int f)
    {
      return this.m_t[f][0];
    }

    public virtual bool ConfigureRun(List<ModelSpec> runList)
    {
      if (runList.Count != 1)
        return false;
      Debug.Assert(runList[0].Model == Model.Model1);
      this.m_citerModel1Max = runList[0].RunCount;
      return true;
    }

    public virtual void AddSegmentPair(int[] source, int[] target, int[] fixedAlignments)
    {
      for (int index1 = 0; index1 < target.Length; ++index1)
      {
        if (fixedAlignments[index1] != -1)
        {
          ++this.m_ctEF[target[index1]][source[fixedAlignments[index1]] + 1];
          ++this.m_ctF[source[fixedAlignments[index1]] + 1];
        }
        else
        {
          double num1 = this.m_t[target[index1]][0];
          for (int index2 = 0; index2 < source.Length; ++index2)
            num1 += this.m_t[target[index1]][source[index2] + 1];
          double num2 = this.m_t[target[index1]][0] / num1;
          this.m_ctEF[target[index1]][0] += num2;
          this.m_ctF[0] += num2;
          for (int index2 = 0; index2 < source.Length; ++index2)
          {
            double num3 = this.m_t[target[index1]][source[index2] + 1] / num1;
            this.m_ctEF[target[index1]][source[index2] + 1] += num3;
            this.m_ctF[source[index2] + 1] += num3;
          }
        }
      }
    }

    public virtual void EndPass()
    {
      ++this.m_citerModel1;
      this.EstimateModel1Probabilities();
      this.m_delta = 0.0;
      if (this.m_citerModel1 > 1)
        this.CalculateModel1Delta();
      this.m_needsMoreTraining = this.m_citerModel1 < this.m_citerModel1Max;
      if (!this.m_needsMoreTraining)
        return;
      this.SaveCurrentModel1();
      this.ZeroModel1Counts();
    }

    public virtual List<Alignment> GetAlignments(
      int[] source,
      int[] target,
      int[] fixedAlignments)
    {
      List<Alignment> alignmentList = new List<Alignment>();
      for (int target1 = 0; target1 < target.Length; ++target1)
      {
        int source1;
        double wordProb;
        if (fixedAlignments[target1] != -1)
        {
          source1 = fixedAlignments[target1];
          wordProb = 1.0;
        }
        else
        {
          source1 = -1;
          wordProb = this.m_t[target[target1]][0];
          for (int index = 0; index < source.Length; ++index)
          {
            double num = this.m_t[target[target1]][source[index] + 1];
            if (num > wordProb)
            {
              wordProb = num;
              source1 = index;
            }
            else if (num == wordProb)
              source1 = -1;
          }
        }
        if (source1 != -1)
          alignmentList.Add(new Alignment(source1, target1, wordProb));
      }
      return alignmentList;
    }

    protected void EstimateModel1Probabilities()
    {
      for (int index1 = 0; index1 < this.m_sourceWordCount; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetWordCount; ++index2)
          this.m_t[index2][index1] = this.m_ctF[index1] == 0.0 ? 0.0 : this.m_ctEF[index2][index1] / this.m_ctF[index1];
      }
    }

    protected void CalculateModel1Delta()
    {
      for (int index1 = 0; index1 < this.m_sourceWordCount; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetWordCount; ++index2)
        {
          double num = Math.Abs(this.m_t[index2][index1] - this.m_tPrev[index2][index1]);
          if (num > this.m_delta)
            this.m_delta = num;
        }
      }
    }

    protected void SaveCurrentModel1()
    {
      for (int index1 = 0; index1 < this.m_sourceWordCount; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetWordCount; ++index2)
          this.m_tPrev[index2][index1] = this.m_t[index2][index1];
      }
    }

    protected void ZeroModel1Counts()
    {
      for (int index1 = 0; index1 < this.m_targetWordCount; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceWordCount; ++index2)
          this.m_ctEF[index1][index2] = 0.0;
      }
      for (int index = 0; index < this.m_sourceWordCount; ++index)
        this.m_ctF[index] = 0.0;
    }
  }
}
