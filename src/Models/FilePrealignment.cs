// Decompiled with JetBrains decompiler
// Type: Models.FilePrealignment
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;
using System.Collections.Generic;
using System.IO;

namespace Models
{
  internal class FilePrealignment
  {
    private string m_fileName;

    public string FileName
    {
      set
      {
        this.m_fileName = value;
      }
    }

    public IEnumerable<Alignment[]> AlignmentLists
    {
      get
      {
        List<Alignment[]> alignmentArrayList = new List<Alignment[]>();
        using (StreamReader streamReader = new StreamReader((Stream) File.Open(this.m_fileName, FileMode.Open)))
        {
          int num = 1;
          string str;
          while ((str = streamReader.ReadLine()) != null)
          {
            Alignment[] alignmentArray;
            if (str == "")
            {
              alignmentArray = new Alignment[0];
            }
            else
            {
              string[] strArray1 = str.Split((char[]) null);
              alignmentArray = new Alignment[strArray1.Length];
              for (int index = 0; index < strArray1.Length; ++index)
              {
                string[] strArray2 = strArray1[index].Split('-');
                int result1;
                int result2;
                if (strArray2.Length != 2 || !int.TryParse(strArray2[0], out result1) || !int.TryParse(strArray2[1], out result2))
                  throw new Exception(string.Format("File '{0}, Line {1}: can't parse alignment '{2}'", (object) this.m_fileName, (object) num, (object) strArray1[index]));
                alignmentArray[index] = new Alignment(result1, result2, 1.0, 1.0);
              }
            }
            alignmentArrayList.Add(alignmentArray);
            ++num;
          }
        }
        return (IEnumerable<Alignment[]>) alignmentArrayList;
      }
    }
  }
}
