// Decompiled with JetBrains decompiler
// Type: Models.HMM
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Models
{
  public class HMM : IBM2
  {
    private const double p0 = 0.2;
    private double[][] m_pi;
    private double[] m_align;
    private double[][] m_alpha;
    private double[][] m_beta;
    private double[][] m_gamma;
    private double[][][] m_xi;
    private double[] m_alphaTotal;
    private int[][] m_backPointer;
    private double[][] m_logProbability;
    private double[][] m_piPrev;
    private double[] m_alignPrev;
    private double[][] m_cPi;
    private double[] m_cAlign;
    protected bool m_fTrainWithModel2;
    protected int m_citerHMM;
    protected int m_citerHMMMax;

    public HMM(int sourceWordCount, int targetWordCount, int sourceMaxLength, int targetMaxLength)
      : base(sourceWordCount, targetWordCount, sourceMaxLength, targetMaxLength)
    {
      this.m_pi = new double[2 * this.m_sourceMaxLength][];
      this.m_piPrev = new double[2 * this.m_sourceMaxLength][];
      this.m_cPi = new double[2 * this.m_sourceMaxLength][];
      for (int index1 = 0; index1 < 2 * this.m_sourceMaxLength; ++index1)
      {
        this.m_pi[index1] = new double[this.m_sourceMaxLength - 1];
        for (int index2 = 0; index2 < this.m_sourceMaxLength - 1; ++index2)
        {
          if (index1 <= index2)
            this.m_pi[index1][index2] = 1.0 / (double) (index2 + 2);
        }
        this.m_piPrev[index1] = new double[this.m_sourceMaxLength - 1];
        this.m_cPi[index1] = new double[this.m_sourceMaxLength - 1];
      }
      this.m_align = new double[2 * this.m_sourceMaxLength - 1];
      this.m_alignPrev = new double[2 * this.m_sourceMaxLength - 1];
      this.m_cAlign = new double[2 * this.m_sourceMaxLength - 1];
      for (int index = 0; index < 2 * this.m_sourceMaxLength - 1; ++index)
        this.m_align[index] = 0.8 / (double) (2 * this.m_sourceMaxLength - 1);
      this.m_alpha = new double[this.m_targetMaxLength][];
      this.m_beta = new double[this.m_targetMaxLength][];
      this.m_gamma = new double[this.m_targetMaxLength][];
      this.m_xi = new double[this.m_targetMaxLength - 1][][];
      this.m_alphaTotal = new double[this.m_targetMaxLength];
      this.m_backPointer = new int[this.m_targetMaxLength][];
      this.m_logProbability = new double[this.m_targetMaxLength][];
      for (int index1 = 0; index1 < this.m_targetMaxLength; ++index1)
      {
        this.m_alpha[index1] = new double[2 * this.m_sourceMaxLength];
        this.m_beta[index1] = new double[2 * this.m_sourceMaxLength];
        this.m_gamma[index1] = new double[2 * this.m_sourceMaxLength];
        if (index1 < this.m_targetMaxLength - 1)
        {
          this.m_xi[index1] = new double[2 * this.m_sourceMaxLength][];
          for (int index2 = 0; index2 < 2 * this.m_sourceMaxLength; ++index2)
            this.m_xi[index1][index2] = new double[2 * this.m_sourceMaxLength];
        }
        this.m_backPointer[index1] = new int[2 * this.m_sourceMaxLength];
        this.m_logProbability[index1] = new double[2 * this.m_sourceMaxLength];
      }
      this.m_citerHMM = 0;
    }

    public override int MaxIterations
    {
      get
      {
        return this.m_citerModel1Max + this.m_citerModel2Max + this.m_citerHMMMax;
      }
    }

    public override string Phase
    {
      get
      {
        if (this.m_fTrainWithModel1)
          return "IBM Model 1";
        return this.m_fTrainWithModel2 ? "IBM Model 2" : nameof (HMM);
      }
    }

    public override string Summary
    {
      get
      {
        if (this.m_citerModel2Max == 0)
          return string.Format("Iterations: Model1={0} HMM={1}; MaxDelta: {2}", (object) this.m_citerModel1, (object) this.m_citerHMM, (object) this.m_delta);
        return string.Format("Iterations: Model1={0} Model2={1} HMM={2}; MaxDelta: {3}", (object) this.m_citerModel1, (object) this.m_citerModel2, (object) this.m_citerHMM, (object) this.m_delta);
      }
    }

    public override bool ConfigureRun(List<ModelSpec> runList)
    {
      if (runList.Count == 2 && runList[1].Model == Model.Model1)
      {
        this.m_citerModel2Max = 0;
        this.m_citerModel1Max = runList[1].RunCount;
      }
      else
      {
        if (runList.Count != 3 || runList[1].Model != Model.Model2 || runList[2].Model != Model.Model1)
          return false;
        this.m_citerModel2Max = runList[1].RunCount;
        this.m_citerModel1Max = runList[2].RunCount;
      }
      Debug.Assert(runList[0].Model == Model.HMM);
      this.m_citerHMMMax = runList[0].RunCount;
      this.m_fTrainWithModel1 = true;
      return true;
    }

    public override void AddSegmentPair(int[] source, int[] target, int[] fixedAlignments)
    {
      if (this.m_fTrainWithModel1 || this.m_fTrainWithModel2)
      {
        base.AddSegmentPair(source, target, fixedAlignments);
      }
      else
      {
        for (int index1 = 0; index1 < target.Length; ++index1)
        {
          this.m_alphaTotal[index1] = 0.0;
          for (int j = 0; j < 2 * source.Length; ++j)
          {
            this.m_alpha[index1][j] = 0.0;
            if (fixedAlignments[index1] != -1)
            {
              if (fixedAlignments[index1] == j)
              {
                if (index1 == 0)
                {
                  this.m_alpha[index1][j] = 1.0;
                }
                else
                {
                  for (int index2 = 0; index2 < 2 * source.Length; ++index2)
                    this.m_alpha[index1][j] += this.m_alpha[index1 - 1][index2];
                }
              }
            }
            else
            {
              if (index1 == 0)
              {
                this.m_alpha[index1][j] = this.m_pi[j][source.Length - 1];
              }
              else
              {
                for (int i = 0; i < 2 * source.Length; ++i)
                  this.m_alpha[index1][j] += this.m_alpha[index1 - 1][i] * this.Align(i, j, source.Length);
              }
              int index2 = j < source.Length ? source[j] + 1 : 0;
              this.m_alpha[index1][j] *= this.m_t[target[index1]][index2];
            }
            this.m_alphaTotal[index1] += this.m_alpha[index1][j];
          }
          if (this.m_alphaTotal[index1] != 0.0)
          {
            for (int index2 = 0; index2 < 2 * source.Length; ++index2)
              this.m_alpha[index1][index2] /= this.m_alphaTotal[index1];
          }
        }
        for (int index1 = target.Length - 1; index1 >= 0; --index1)
        {
          for (int i = 0; i < 2 * source.Length; ++i)
          {
            this.m_beta[index1][i] = 0.0;
            if (fixedAlignments[index1] != -1)
            {
              if (fixedAlignments[index1] == i)
              {
                if (index1 == target.Length - 1)
                {
                  this.m_beta[index1][i] = 1.0;
                }
                else
                {
                  for (int index2 = 0; index2 < 2 * source.Length; ++index2)
                  {
                    int num = index2 < source.Length ? source[index2] + 1 : 0;
                    this.m_beta[index1][i] += this.m_beta[index1 + 1][index2];
                  }
                }
              }
            }
            else if (index1 == target.Length - 1)
            {
              this.m_beta[index1][i] = 1.0;
            }
            else
            {
              for (int j = 0; j < 2 * source.Length; ++j)
              {
                int index2 = j < source.Length ? source[j] + 1 : 0;
                this.m_beta[index1][i] += this.m_beta[index1 + 1][j] * this.Align(i, j, source.Length) * this.m_t[target[index1 + 1]][index2];
              }
            }
            if (this.m_alphaTotal[index1] != 0.0)
              this.m_beta[index1][i] /= this.m_alphaTotal[index1];
          }
        }
        for (int index1 = 0; index1 < target.Length; ++index1)
        {
          double num = 0.0;
          for (int index2 = 0; index2 < 2 * source.Length; ++index2)
            num += this.m_alpha[index1][index2] * this.m_beta[index1][index2];
          for (int i = 0; i < 2 * source.Length; ++i)
          {
            this.m_gamma[index1][i] = num == 0.0 ? 0.0 : this.m_alpha[index1][i] * this.m_beta[index1][i] / num;
            if (index1 < target.Length - 1)
            {
              for (int j = 0; j < 2 * source.Length; ++j)
              {
                int index2 = j < source.Length ? source[j] + 1 : 0;
                this.m_xi[index1][i][j] = num == 0.0 ? 0.0 : this.m_alpha[index1][i] * this.Align(i, j, source.Length) * this.m_t[target[index1 + 1]][index2] * this.m_beta[index1 + 1][j] / num;
              }
            }
          }
        }
        for (int index1 = 0; index1 < 2 * source.Length; ++index1)
        {
          this.m_cPi[index1][source.Length - 1] += this.m_gamma[0][index1];
          double num = 0.0;
          for (int index2 = 0; index2 < target.Length; ++index2)
            num += this.m_gamma[index2][index1];
          int index3 = index1 < source.Length ? source[index1] + 1 : 0;
          for (int index2 = 0; index2 < target.Length; ++index2)
          {
            if (num != 0.0)
            {
              this.m_ctEF[target[index2]][index3] += this.m_gamma[index2][index1] / num;
              this.m_ctF[index3] += this.m_gamma[index2][index1] / num;
            }
          }
        }
        for (int index1 = 0; index1 < source.Length; ++index1)
        {
          double num1 = 0.0;
          for (int index2 = 0; index2 < target.Length - 1; ++index2)
          {
            for (int index3 = 0; index3 < source.Length; ++index3)
              num1 += this.m_xi[index2][index1][index3];
          }
          if (num1 != 0.0)
          {
            for (int index2 = 0; index2 < source.Length; ++index2)
            {
              double num2 = 0.0;
              for (int index3 = 0; index3 < target.Length - 1; ++index3)
                num2 += this.m_xi[index3][index1][index2];
              this.m_cAlign[this.m_sourceMaxLength - 1 + index1 - index2] += num2 / num1;
            }
          }
        }
      }
    }

    public override void EndPass()
    {
      if (this.m_fTrainWithModel1 || this.m_fTrainWithModel2)
      {
        base.EndPass();
        if (!this.m_needsMoreTraining)
        {
          this.m_needsMoreTraining = true;
          if (this.m_fTrainWithModel2)
          {
            this.m_fTrainWithModel2 = false;
          }
          else
          {
            this.m_fTrainWithModel1 = false;
            this.m_fTrainWithModel2 = this.m_citerModel2Max > 0;
          }
        }
        else
        {
          if (this.m_fTrainWithModel1 || this.m_citerModel2Max <= 0)
            return;
          this.m_fTrainWithModel2 = true;
        }
      }
      else
      {
        ++this.m_citerHMM;
        this.EstimateHMMProbabilities();
        this.m_delta = 0.0;
        if (this.m_citerHMM == 1)
          this.CalculateModel1Delta();
        else
          this.CalculateHMMDelta();
        this.m_needsMoreTraining = this.m_citerHMM < this.m_citerHMMMax;
        if (this.m_needsMoreTraining)
        {
          this.SaveCurrentHMM();
          this.ZeroHMMCounts();
        }
      }
    }

    public override List<Alignment> GetAlignments(
      int[] source,
      int[] target,
      int[] fixedAlignments)
    {
      int index1 = -1;
      List<Alignment> alignmentList = new List<Alignment>();
      for (int index2 = 0; index2 < target.Length; ++index2)
      {
        for (int j = 0; j < 2 * source.Length; ++j)
        {
          double num1 = double.MinValue;
          int num2 = -1;
          if (fixedAlignments[index2] != -1)
          {
            if (fixedAlignments[index2] == j)
            {
              if (index2 == 0)
              {
                num1 = 1.0;
              }
              else
              {
                for (int index3 = 0; index3 < 2 * source.Length; ++index3)
                {
                  double num3 = this.m_logProbability[index2 - 1][index3];
                  if (num3 > num1)
                  {
                    num1 = num3;
                    num2 = index3;
                  }
                }
              }
            }
          }
          else
          {
            int index3 = j < source.Length ? source[j] + 1 : 0;
            double num3 = Math.Log(this.m_t[target[index2]][index3]);
            if (index2 == 0)
            {
              num1 = num3 + Math.Log(this.m_pi[j][source.Length - 1]);
            }
            else
            {
              for (int i = 0; i < 2 * source.Length; ++i)
              {
                double num4 = this.m_logProbability[index2 - 1][i] + num3 + Math.Log(this.Align(i, j, source.Length));
                if (num4 > num1)
                {
                  num1 = num4;
                  num2 = i;
                }
              }
            }
          }
          this.m_logProbability[index2][j] = num1;
          this.m_backPointer[index2][j] = num2;
        }
        if (index2 == target.Length - 1)
        {
          double num1 = this.m_logProbability[index2][0];
          int num2 = 0;
          for (int index3 = 1; index3 < 2 * source.Length; ++index3)
          {
            if (this.m_logProbability[index2][index3] > num1)
            {
              num1 = this.m_logProbability[index2][index3];
              num2 = index3;
            }
          }
          index1 = num2;
        }
      }
      for (int target1 = target.Length - 1; target1 >= 0; --target1)
      {
        int i = this.m_backPointer[target1][index1];
        if (index1 < source.Length)
        {
          if (target1 == 0)
            alignmentList.Insert(0, new Alignment(index1, target1, this.m_t[target[target1]][source[index1] + 1], this.m_pi[index1][source.Length - 1]));
          else
            alignmentList.Insert(0, new Alignment(index1, target1, this.m_t[target[target1]][source[index1] + 1], this.Align(i, index1, source.Length)));
        }
        index1 = i;
      }
      return alignmentList;
    }

    private void EstimateHMMProbabilities()
    {
      this.EstimateModel2Probabilities();
      for (int index1 = 0; index1 < this.m_sourceMaxLength - 1; ++index1)
      {
        double num = 0.0;
        for (int index2 = 0; index2 < 2 * this.m_sourceMaxLength; ++index2)
          num += this.m_cPi[index2][index1];
        for (int index2 = 0; index2 < 2 * this.m_sourceMaxLength; ++index2)
          this.m_pi[index2][index1] = num == 0.0 ? 0.0 : this.m_cPi[index2][index1] / num;
      }
      double num1 = 0.0;
      for (int index = 0; index < 2 * this.m_sourceMaxLength - 1; ++index)
        num1 += this.m_cAlign[index];
      Debug.Assert(num1 != 0.0);
      for (int index = 0; index < 2 * this.m_sourceMaxLength - 1; ++index)
        this.m_align[index] = (this.m_cAlign[index] /= num1);
    }

    protected void CalculateHMMDelta()
    {
      this.CalculateModel2Delta();
      for (int index1 = 0; index1 < this.m_sourceMaxLength - 1; ++index1)
      {
        for (int index2 = 0; index2 < 2 * this.m_sourceMaxLength; ++index2)
        {
          double num = Math.Abs(this.m_pi[index2][index1] - this.m_piPrev[index2][index1]);
          Debug.Assert(num <= 1.0);
          if (num > this.m_delta)
            this.m_delta = num;
        }
      }
      for (int index = 0; index < 2 * this.m_sourceMaxLength - 1; ++index)
      {
        double num = Math.Abs(this.m_align[index] - this.m_alignPrev[index]);
        Debug.Assert(num <= 1.0);
        if (num > this.m_delta)
          this.m_delta = num;
      }
    }

    protected void SaveCurrentHMM()
    {
      this.SaveCurrentModel2();
      for (int index1 = 0; index1 < this.m_sourceMaxLength - 1; ++index1)
      {
        for (int index2 = 0; index2 < 2 * this.m_sourceMaxLength; ++index2)
          this.m_piPrev[index2][index1] = this.m_pi[index2][index1];
      }
      for (int index = 0; index < 2 * this.m_sourceMaxLength - 1; ++index)
        this.m_alignPrev[index] = this.m_align[index];
    }

    private void ZeroHMMCounts()
    {
      this.ZeroModel2Counts();
      for (int index1 = 0; index1 < this.m_sourceMaxLength - 1; ++index1)
      {
        for (int index2 = 0; index2 < 2 * this.m_sourceMaxLength; ++index2)
          this.m_cPi[index2][index1] = 0.0;
      }
      for (int index = 0; index < 2 * this.m_sourceMaxLength - 1; ++index)
        this.m_cAlign[index] = 0.0;
    }

    private double Align(int i, int j, int length)
    {
      if (i >= length)
        return j == i || j + length == i ? 0.2 : 0.0;
      if (j < length)
        return this.m_align[this.m_sourceMaxLength - 1 + i - j];
      return i + length == j ? 0.2 : 0.0;
    }
  }
}
