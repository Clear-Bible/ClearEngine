// Decompiled with JetBrains decompiler
// Type: Models.ModelBuilder
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Models
{
  public class ModelBuilder
  {
    private List<string> m_sourceFiles;
    private List<string> m_targetFiles;
    private List<string> m_prealignmentFiles;
    private List<ModelSpec> m_runList;
    private SymmetrizationType m_symType;
    private bool m_lowerCase;
    private Dictionary<string, int> m_corpus1Dict;
    private Dictionary<string, int> m_corpus2Dict;
    private List<IEnumerable<int[]>> m_corpus1SegmentLists;
    private List<IEnumerable<int[]>> m_corpus2SegmentLists;
    private List<IEnumerable<Alignment[]>> m_corpusAlignmentLists;
    private IAlignmentModel m_model;

    public ModelBuilder()
    {
      this.m_sourceFiles = (List<string>) null;
      this.m_targetFiles = (List<string>) null;
      this.m_prealignmentFiles = (List<string>) null;
      this.m_runList = (List<ModelSpec>) null;
      this.m_symType = SymmetrizationType.None;
      this.m_lowerCase = false;
      this.m_model = (IAlignmentModel) null;
      this.m_corpus1Dict = (Dictionary<string, int>) null;
      this.m_corpus2Dict = (Dictionary<string, int>) null;
    }

    public string SourceFile
    {
      set
      {
        this.m_sourceFiles = new List<string>() { value };
      }
    }

    public List<string> SourceFiles
    {
      set
      {
        this.m_sourceFiles = value;
      }
    }

    public string TargetFile
    {
      set
      {
        this.m_targetFiles = new List<string>() { value };
      }
    }

    public List<string> TargetFiles
    {
      set
      {
        this.m_targetFiles = value;
      }
    }

    public string PrealignmentFile
    {
      set
      {
        this.m_prealignmentFiles = new List<string>()
        {
          value
        };
      }
    }

    public List<string> PrealignmentFiles
    {
      set
      {
        this.m_prealignmentFiles = value;
      }
    }

    public string RunSpecification
    {
      set
      {
        this.m_runList = RunSpec.ParseModelList(value);
        if (this.m_runList == null)
          throw new Exception("Unsupported run specification");
      }
    }

    public SymmetrizationType Symmetrization
    {
      set
      {
        this.m_symType = value;
      }
    }

    public bool LowerCase
    {
      set
      {
        this.m_lowerCase = true;
      }
    }

    public string Summary
    {
      get
      {
        return this.m_model == null ? "Not trained yet." : this.m_model.Summary;
      }
    }

    public void Train(IProgress<Progress> progressBar)
    {
      this.Validate();
      this.m_corpus1Dict = new Dictionary<string, int>();
      this.m_corpus2Dict = new Dictionary<string, int>();
      CorpusSegmenter<TextFileSegmenter> corpusSegmenter1 = new CorpusSegmenter<TextFileSegmenter>((IEnumerable<string>) this.m_sourceFiles, this.m_corpus1Dict);
      CorpusSegmenter<TextFileSegmenter> corpusSegmenter2 = new CorpusSegmenter<TextFileSegmenter>((IEnumerable<string>) this.m_targetFiles, this.m_corpus2Dict);
      corpusSegmenter1.LowerCase = this.m_lowerCase;
      corpusSegmenter2.LowerCase = this.m_lowerCase;
      this.m_corpus1SegmentLists = corpusSegmenter1.SegmentLists;
      this.m_corpus2SegmentLists = corpusSegmenter2.SegmentLists;
      if (this.m_prealignmentFiles != null)
        this.m_corpusAlignmentLists = new CorpusPrealignment((IEnumerable<string>) this.m_prealignmentFiles).AlignmentLists;
      else
        this.m_corpusAlignmentLists = (List<IEnumerable<Alignment[]>>) null;
      switch (this.m_runList[0].Model)
      {
        case Model.Model1:
          this.m_model = this.m_symType != SymmetrizationType.None ? (IAlignmentModel) new Symmetrizer((IAlignmentModel) new IBM1(this.m_corpus1Dict.Count, this.m_corpus2Dict.Count), (IAlignmentModel) new IBM1(this.m_corpus2Dict.Count, this.m_corpus1Dict.Count), this.m_symType) : (IAlignmentModel) new IBM1(this.m_corpus1Dict.Count, this.m_corpus2Dict.Count);
          break;
        case Model.Model2:
          this.m_model = this.m_symType != SymmetrizationType.None ? (IAlignmentModel) new Symmetrizer((IAlignmentModel) new IBM2(this.m_corpus1Dict.Count, this.m_corpus2Dict.Count, corpusSegmenter1.MaxLength, corpusSegmenter2.MaxLength), (IAlignmentModel) new IBM2(this.m_corpus2Dict.Count, this.m_corpus1Dict.Count, corpusSegmenter2.MaxLength, corpusSegmenter1.MaxLength), this.m_symType) : (IAlignmentModel) new IBM2(this.m_corpus1Dict.Count, this.m_corpus2Dict.Count, corpusSegmenter1.MaxLength, corpusSegmenter2.MaxLength);
          break;
        case Model.Model3:
          this.m_model = this.m_symType != SymmetrizationType.None ? (IAlignmentModel) new Symmetrizer((IAlignmentModel) new IBM3(this.m_corpus1Dict.Count, this.m_corpus2Dict.Count, corpusSegmenter1.MaxLength, corpusSegmenter2.MaxLength), (IAlignmentModel) new IBM3(this.m_corpus2Dict.Count, this.m_corpus1Dict.Count, corpusSegmenter2.MaxLength, corpusSegmenter1.MaxLength), this.m_symType) : (IAlignmentModel) new IBM3(this.m_corpus1Dict.Count, this.m_corpus2Dict.Count, corpusSegmenter1.MaxLength, corpusSegmenter2.MaxLength);
          break;
        case Model.HMM:
          this.m_model = this.m_symType != SymmetrizationType.None ? (IAlignmentModel) new Symmetrizer((IAlignmentModel) new HMM(this.m_corpus1Dict.Count, this.m_corpus2Dict.Count, corpusSegmenter1.MaxLength, corpusSegmenter2.MaxLength), (IAlignmentModel) new HMM(this.m_corpus2Dict.Count, this.m_corpus1Dict.Count, corpusSegmenter2.MaxLength, corpusSegmenter1.MaxLength), this.m_symType) : (IAlignmentModel) new HMM(this.m_corpus1Dict.Count, this.m_corpus2Dict.Count, corpusSegmenter1.MaxLength, corpusSegmenter2.MaxLength);
          break;
        default:
          this.m_model = (IAlignmentModel) null;
          Debug.Assert(false);
          break;
      }
      if (!this.m_model.ConfigureRun(this.m_runList))
        throw new Exception("Unsupported run specification");
      int maxIterations = this.m_model.MaxIterations;
      for (int index1 = 0; index1 < maxIterations; ++index1)
      {
        for (int index2 = 0; index2 < this.m_sourceFiles.Count; ++index2)
        {
          using (IEnumerator<int[]> enumerator1 = this.m_corpus1SegmentLists[index2].GetEnumerator())
          {
            using (IEnumerator<int[]> enumerator2 = this.m_corpus2SegmentLists[index2].GetEnumerator())
            {
              if (this.m_corpusAlignmentLists == null)
              {
                while (enumerator1.MoveNext())
                {
                  if (!enumerator2.MoveNext())
                    throw new Exception("Number of segments in file does not match");
                  int[] array = this.ConvertAlignmentsToArray(enumerator1.Current, enumerator2.Current, (Alignment[]) null);
                  if (array == null)
                    throw new Exception("Fixed alignments are out of range of segments");
                  this.m_model.AddSegmentPair(enumerator1.Current, enumerator2.Current, array);
                }
              }
              else
              {
                using (IEnumerator<Alignment[]> enumerator3 = this.m_corpusAlignmentLists[index2].GetEnumerator())
                {
                  while (enumerator1.MoveNext())
                  {
                    if (!enumerator2.MoveNext())
                      throw new Exception("Number of segments in file does not match");
                    if (!enumerator3.MoveNext())
                      throw new Exception("Number of alignments in file does not match the number of segments");
                    int[] array = this.ConvertAlignmentsToArray(enumerator1.Current, enumerator2.Current, enumerator3.Current);
                    this.m_model.AddSegmentPair(enumerator1.Current, enumerator2.Current, array);
                  }
                }
              }
            }
          }
        }
        this.m_model.EndPass();
        if (progressBar != null)
        {
          Progress progress;
          progress.Phase = this.m_model.Phase;
          progress.MaxDelta = this.m_model.MaxDelta;
          progress.IterationRatio = ((double) index1 + 1.0) / (double) maxIterations;
          progressBar.Report(progress);
        }
        if (!this.m_model.NeedsMoreTraining)
          break;
      }
    }

    public Alignments GetAlignments(int index)
    {
      return new Alignments(this, index);
    }

    public IEnumerable<int[]> GetSourceSegments(int f)
    {
      if (this.m_model == null)
        throw new Exception("Not trained yet.");
      return this.m_corpus1SegmentLists[f];
    }

    public IEnumerable<int[]> GetTargetSegments(int f)
    {
      if (this.m_model == null)
        throw new Exception("Not trained yet.");
      return this.m_corpus2SegmentLists[f];
    }

    public IEnumerable<Alignment[]> GetPrealignments(int f)
    {
      if (this.m_model == null)
        throw new Exception("Not trained yet.");
      return this.m_corpusAlignmentLists == null ? (IEnumerable<Alignment[]>) null : this.m_corpusAlignmentLists[f];
    }

    public List<Alignment> GetAlignments(
      int[] source,
      int[] target,
      int[] fixedAlignments)
    {
      if (this.m_model == null)
        throw new Exception("Not trained yet.");
      return this.m_model.GetAlignments(source, target, fixedAlignments);
    }

    public Dictionary<string, int>.KeyCollection GetTargetWords()
    {
      if (this.m_corpus2Dict == null)
        throw new Exception("You need to call the Train method first");
      return this.m_corpus2Dict.Keys;
    }

    public Hashtable GetTranslationTable(double epsilon)
    {
      if (this.m_corpus1Dict == null)
        throw new Exception("You need to call the Train method first");
      Hashtable hashtable1 = new Hashtable();
      foreach (KeyValuePair<string, int> keyValuePair1 in this.m_corpus1Dict)
      {
        Hashtable hashtable2 = new Hashtable();
        foreach (KeyValuePair<string, int> keyValuePair2 in this.m_corpus2Dict)
        {
          if (this.m_model.T(keyValuePair1.Value, keyValuePair2.Value) > epsilon)
            hashtable2.Add((object) keyValuePair2.Key, (object) this.m_model.T(keyValuePair1.Value, keyValuePair2.Value));
        }
        hashtable1.Add((object) keyValuePair1.Key, (object) hashtable2);
      }
      if (this.m_model.HasNullSourceWord)
      {
        Hashtable hashtable2 = new Hashtable();
        foreach (KeyValuePair<string, int> keyValuePair in this.m_corpus2Dict)
        {
          if (this.m_model.T_NULL(keyValuePair.Value) > epsilon)
            hashtable2.Add((object) keyValuePair.Key, (object) this.m_model.T_NULL(keyValuePair.Value));
        }
        hashtable1.Add((object) "<NULL>", (object) hashtable2);
      }
      return hashtable1;
    }

    private void Validate()
    {
      if (this.m_sourceFiles == null)
        throw new Exception("You need to set the SourceFiles property.");
      int count1 = this.m_sourceFiles.Count;
      for (int index = 0; index < count1; ++index)
      {
        if (!File.Exists(this.m_sourceFiles[index]))
          throw new Exception(string.Format("Can't open '{0}'", (object) this.m_sourceFiles[index]));
      }
      if (this.m_targetFiles == null)
        throw new Exception("You need to set the TargetFiles property.");
      int count2 = this.m_targetFiles.Count;
      for (int index = 0; index < count2; ++index)
      {
        if (!File.Exists(this.m_targetFiles[index]))
          throw new Exception(string.Format("Can't open '{0}'", (object) this.m_targetFiles[index]));
      }
      if (this.m_sourceFiles.Count != this.m_targetFiles.Count)
        throw new Exception("The number of target files must equal the number of source files.");
      if (this.m_prealignmentFiles != null && this.m_sourceFiles.Count != this.m_prealignmentFiles.Count)
        throw new Exception("If specified, the number of partial alignment files must equal the number of source and target pairs.");
      if (this.m_runList == null)
        throw new Exception("You need to set the RunSpecification property");
    }

    public int[] ConvertAlignmentsToArray(int[] source, int[] target, Alignment[] alignments)
    {
      int[] numArray = new int[target.Length];
      for (int index = 0; index < numArray.Length; ++index)
        numArray[index] = -1;
      if (alignments != null)
      {
        foreach (Alignment alignment in alignments)
        {
          if (alignment.Target < 0 || alignment.Target >= target.Length || alignment.Source < 0 || alignment.Source > source.Length)
            return (int[]) null;
          numArray[alignment.Target] = alignment.Source;
        }
      }
      return numArray;
    }
  }
}
