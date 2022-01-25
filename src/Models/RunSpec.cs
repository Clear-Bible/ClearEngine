// Decompiled with JetBrains decompiler
// Type: Models.RunSpec
// Assembly: Models, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 13215851-0DC5-4000-8242-8696083F92E8
// Assembly location: C:\Users\tim\work\GBI\CLEAR\TimClear2\ExternalDlls\Models.dll

using System.Collections.Generic;

namespace Models
{
  public class RunSpec
  {
    public static List<ModelSpec> ParseModelList(string spec)
    {
      List<ModelSpec> modelSpecList = new List<ModelSpec>();
      string[] strArray1 = spec.Split(';');
      if (strArray1.Length == 0)
        return (List<ModelSpec>) null;
      foreach (string str in strArray1)
      {
        char[] chArray = new char[1]{ ':' };
        string[] strArray2 = str.Split(chArray);
        int result;
        if (strArray2.Length != 2 || !int.TryParse(strArray2[1], out result) || result <= 0)
          return (List<ModelSpec>) null;
        Model model;
        switch (strArray2[0].ToUpper())
        {
          case "1":
          case "IBM1":
          case "MODEL1":
            model = Model.Model1;
            break;
          case "2":
          case "IBM2":
          case "MODEL2":
            model = Model.Model2;
            break;
          case "3":
          case "IBM3":
          case "MODEL3":
            model = Model.Model3;
            break;
          case "H":
          case "HMM":
            model = Model.HMM;
            break;
          case "F":
          case "FASTALIGN":
            model = Model.FastAlign;
            break;
          default:
            return (List<ModelSpec>) null;
        }
        modelSpecList.Insert(0, new ModelSpec(model, result));
      }
      return modelSpecList;
    }

    public static List<ModelSpec> ParseMachineModelList(string spec)
    {
      List<ModelSpec> modelSpecList = new List<ModelSpec>();
      string[] strArray1 = spec.Split(';');
      if (strArray1.Length == 0)
        return (List<ModelSpec>)null;
      foreach (string str in strArray1)
      {

        char[] chArray = new char[1] { ':' };
        string[] strArray2 = str.Split(chArray);
        // int result;
        // if (strArray2.Length != 2 || !int.TryParse(strArray2[1], out result) || result <= 0)
        //   return (List<ModelSpec>)null;
        int result = 1;
        Model model;
        switch (strArray2[0].ToUpper())
        {
          case "1":
          case "IBM1":
          case "MODEL1":
            model = Model.Model1;
            break;
          case "2":
          case "IBM2":
          case "MODEL2":
            model = Model.Model2;
            break;
          case "H":
          case "HMM":
            model = Model.HMM;
            break;
          case "F":
          case "FASTALIGN":
            model = Model.FastAlign;
            break;
          default:
            return (List<ModelSpec>)null;
        }
        modelSpecList.Insert(0, new ModelSpec(model, result));
      }
      return modelSpecList;
    }

    public static SymmetrizationType ParseSymmetrization(string spec)
    {
      switch (spec.ToUpper())
      {
        case "":
          return SymmetrizationType.None;
        case "MIN":
          return SymmetrizationType.Min;
        case "DIAG":
          return SymmetrizationType.Diag;
        case "MAX":
          return SymmetrizationType.Max;
        default:
          return SymmetrizationType.Null;
      }
    }
  }
}
