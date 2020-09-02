// Decompiled with JetBrains decompiler
// Type: Models.Tabulator
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System;

namespace Models
{
  internal class Tabulator
  {
    private bool m_forUnion;
    private bool m_inUnion;
    private bool m_inSet;
    private double m_wordProb;
    private double m_alignProb;

    public Tabulator(bool forUnion)
    {
      this.m_forUnion = forUnion;
      this.m_inUnion = false;
      this.m_inSet = false;
    }

    public bool InUnion
    {
      get
      {
        return this.m_inUnion;
      }
    }

    public bool InSet
    {
      get
      {
        return this.m_inSet;
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

    public void AddToUnion(double wordProb, double alignProb)
    {
      this.m_inUnion = true;
      if (this.m_forUnion)
      {
        this.m_inSet = true;
        this.m_wordProb = Math.Max(this.m_wordProb, wordProb);
        this.m_alignProb = Math.Max(this.m_alignProb, alignProb);
      }
      else
      {
        this.m_wordProb = Math.Min(this.m_wordProb, wordProb);
        this.m_alignProb = Math.Min(this.m_alignProb, alignProb);
      }
    }

    public void MarkInSet()
    {
      this.m_inSet = true;
    }
  }
}
