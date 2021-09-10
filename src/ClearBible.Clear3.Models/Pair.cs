// Decompiled with JetBrains decompiler
// Type: Models.Pair
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

namespace Models
{
  internal class Pair
  {
    private int m_e;
    private int m_f;

    public Pair(int j, int i)
    {
      this.m_e = j;
      this.m_f = i;
    }

    public int e
    {
      get
      {
        return this.m_e;
      }
    }

    public int f
    {
      get
      {
        return this.m_f;
      }
    }
  }
}
