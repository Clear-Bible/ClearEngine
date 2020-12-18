using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ClearBible.Clear3.API;
using ClearBible.Clear3.Impl.TreeService;
using System.Linq.Expressions;

namespace ClearBible.Clear3.Impl.AutoAlign
{
    // Internal Data Model for Tree-based Auto Alignment Algorithm
    //------------------------------------------------------------


    /// <summary>
    /// Something that might be a TargetPoint or might
    /// be nothing.
    /// </summary>
    /// 
    public record MaybeTargetPoint(
        TargetPoint TargetPoint)
    {
        public string ID =>
            TargetPoint?.TargetID.AsCanonicalString ?? "0";

        public string AltID =>
            TargetPoint?.AltID ?? "";

        public string Lower =>
            TargetPoint?.Lower ?? "";

        public string Text =>
            TargetPoint?.Text ?? "";

        public int Position =>
            TargetPoint?.Position ?? -1;

        public bool IsNothing =>
            TargetPoint == null;

        public double RelativePos =>
            TargetPoint?.RelativePosition ?? 0.0;
    }


    /// <summary>
    /// A version of TargetBond that might not have a TargetPoint.
    /// </summary>
    /// 
    public record OpenTargetBond(
        MaybeTargetPoint MaybeTargetPoint,
        double Score)
    {
        public bool HasTargetPoint => !MaybeTargetPoint.IsNothing;
    }


    /// <summary>
    /// A version of MonoLink with an OpenTargetBond instead of a
    /// TargetBond, and whose OpenTargetBond property can be reset.
    /// </summary>
    /// 
    public class OpenMonoLink
    {
        public SourcePoint SourcePoint { get; }
        public OpenTargetBond OpenTargetBond { get; private set; }

        public bool HasTargetPoint =>
            OpenTargetBond.HasTargetPoint;

        public OpenMonoLink(
            SourcePoint sourcePoint,
            OpenTargetBond openTargetBond)
        {
            SourcePoint = sourcePoint;
            OpenTargetBond = openTargetBond;
        }

        public void ResetOpenTargetBond(OpenTargetBond bond)
        {
            OpenTargetBond = bond;
        }           
    }    
}
