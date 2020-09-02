// Decompiled with JetBrains decompiler
// Type: Models.TextFileSegmenter
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System.Collections.Generic;
using System.IO;

namespace Models
{
  public class TextFileSegmenter : IFileSegmenter
  {
    private string m_fileName;
    private Dictionary<string, int> m_dict;
    private int m_maxLength;
    private bool m_toLower;

    public TextFileSegmenter()
    {
      this.m_maxLength = 0;
      this.m_toLower = false;
    }

    public string FileName
    {
      set
      {
        this.m_fileName = value;
      }
    }

    public Dictionary<string, int> Dict
    {
      set
      {
        this.m_dict = value;
      }
    }

    public int MaxLength
    {
      get
      {
        return this.m_maxLength;
      }
    }

    public bool LowerCase
    {
      set
      {
        this.m_toLower = value;
      }
    }

    public IEnumerable<int[]> Segments
    {
      get
      {
        List<int[]> numArrayList = new List<int[]>();
        using (StreamReader streamReader = new StreamReader((Stream) File.Open(this.m_fileName, FileMode.Open)))
        {
          int num = 1;
          string str;
          while ((str = streamReader.ReadLine()) != null)
          {
            int[] numArray;
            if (str == "")
            {
              numArray = new int[0];
            }
            else
            {
              string[] strArray = str.Split((char[]) null);
              numArray = new int[strArray.Length];
              for (int index = 0; index < strArray.Length; ++index)
              {
                string lower = strArray[index];
                if (this.m_toLower)
                  lower = lower.ToLower();
                int count;
                if (!this.m_dict.TryGetValue(lower, out count))
                {
                  count = this.m_dict.Count;
                  this.m_dict.Add(lower, count);
                }
                numArray[index] = count;
              }
              if (strArray.Length > this.m_maxLength)
                this.m_maxLength = strArray.Length;
            }
            numArrayList.Add(numArray);
            ++num;
          }
        }
        return (IEnumerable<int[]>) numArrayList;
      }
    }
  }
}
