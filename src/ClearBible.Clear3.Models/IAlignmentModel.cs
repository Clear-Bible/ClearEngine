// Decompiled with JetBrains decompiler
// Type: Models.IAlignmentModel
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System.Collections.Generic;

namespace Models
{
  public interface IAlignmentModel
  {
    bool ConfigureRun(List<ModelSpec> runList);

    void AddSegmentPair(int[] source, int[] target, int[] fixedAlignments);

    void EndPass();

    List<Alignment> GetAlignments(int[] source, int[] target, int[] fixedAlignments);

    int MaxIterations { get; }

    bool NeedsMoreTraining { get; }

    string Phase { get; }

    double MaxDelta { get; }

    string Summary { get; }

    bool HasNullSourceWord { get; }

    double T(int e, int f);

    double T_NULL(int f);
  }
}
