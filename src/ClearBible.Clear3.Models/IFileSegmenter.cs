// Decompiled with JetBrains decompiler
// Type: Models.IFileSegmenter
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System.Collections.Generic;

namespace Models
{
  public interface IFileSegmenter
  {
    bool LowerCase { set; }

    string FileName { set; }

    Dictionary<string, int> Dict { set; }

    IEnumerable<int[]> Segments { get; }

    int MaxLength { get; }
  }
}
