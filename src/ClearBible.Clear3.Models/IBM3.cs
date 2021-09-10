// Decompiled with JetBrains decompiler
// Type: Models.IBM3
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Models
{
  public class IBM3 : HMM
  {
    private double[][] m_n;
    private double[][][][] m_d;
    private double m_p1;
    private double m_cpP0;
    private double m_cpP1;
    private double[][][][] m_cdJILM;
    private double[][][] m_cdILM;
    private double[][] m_cnPF;
    private double[] m_cnF;
    private double[][] m_nPrev;
    private double[][][][] m_dPrev;
    private bool m_fTrainWithHMM;
    private int m_citerModel3;
    private int m_citerModel3Max;

    public IBM3(
      int sourceWordCount,
      int targetWordCount,
      int sourceMaxLength,
      int targetMaxLength)
      : base(sourceWordCount, targetWordCount, sourceMaxLength, targetMaxLength)
    {
      this.m_d = new double[this.m_targetMaxLength][][][];
      this.m_dPrev = new double[this.m_targetMaxLength][][][];
      this.m_cdJILM = new double[this.m_targetMaxLength][][][];
      for (int index1 = 0; index1 < this.m_targetMaxLength; ++index1)
      {
        this.m_d[index1] = new double[this.m_sourceMaxLength][][];
        this.m_dPrev[index1] = new double[this.m_sourceMaxLength][][];
        this.m_cdJILM[index1] = new double[this.m_sourceMaxLength][][];
        for (int index2 = 0; index2 < this.m_sourceMaxLength; ++index2)
        {
          this.m_d[index1][index2] = new double[this.m_targetMaxLength][];
          this.m_dPrev[index1][index2] = new double[this.m_targetMaxLength][];
          this.m_cdJILM[index1][index2] = new double[this.m_targetMaxLength][];
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            this.m_d[index1][index2][index3] = new double[this.m_sourceMaxLength - 1];
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
              this.m_d[index1][index2][index3][index4] = 1.0;
            this.m_dPrev[index1][index2][index3] = new double[this.m_sourceMaxLength - 1];
            this.m_cdJILM[index1][index2][index3] = new double[this.m_sourceMaxLength - 1];
          }
        }
      }
      this.m_cdILM = new double[this.m_sourceMaxLength][][];
      for (int index1 = 0; index1 < this.m_sourceMaxLength; ++index1)
      {
        this.m_cdILM[index1] = new double[this.m_targetMaxLength][];
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
          this.m_cdILM[index1][index2] = new double[this.m_sourceMaxLength - 1];
      }
      this.m_n = new double[this.m_targetMaxLength + 1][];
      this.m_nPrev = new double[this.m_targetMaxLength + 1][];
      this.m_cnPF = new double[this.m_targetMaxLength + 1][];
      for (int index1 = 0; index1 < this.m_targetMaxLength + 1; ++index1)
      {
        this.m_n[index1] = new double[this.m_sourceWordCount - 1];
        for (int index2 = 0; index2 < this.m_sourceWordCount - 1; ++index2)
          this.m_n[index1][index2] = 1.0;
        this.m_nPrev[index1] = new double[this.m_sourceWordCount - 1];
        this.m_cnPF[index1] = new double[this.m_sourceWordCount - 1];
      }
      this.m_cnF = new double[this.m_sourceWordCount - 1];
      this.m_p1 = 0.5;
      this.m_cpP0 = 0.0;
      this.m_cpP1 = 0.0;
      this.m_citerModel3 = 0;
    }

    public override int MaxIterations
    {
      get
      {
        return this.m_citerModel1Max + this.m_citerModel2Max + this.m_citerHMMMax + this.m_citerModel3Max;
      }
    }

    public override string Phase
    {
      get
      {
        if (this.m_fTrainWithModel1)
          return "IBM Model 1";
        if (this.m_fTrainWithModel2)
          return "IBM Model 2";
        return this.m_fTrainWithHMM ? "HMM" : "IBM Model 3";
      }
    }

    public override string Summary
    {
      get
      {
        string str = string.Format("Iterations: Model1={0}", (object) this.m_citerModel1);
        if (this.m_citerModel2Max > 0)
          str += string.Format(" Model2={0}", (object) this.m_citerModel2);
        if (this.m_citerHMMMax > 0)
          str += string.Format(" HMM={0}", (object) this.m_citerHMM);
        return str + string.Format(" Model3={0} MaxDelta: {1}]", (object) this.m_citerModel3, (object) this.m_delta);
      }
    }

    public override bool ConfigureRun(List<ModelSpec> runList)
    {
      if (runList.Count == 3 && runList[1].Model == Model.Model2 && runList[2].Model == Model.Model1)
      {
        this.m_citerModel1Max = runList[2].RunCount;
        this.m_citerModel2Max = runList[1].RunCount;
        this.m_citerHMMMax = 0;
      }
      else
      {
        if (runList.Count != 3 || runList[1].Model != Model.HMM || runList[2].Model != Model.Model1)
          return false;
        this.m_citerModel1Max = runList[2].RunCount;
        this.m_citerModel2Max = 0;
        this.m_citerHMMMax = runList[1].RunCount;
      }
      Debug.Assert(runList[0].Model == Model.Model3);
      this.m_citerModel3Max = runList[0].RunCount;
      this.m_fTrainWithModel1 = true;
      return true;
    }

    public override void AddSegmentPair(int[] source, int[] target, int[] fixedAlignments)
    {
      if (this.m_fTrainWithModel1 || this.m_fTrainWithModel2 || this.m_fTrainWithHMM)
        base.AddSegmentPair(source, target, fixedAlignments);
      else
        this.UpdateCounts(source, target, this.GetAlignmentCandidates(source, target, fixedAlignments), fixedAlignments);
    }

    public override void EndPass()
    {
      if (this.m_fTrainWithModel1 || this.m_fTrainWithModel2 || this.m_fTrainWithHMM)
      {
        base.EndPass();
        if (this.m_needsMoreTraining)
          return;
        this.m_needsMoreTraining = true;
        if (this.m_fTrainWithModel2)
          this.m_fTrainWithModel2 = false;
        else if (this.m_fTrainWithHMM)
        {
          this.m_fTrainWithHMM = false;
        }
        else
        {
          this.m_fTrainWithModel1 = false;
          if (this.m_citerModel2Max > 0)
            this.m_fTrainWithModel2 = true;
          else
            this.m_fTrainWithHMM = true;
        }
      }
      else
      {
        ++this.m_citerModel3;
        this.EstimateModel3Probabilities();
        this.m_delta = 0.0;
        if (this.m_citerModel3 == 1)
          this.CalculateModel1Delta();
        else
          this.CalculateModel3Delta();
        this.m_needsMoreTraining = this.m_citerModel3 < this.m_citerModel3Max;
        if (this.m_needsMoreTraining)
        {
          this.SaveCurrentModel3();
          this.ZeroModel3Counts();
        }
      }
    }

    public override List<Alignment> GetAlignments(
      int[] source,
      int[] target,
      int[] fixedAlignments)
    {
      int[] numArray = (int[]) null;
      double num1 = double.MinValue;
      foreach (int[] alignmentCandidate in this.GetAlignmentCandidates(source, target, fixedAlignments))
      {
        double num2 = this.LogProbability(source, target, alignmentCandidate, fixedAlignments);
        if (num2 > num1)
        {
          num1 = num2;
          numArray = alignmentCandidate;
        }
      }
      List<Alignment> alignmentList = new List<Alignment>();
      if (numArray != null)
      {
        for (int target1 = 0; target1 < target.Length; ++target1)
        {
          if ((uint) numArray[target1] > 0U)
          {
            if (fixedAlignments[target1] != -1)
              alignmentList.Add(new Alignment(numArray[target1] - 1, target1, 1.0, 1.0));
            else
              alignmentList.Add(new Alignment(numArray[target1] - 1, target1, this.m_t[target[target1]][source[numArray[target1] - 1] + 1], this.m_a[numArray[target1]][target1][target.Length - 1][source.Length - 1]));
          }
        }
      }
      return alignmentList;
    }

    private void EstimateModel3Probabilities()
    {
      this.EstimateModel2Probabilities();
      for (int index1 = 0; index1 < this.m_targetMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
            {
              if (this.m_cdILM[index2][index3][index4] != 0.0)
              {
                this.m_d[index1][index2][index3][index4] = this.m_cdJILM[index1][index2][index3][index4] / this.m_cdILM[index2][index3][index4];
                Debug.Assert(!double.IsNaN(this.m_d[index1][index2][index3][index4]));
              }
              else
                this.m_d[index1][index2][index3][index4] = 0.0;
            }
          }
        }
      }
      for (int index1 = 0; index1 < this.m_targetMaxLength + 1; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceWordCount - 1; ++index2)
        {
          if (this.m_cnF[index2] != 0.0)
          {
            this.m_n[index1][index2] = this.m_cnPF[index1][index2] / this.m_cnF[index2];
            Debug.Assert(!double.IsNaN(this.m_n[index1][index2]));
          }
          else
            this.m_n[index1][index2] = 0.0;
        }
      }
      if (this.m_cpP0 + this.m_cpP1 == 0.0)
      {
        this.m_p1 = 0.0;
      }
      else
      {
        this.m_p1 = this.m_cpP1 / (this.m_cpP0 + this.m_cpP1);
        Debug.Assert(!double.IsNaN(this.m_p1));
      }
    }

    private void UpdateCounts(
      int[] source,
      int[] target,
      List<int[]> alignments,
      int[] fixedAlignments)
    {
      double num1 = 0.0;
      double[] numArray = new double[alignments.Count];
      for (int index = 0; index < numArray.Length; ++index)
      {
        double d = this.LogProbability(source, target, alignments[index], fixedAlignments);
        Debug.Assert(!double.IsNaN(d));
        if (d != double.MinValue)
        {
          numArray[index] = Math.Exp(d);
          num1 += numArray[index];
        }
        else
          numArray[index] = 0.0;
      }
      for (int index1 = 0; index1 < numArray.Length; ++index1)
      {
        int[] alignment = alignments[index1];
        int num2 = 0;
        double d;
        if (num1 == 0.0)
        {
          d = 0.0;
        }
        else
        {
          d = numArray[index1] / num1;
          Debug.Assert(!double.IsNaN(d));
        }
        for (int index2 = 0; index2 < target.Length; ++index2)
        {
          int index3 = alignment[index2] != 0 ? source[alignment[index2] - 1] + 1 : 0;
          this.m_ctEF[target[index2]][index3] += d;
          this.m_ctF[index3] += d;
          this.m_caIJLM[alignment[index2]][index2][target.Length - 1][source.Length - 1] += d;
          this.m_caJLM[index2][target.Length - 1][source.Length - 1] += d;
          this.m_cdJILM[index2][alignment[index2]][target.Length - 1][source.Length - 1] += d;
          this.m_cdILM[alignment[index2]][target.Length - 1][source.Length - 1] += d;
          if (alignment[index2] == 0)
            ++num2;
        }
        this.m_cpP1 += (double) num2 * d;
        this.m_cpP0 += (double) (source.Length - 2 * num2) * d;
        for (int index2 = 0; index2 < source.Length; ++index2)
        {
          int index3 = 0;
          for (int index4 = 0; index4 < target.Length; ++index4)
          {
            if (index2 + 1 == alignment[index4])
              ++index3;
          }
          this.m_cnPF[index3][source[index2]] += d;
          this.m_cnF[source[index2]] += d;
        }
      }
    }

    protected void CalculateModel3Delta()
    {
      this.CalculateModel2Delta();
      for (int index1 = 0; index1 < this.m_targetMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
            {
              double num = Math.Abs(this.m_d[index1][index2][index3][index4] - this.m_dPrev[index1][index2][index3][index4]);
              if (num > this.m_delta)
                this.m_delta = num;
            }
          }
        }
      }
      for (int index1 = 0; index1 < this.m_targetMaxLength + 1; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceWordCount - 1; ++index2)
        {
          double num = Math.Abs(this.m_n[index1][index2] - this.m_nPrev[index1][index2]);
          if (num > this.m_delta)
            this.m_delta = num;
        }
      }
    }

    protected void SaveCurrentModel3()
    {
      this.SaveCurrentModel2();
      for (int index1 = 0; index1 < this.m_targetMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
              this.m_dPrev[index1][index2][index3][index4] = this.m_d[index1][index2][index3][index4];
          }
        }
      }
      for (int index1 = 0; index1 < this.m_targetMaxLength + 1; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceWordCount - 1; ++index2)
          this.m_nPrev[index1][index2] = this.m_n[index1][index2];
      }
    }

    private void ZeroModel3Counts()
    {
      this.ZeroModel2Counts();
      for (int index1 = 0; index1 < this.m_targetMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_targetMaxLength; ++index3)
          {
            for (int index4 = 0; index4 < this.m_sourceMaxLength - 1; ++index4)
              this.m_cdJILM[index1][index2][index3][index4] = 0.0;
          }
        }
      }
      for (int index1 = 0; index1 < this.m_sourceMaxLength; ++index1)
      {
        for (int index2 = 0; index2 < this.m_targetMaxLength; ++index2)
        {
          for (int index3 = 0; index3 < this.m_sourceMaxLength - 1; ++index3)
            this.m_cdILM[index1][index2][index3] = 0.0;
        }
      }
      this.m_cpP1 = 0.0;
      this.m_cpP0 = 0.0;
      for (int index1 = 0; index1 < this.m_targetMaxLength + 1; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceWordCount - 1; ++index2)
          this.m_cnPF[index1][index2] = 0.0;
      }
      for (int index = 0; index < this.m_sourceWordCount - 1; ++index)
        this.m_cnF[index] = 0.0;
    }

    private List<int[]> GetAlignmentCandidates(
      int[] source,
      int[] target,
      int[] fixedAlignments)
    {
      List<int[]> numArrayList = new List<int[]>();
      for (int jPegged = 0; jPegged < target.Length; ++jPegged)
      {
        if (fixedAlignments[jPegged] != -1)
        {
          int[] peggedCandidate = this.CreatePeggedCandidate(source, target, jPegged, fixedAlignments[jPegged], fixedAlignments);
          int[] better;
          this.HillClimb(source, target, peggedCandidate, jPegged, fixedAlignments, out better);
          numArrayList.Add(better);
        }
        else
        {
          for (int iPegged = 0; iPegged < source.Length + 1; ++iPegged)
          {
            int[] peggedCandidate = this.CreatePeggedCandidate(source, target, jPegged, iPegged, fixedAlignments);
            int[] better;
            this.HillClimb(source, target, peggedCandidate, jPegged, fixedAlignments, out better);
            numArrayList.Add(better);
          }
        }
      }
      return numArrayList;
    }

    private int[] CreatePeggedCandidate(
      int[] source,
      int[] target,
      int jPegged,
      int iPegged,
      int[] fixedAlignments)
    {
      int[] numArray = new int[target.Length];
      for (int index1 = 0; index1 < target.Length; ++index1)
      {
        if (index1 == jPegged)
          numArray[index1] = iPegged;
        else if (fixedAlignments[index1] != -1)
        {
          numArray[index1] = fixedAlignments[index1] + 1;
        }
        else
        {
          double num1 = this.m_a[0][index1][target.Length - 1][source.Length - 1] * this.m_t[target[index1]][0];
          int num2 = 0;
          for (int index2 = 1; index2 < source.Length + 1; ++index2)
          {
            double num3 = this.m_a[index2][index1][target.Length - 1][source.Length - 1] * this.m_t[target[index1]][source[index2 - 1] + 1];
            if (num3 > num1)
            {
              num1 = num3;
              num2 = index2;
            }
          }
          numArray[index1] = num2;
        }
      }
      return numArray;
    }

    private void HillClimb(
      int[] source,
      int[] target,
      int[] alignment,
      int jPegged,
      int[] fixedAlignments,
      out int[] better)
    {
      int[] alignment1 = alignment;
      while (this.FindMostProbableNeighbor(source, target, alignment1, jPegged, fixedAlignments, out better))
        alignment1 = better;
    }

    private void Dump(string title, int[] arr)
    {
      Console.Write(title);
      for (int index = 0; index < arr.Length; ++index)
        Console.Write("{0} ", (object) arr[index]);
      Console.Write(" ");
    }

    private bool FindMostProbableNeighbor(
      int[] source,
      int[] target,
      int[] alignment,
      int jPegged,
      int[] fixedAlignments,
      out int[] better)
    {
      bool flag1 = false;
      double num1 = 1.0;
      double num2 = double.MinValue;
      better = new int[alignment.Length];
      for (int index = 0; index < target.Length; ++index)
        better[index] = alignment[index];
      int[] alignment1 = new int[alignment.Length];
      int[] numArray = new int[source.Length + 1];
      for (int index = 0; index < target.Length; ++index)
        ++numArray[alignment[index]];
      double d1 = this.LogProbability(source, target, alignment, fixedAlignments);
      Debug.Assert(!double.IsNaN(d1));
      for (int index1 = 0; index1 < target.Length; ++index1)
      {
        if (index1 != jPegged && fixedAlignments[index1] == -1)
        {
          for (int index2 = 0; index2 < source.Length + 1; ++index2)
          {
            if (alignment[index1] != index2)
            {
              for (int index3 = 0; index3 < target.Length; ++index3)
                alignment1[index3] = index3 != index1 ? alignment[index3] : index2;
              bool flag2;
              if (d1 == double.MinValue)
              {
                double d2 = this.LogProbability(source, target, alignment1, fixedAlignments);
                Debug.Assert(!double.IsNaN(d2));
                flag2 = d2 > num2 && Math.Abs(d2 - num2) > 1E-15;
                if (flag2)
                  num2 = d2;
              }
              else
              {
                double num3;
                if (index2 == 0)
                {
                  int index3 = 0;
                  int index4 = source[alignment[index1] - 1] + 1;
                  num3 = (double) ((source.Length - 2 * numArray[0]) * (source.Length - 2 * numArray[0] - 1)) / (double) ((numArray[0] + 1) * (source.Length - numArray[0])) * (this.m_p1 / ((1.0 - this.m_p1) * (1.0 - this.m_p1))) * (1.0 / (double) numArray[alignment[index1]]) * (this.m_n[numArray[alignment[index1]] - 1][index4 - 1] / this.m_n[numArray[alignment[index1]]][index4 - 1]) * (this.m_t[target[index1]][index3] / this.m_t[target[index1]][index4]) * (this.m_d[index1][index2][target.Length - 1][source.Length - 1] / this.m_d[index1][alignment[index1]][target.Length - 1][source.Length - 1]);
                }
                else if (alignment[index1] == 0)
                {
                  int index3 = source[index2 - 1] + 1;
                  int index4 = 0;
                  num3 = (double) (numArray[0] * (source.Length - numArray[0] + 1)) / (double) ((source.Length - 2 * numArray[0] + 2) * (source.Length - 2 * numArray[0] + 1)) * ((1.0 - this.m_p1) * (1.0 - this.m_p1) / this.m_p1) * ((double) numArray[index2] + 1.0) * (this.m_n[numArray[index2] + 1][index3 - 1] / this.m_n[numArray[index2]][index3 - 1]) * (this.m_t[target[index1]][index3] / this.m_t[target[index1]][index4]) * (this.m_d[index1][index2][target.Length - 1][source.Length - 1] / this.m_d[index1][alignment[index1]][target.Length - 1][source.Length - 1]);
                }
                else
                {
                  int index3 = source[index2 - 1] + 1;
                  int index4 = source[alignment[index1] - 1] + 1;
                  num3 = ((double) numArray[index2] + 1.0) / (double) numArray[alignment[index1]] * (this.m_n[numArray[alignment[index1]] - 1][index4 - 1] / this.m_n[numArray[alignment[index1]]][index4 - 1]) * (this.m_n[numArray[index2] + 1][index3 - 1] / this.m_n[numArray[index2]][index3 - 1]) * (this.m_t[target[index1]][index3] / this.m_t[target[index1]][index4]) * (this.m_d[index1][index2][target.Length - 1][source.Length - 1] / this.m_d[index1][alignment[index1]][target.Length - 1][source.Length - 1]);
                }
                flag2 = num3 > num1 && Math.Abs(num3 - num1) > 1E-15;
                if (flag2)
                  num1 = num3;
              }
              if (flag2)
              {
                flag1 = true;
                for (int index3 = 0; index3 < target.Length; ++index3)
                  better[index3] = alignment1[index3];
              }
            }
          }
        }
      }
      for (int index1 = 0; index1 < target.Length; ++index1)
      {
        if (index1 != jPegged && fixedAlignments[index1] == -1)
        {
          for (int index2 = 1; index2 < target.Length; ++index2)
          {
            if (index1 != index2 && index2 != jPegged && fixedAlignments[index2] == -1 && alignment[index1] != alignment[index2])
            {
              for (int index3 = 0; index3 < target.Length; ++index3)
                alignment1[index3] = index3 != index1 ? (index3 != index2 ? alignment[index3] : alignment[index1]) : alignment[index2];
              bool flag2;
              if (d1 == double.MinValue)
              {
                double d2 = this.LogProbability(source, target, alignment1, fixedAlignments);
                Debug.Assert(!double.IsNaN(d2));
                flag2 = d2 > num2 && Math.Abs(d2 - num2) > 1E-15;
                if (flag2)
                  num2 = d2;
              }
              else
              {
                int index3 = alignment[index1] != 0 ? source[alignment[index1] - 1] + 1 : 0;
                int index4 = alignment[index2] != 0 ? source[alignment[index2] - 1] + 1 : 0;
                double num3 = this.m_t[target[index1]][index4] / this.m_t[target[index1]][index3] * (this.m_t[target[index2]][index3] / this.m_t[target[index2]][index4]) * (this.m_d[index1][alignment[index2]][target.Length - 1][source.Length - 1] / this.m_d[index1][alignment[index1]][target.Length - 1][source.Length - 1]) * (this.m_d[index2][alignment[index1]][target.Length - 1][source.Length - 1] / this.m_d[index2][alignment[index2]][target.Length - 1][source.Length - 1]);
                flag2 = num3 > num1 && Math.Abs(num3 - num1) > 1E-15;
                if (flag2)
                  num1 = num3;
              }
              if (flag2)
              {
                flag1 = true;
                for (int index3 = 0; index3 < target.Length; ++index3)
                  better[index3] = alignment1[index3];
              }
            }
          }
        }
      }
      return flag1;
    }

    private double LogProbability(
      int[] source,
      int[] target,
      int[] alignment,
      int[] fixedAlignments)
    {
      int k = 0;
      for (int index = 0; index < target.Length; ++index)
      {
        if (alignment[index] == 0)
          ++k;
      }
      if (this.LogBinom(source.Length - k, k) == double.MinValue)
        return double.MinValue;
      double num = this.LogBinom(source.Length - k, k) + (double) k * Math.Log(this.m_p1) + (double) (source.Length - 2 * k) * Math.Log(1.0 - this.m_p1);
      for (int index1 = 0; index1 < source.Length; ++index1)
      {
        int n = 0;
        for (int index2 = 0; index2 < target.Length; ++index2)
        {
          if (index1 + 1 == alignment[index2])
            ++n;
        }
        if (this.m_n[n][source[index1]] == 0.0)
          return double.MinValue;
        num += this.LogFactorial(n) + Math.Log(this.m_n[n][source[index1]]);
      }
      for (int index1 = 0; index1 < target.Length; ++index1)
      {
        if (fixedAlignments[index1] == -1)
        {
          int index2 = alignment[index1] != 0 ? source[alignment[index1] - 1] + 1 : 0;
          if (this.m_t[target[index1]][index2] == 0.0 || this.m_d[index1][alignment[index1]][target.Length - 1][source.Length - 1] == 0.0)
            return double.MinValue;
          num += Math.Log(this.m_t[target[index1]][index2]) + Math.Log(this.m_d[index1][alignment[index1]][target.Length - 1][source.Length - 1]);
        }
      }
      return num;
    }

    private double LogBinom(int n, int k)
    {
      if (k > n)
        return double.MinValue;
      if (k > n / 2)
        k = n - k;
      if (k == 0)
        return 0.0;
      if (k == 1)
        return Math.Log((double) n);
      double num = 0.0;
      for (int index = 0; index < k; ++index)
        num = num + Math.Log((double) (n - index)) - Math.Log((double) (index + 1));
      return num;
    }

    private double LogFactorial(int n)
    {
      switch (n)
      {
        case 0:
        case 1:
          return 0.0;
        case 2:
          return Math.Log(2.0);
        case 3:
          return Math.Log(6.0);
        case 4:
          return Math.Log(24.0);
        default:
          double num = 0.0;
          for (int index = 2; index <= n; ++index)
            num += Math.Log((double) index);
          return num;
      }
    }
  }
}
