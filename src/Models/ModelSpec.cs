// Decompiled with JetBrains decompiler
// Type: Models.ModelSpec
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

namespace Models
{
  public class ModelSpec
  {
    private Model m_model;
    private int m_runCount;

    public ModelSpec(Model model, int runCount)
    {
      this.m_model = model;
      this.m_runCount = runCount;
    }

    public Model Model
    {
      get
      {
        return this.m_model;
      }
    }

    public int RunCount
    {
      get
      {
        return this.m_runCount;
      }
    }
  }
}
