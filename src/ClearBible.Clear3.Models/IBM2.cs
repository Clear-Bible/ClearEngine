// Decompiled with JetBrains decompiler
// Type: Models.IBM2
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Models
{
  public class IBM2 : IBM1
  {
    protected double[][][][] m_a;
    protected double[][][][] m_caIJLM;
    protected double[][][] m_caJLM;
    private double[][][][] m_aPrev;
    protected int m_sourceMaxLength;
    protected int m_targetMaxLength;
    protected bool m_fTrainWithModel1;
    protected int m_citerModel2;
    protected int m_citerModel2Max;

    public IBM2(
      int sourceWordCount,
      int targetWordCount,
      int sourceMaxLength,
      int targetMaxLength)
      : base(sourceWordCount, targetWordCount)
    {
      this.m_sourceMaxLength = sourceMaxLength + 1;
      this.m_targetMaxLength = targetMaxLength;
      this.m_a = new double[this.m_sourceMaxLength][][][];
      this.m_aPrev = new double[this.m_sourceMaxLength][][][];
      this.m_caIJLM = new double[this.m_sourceMaxLength][][][];
      for (int index1 = 0; index1 < this.m_sourceMaxLength; ++index1)
      {
        this.m_a[index1] = new double[this.m_targetMaxLength][][];
        this.m_aPrev[index1] = new double[this.m_targetMaxLength][][];
        this.m_caIJLM[index1] = new double[this.m_targetMaxLength][][];
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
        {
          this.m_a[index1][index2] = new double[this.m_targetMaxLength][];
          this.m_aPrev[index1][index2] = new double[this.m_targetMaxLength][];
          this.m_caIJLM[index1][index2] = new double[this.m_targetMaxLength][];
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            this.m_a[index1][index2][index3] = new double[this.m_sourceMaxLength - 1];
            this.m_aPrev[index1][index2][index3] = new double[this.m_sourceMaxLength - 1];
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
              this.m_a[index1][index2][index3][index4] = 1.0;
            this.m_caIJLM[index1][index2][index3] = new double[this.m_sourceMaxLength - 1];
          }
        }
      }
      this.m_caJLM = new double[this.m_targetMaxLength][][];
      for (int index1 = 0; index1 < this.m_targetMaxLength; ++index1)
      {
        this.m_caJLM[index1] = new double[this.m_targetMaxLength][];
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
          this.m_caJLM[index1][index2] = new double[this.m_sourceMaxLength - 1];
      }
      this.m_citerModel2 = 0;
    }

    public override int MaxIterations
    {
      get
      {
        return this.m_citerModel1Max + this.m_citerModel2Max;
      }
    }

    public override string Phase
    {
      get
      {
        return this.m_fTrainWithModel1 ? "IBM Model 1" : "IBM Model 2";
      }
    }

    public override string Summary
    {
      get
      {
        return string.Format("Iterations: Model1={0} Model2={1}; MaxDelta: {2}", (object) this.m_citerModel1, (object) this.m_citerModel2, (object) this.m_delta);
      }
    }

    public override bool ConfigureRun(List<ModelSpec> runList)
    {
      if (runList.Count != 2 || (uint) runList[1].Model > 0U)
        return false;
      Debug.Assert(runList[0].Model == Model.Model2);
      this.m_citerModel2Max = runList[0].RunCount;
      this.m_citerModel1Max = runList[1].RunCount;
      this.m_fTrainWithModel1 = true;
      return true;
    }

    public override void AddSegmentPair(int[] source, int[] target, int[] fixedAlignments)
    {
      if (this.m_fTrainWithModel1)
      {
        base.AddSegmentPair(source, target, fixedAlignments);
      }
      else
      {
        for (int index1 = 0; index1 < target.Length; ++index1)
        {
          if (fixedAlignments[index1] != -1)
          {
            ++this.m_ctEF[target[index1]][source[fixedAlignments[index1]] + 1];
            ++this.m_ctF[source[fixedAlignments[index1]] + 1];
            ++this.m_caIJLM[fixedAlignments[index1] + 1][index1][target.Length - 1][source.Length - 1];
            ++this.m_caJLM[index1][target.Length - 1][source.Length - 1];
          }
          else
          {
            double num1 = this.m_a[0][index1][target.Length - 1][source.Length - 1] * this.m_t[target[index1]][0];
            for (int index2 = 0; index2 < source.Length; ++index2)
              num1 += this.m_a[index2 + 1][index1][target.Length - 1][source.Length - 1] * this.m_t[target[index1]][source[index2] + 1];
            double num2 = this.m_a[0][index1][target.Length - 1][source.Length - 1] * this.m_t[target[index1]][0] / num1;
            this.m_ctEF[target[index1]][0] += num2;
            this.m_ctF[0] += num2;
            this.m_caIJLM[0][index1][target.Length - 1][source.Length - 1] += num2;
            this.m_caJLM[index1][target.Length - 1][source.Length - 1] += num2;
            for (int index2 = 0; index2 < source.Length; ++index2)
            {
              double num3 = this.m_a[index2 + 1][index1][target.Length - 1][source.Length - 1] * this.m_t[target[index1]][source[index2] + 1] / num1;
              this.m_ctEF[target[index1]][source[index2] + 1] += num3;
              this.m_ctF[source[index2] + 1] += num3;
              this.m_caIJLM[index2 + 1][index1][target.Length - 1][source.Length - 1] += num3;
              this.m_caJLM[index1][target.Length - 1][source.Length - 1] += num3;
            }
          }
        }
      }
    }

    public override void EndPass()
    {
      if (this.m_fTrainWithModel1)
      {
        base.EndPass();
        if (this.m_needsMoreTraining)
          return;
        this.m_needsMoreTraining = true;
        this.m_fTrainWithModel1 = false;
      }
      else
      {
        ++this.m_citerModel2;
        this.EstimateModel2Probabilities();
        this.m_delta = 0.0;
        if (this.m_citerModel2 == 1)
          this.CalculateModel1Delta();
        else
          this.CalculateModel2Delta();
        this.m_needsMoreTraining = this.m_citerModel2 < this.m_citerModel2Max;
        if (this.m_needsMoreTraining)
        {
          this.SaveCurrentModel2();
          this.ZeroModel2Counts();
        }
      }
    }

    public override List<Alignment> GetAlignments(
      int[] source,
      int[] target,
      int[] fixedAlignments)
    {
      List<Alignment> alignmentList = new List<Alignment>();
      for (int target1 = 0; target1 < target.Length; ++target1)
      {
        int source1;
        if (fixedAlignments[target1] != -1)
        {
          source1 = fixedAlignments[target1];
        }
        else
        {
          source1 = -1;
          double num1 = this.m_a[0][target1][target.Length - 1][source.Length - 1] * this.m_t[target[target1]][0];
          for (int index = 0; index < source.Length; ++index)
          {
            double num2 = this.m_a[index + 1][target1][target.Length - 1][source.Length - 1] * this.m_t[target[target1]][source[index] + 1];
            if (num2 > num1)
            {
              num1 = num2;
              source1 = index;
            }
            else if (num2 == num1)
              source1 = -1;
          }
        }
        if (source1 != -1)
          alignmentList.Add(new Alignment(source1, target1, this.m_t[target[target1]][source[source1] + 1], this.m_a[source1 + 1][target1][target.Length - 1][source.Length - 1]));
      }
      return alignmentList;
    }

    protected void EstimateModel2Probabilities()
    {
      this.EstimateModel1Probabilities();
      for (int index1 = 0; index1 < this.m_sourceMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
              this.m_a[index1][index2][index3][index4] = this.m_caJLM[index2][index3][index4] == 0.0 ? 0.0 : this.m_caIJLM[index1][index2][index3][index4] / this.m_caJLM[index2][index3][index4];
          }
        }
      }
    }

    protected void CalculateModel2Delta()
    {
      this.CalculateModel1Delta();
      for (int index1 = 0; index1 < this.m_sourceMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
            {
              double num = Math.Abs(this.m_a[index1][index2][index3][index4] - this.m_aPrev[index1][index2][index3][index4]);
              if (num > this.m_delta)
                this.m_delta = num;
            }
          }
        }
      }
    }

    protected void SaveCurrentModel2()
    {
      this.SaveCurrentModel1();
      for (int index1 = 0; index1 < this.m_sourceMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
              this.m_aPrev[index1][index2][index3][index4] = this.m_a[index1][index2][index3][index4];
          }
        }
      }
    }

    protected void ZeroModel2Counts()
    {
      this.ZeroModel1Counts();
      for (int index1 = 0; index1 < this.m_sourceMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
              this.m_caIJLM[index1][index2][index3][index4] = 0.0;
          }
        }
      }
      for (int index1 = 0; index1 < this.m_targetMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_sourceMaxLength - 1; ++index3)
            this.m_caJLM[index1][index2][index3] = 0.0;
        }
      }
    }
  }
}
