// Decompiled with JetBrains decompiler
// Type: Models.CorpusSegmenter`1
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System.Collections.Generic;

namespace Models
{
  public class CorpusSegmenter<FileSegmenter> where FileSegmenter : IFileSegmenter, new()
  {
    private IEnumerable<string> m_fileNames;
    private Dictionary<string, int> m_dict;
    private int m_maxLength;
    private bool m_toLower;

    public CorpusSegmenter(IEnumerable<string> fileNames, Dictionary<string, int> dict)
    {
      this.m_fileNames = fileNames;
      this.m_dict = dict;
      this.m_maxLength = 0;
    }

    public bool LowerCase
    {
      set
      {
        this.m_toLower = value;
      }
    }

    public List<IEnumerable<int[]>> SegmentLists
    {
      get
      {
        List<IEnumerable<int[]>> numArraysList = new List<IEnumerable<int[]>>();
        foreach (string fileName in this.m_fileNames)
        {
          FileSegmenter fileSegmenter = new FileSegmenter();
          fileSegmenter.LowerCase = this.m_toLower;
          fileSegmenter.FileName = fileName;
          fileSegmenter.Dict = this.m_dict;
          numArraysList.Add(fileSegmenter.Segments);
          if (fileSegmenter.MaxLength > this.m_maxLength)
            this.m_maxLength = fileSegmenter.MaxLength;
        }
        return numArraysList;
      }
    }

    public int MaxLength
    {
      get
      {
        return this.m_maxLength;
      }
    }
  }
}
