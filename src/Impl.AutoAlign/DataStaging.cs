using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using System.Net.Http.Headers;
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.TreeService;


    public record SourcePoint(
        string Lemma,
        XElement Terminal,
        SourceID SourceID,
        string AltID,
        int TreePosition,
        double RelativeTreePosition,
        int SourcePosition);


    public record TargetPoint(
        string Text,
        string Lower,
        TargetID TargetID,
        string AltID,
        int Position,
        double RelativePosition);


    public record TargetBond(
        TargetPoint TargetPoint,
        double Score);
        // FIXME: Someday may also need to track why a bond was made.


    public record MonoLink(
        SourcePoint SourcePoint,
        TargetBond TargetBond);


    public record MultiLink(
        List<SourcePoint> Sources,
        List<TargetBond> Targets);
    
    


    public class MaybeTargetPoint
    {
        public MaybeTargetPoint(TargetPoint targetPoint)
        {
            TargetPoint = targetPoint;
        }

        public MaybeTargetPoint()
        {
            TargetPoint = null;
        }

        public TargetPoint TargetPoint { get; }

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



    public record OpenTargetBond(
        MaybeTargetPoint MaybeTargetPoint,
        double Score)
    {
        public bool HasTargetPoint => !MaybeTargetPoint.IsNothing;
    }





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



    public class SourceWord
    {
        public string ID { get; set; }
        public string AltID { get; set; }
        public string Text { get; set; }
        public string Lemma { get; set; }
        public string Strong { get; set; }
    }

    public class MappedGroup
    {
        public List<SourcePoint> SourcePoints = new List<SourcePoint>();
        public List<OpenTargetBond> TargetNodes = new List<OpenTargetBond>();
    }


    public class Candidate
    {
        public CandidateChain Chain;
        public double Prob;

        public Candidate()
        {
            Chain = new CandidateChain();
        }

        public Candidate(MaybeTargetPoint tw, double probability)
        {
            Chain = new CandidateChain(Enumerable.Repeat(tw, 1));
            Prob = probability;
        }

        public Candidate(CandidateChain chain, double probability)
        {
            Chain = chain;
            Prob = probability;
        }
    }

    /// <summary>
    /// A CandidateChain is a sequence of TargetWord objects
    /// or a sequence of Candidate objects.
    /// </summary>
    /// 
    public class CandidateChain : ArrayList
    {
        public CandidateChain()
            : base()
        {
        }

        public CandidateChain(IEnumerable<Candidate> candidates)
            : base(candidates.ToList())
        {
        }

        public CandidateChain(IEnumerable<MaybeTargetPoint> targetWords)
            : base(targetWords.ToList())
        {
        }
    }


    /// <summary>
    /// An AlternativeCandidates object is a list of Candidate
    /// objects that are alternatives to one another.
    /// </summary>
    /// 
    public class AlternativeCandidates : List<Candidate>
    {
        public AlternativeCandidates()
            : base()
        {
        }

        public AlternativeCandidates(IEnumerable<Candidate> candidates)
            : base(candidates)
        {
        }
    }


    /// <summary>
    /// An AlternativesForTerminals object is a mapping:
    /// SourceWord.ID => AlternativeCandidates.
    /// </summary>
    /// 
    public class AlternativesForTerminals : Dictionary<string, List<Candidate>>
    {
        public AlternativesForTerminals()
            : base()
        {
        }
    }


    

    public class ManuscriptWord
    {
        public long id;
        public string altId;
        public string text;
        public string strong;
        public string gloss;
        public string gloss2;
        public string lemma;
        public string pos;
        public string morph;
    }

    public class TranslationWord
    {
        public long id;
        public string altId;
        public string text;
    }

    public class Manuscript
    {
        public ManuscriptWord[] words;
    }

    public class Translation
    {
        public TranslationWord[] words;
    }

    public class Link
    {
        public int[] source;
        public int[] target;
        public double? cscore;

    }

    public class Line
    {
        public Manuscript manuscript;
        public Translation translation;

        //public int[][][] links;
        [JsonConverter(typeof(LinkJsonConverter))]
        public List<Link> links;
    }

    public class Alignment2
    {
        public Line[] Lines;
    }

    public class Line3
    {
        public Manuscript manuscript;
        public Translation gtranslation;
        public int[][][] glinks;

        public Translation translation;
        //public int[][][] links;

        [JsonConverter(typeof(LinkJsonConverter))]
        public List<Link> links;
    }

   

    public class LinkJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Link).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var links = new List<Link>();

            if (reader.TokenType == JsonToken.Null)
            {
                return links;
            }
            else
            {
                JArray array = JArray.Load(reader);
                var linksobj = array.ToObject<IList<dynamic>>();
                foreach (var linkobj in linksobj)
                {
                    int[] source = JsonConvert.DeserializeObject<int[]>(linkobj[0].ToString());
                    int[] target = JsonConvert.DeserializeObject<int[]>(linkobj[1].ToString());
                    double? cscore = null;
                    if (linkobj.Count >= 3)
                    {
                        dynamic attr = JsonConvert.DeserializeObject<dynamic>(linkobj[2].ToString());
                        cscore = attr.cscore;
                    }

                    links.Add(new Link() { source = source, target = target, cscore = cscore });
                }
            }

            return links;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var links = value as List<Link>;
            var linksobj = new List<dynamic>();

            foreach (var link in links)
            {
                var linkobj = new List<dynamic>();
                linkobj.Add(link.source);
                linkobj.Add(link.target);
                linkobj.Add(new Dictionary<string, object>(){
                    {"cscore", link.cscore}
                });

                linksobj.Add(linkobj);
            }

            JToken t = JToken.FromObject(linksobj);
            t.WriteTo(writer);
        }
    }

    public class WordInfo
    {
        public string Lang;
        public string Strong;
        public string Surface;
        public string Lemma;
        public string Cat;
        public string Morph;
    }
}
