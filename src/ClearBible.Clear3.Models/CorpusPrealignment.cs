// Decompiled with JetBrains decompiler
// Type: Models.CorpusPrealignment
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System.Collections.Generic;

namespace Models
{
  public class CorpusPrealignment
  {
    private IEnumerable<string> m_fileNames;

    public CorpusPrealignment(IEnumerable<string> fileNames)
    {
      this.m_fileNames = fileNames;
    }

    public List<IEnumerable<Alignment[]>> AlignmentLists
    {
      get
      {
        List<IEnumerable<Alignment[]>> alignmentArraysList = new List<IEnumerable<Alignment[]>>();
        foreach (string fileName in this.m_fileNames)
          alignmentArraysList.Add(new FilePrealignment()
          {
            FileName = fileName
          }.AlignmentLists);
        return alignmentArraysList;
      }
    }
  }
}
