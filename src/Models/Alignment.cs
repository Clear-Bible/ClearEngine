// Decompiled with JetBrains decompiler
// Type: Models.Alignment
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

namespace Models
{
  public class Alignment
  {
    private int m_source;
    private int m_target;
    private double m_wordProb;
    private double m_alignProb;
    private bool m_fAlignProb;

    public Alignment(int source, int target, double wordProb, double alignProb)
    {
      this.m_source = source;
      this.m_target = target;
      this.m_wordProb = wordProb;
      this.m_alignProb = alignProb;
      this.m_fAlignProb = true;
    }

    public Alignment(int source, int target, double wordProb)
    {
      this.m_source = source;
      this.m_target = target;
      this.m_wordProb = wordProb;
      this.m_fAlignProb = false;
    }

    public int Source
    {
      get
      {
        return this.m_source;
      }
    }

    public int Target
    {
      get
      {
        return this.m_target;
      }
    }

    public double WordProb
    {
      get
      {
        return this.m_wordProb;
      }
    }

    public double AlignProb
    {
      get
      {
        return this.m_alignProb;
      }
    }

    public bool HasAlignProb
    {
      get
      {
        return this.m_fAlignProb;
      }
    }
  }
}
