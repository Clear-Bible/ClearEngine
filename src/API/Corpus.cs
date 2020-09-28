using System;
using System.Collections.Generic;

namespace ClearBible.Clear3.API
{
    public interface Corpus
    {
        string Key { get; }

        IEnumerable<SegmentInstance> SegmentsForZone(Zone zone);

        SegmentInstance SegmentsForPlace(Place place);

        IEnumerable<SegmentInstance> SegmentsForPlaceSet(PlaceSet placeSet);

        RelativePlace RelativePlace(Place place);

        IEnumerable<Zone> AllZones();

        Corpus AddZone(Zone zone, IEnumerable<string> segments);



        //void AddZone(Zone zone);

        //void PutText(Zone zone, int index, string text);




        //Corpus Map(Func<Token, string> mappingFunction);

        //Corpus Filter(Func<Token, bool> filterFunction);





        //Token Token(Zone zone, int index);

        
        //IEnumerable<Token> TokensForZoneRange(ZoneRange zoneRange);

        //IEnumerable<Token> TokensForZone(Zone zone);

        //IEnumerable<Token> Find(string tokenText);




        
    }


    /// <summary>
    /// An example of a RelativePlace is a datum that means
    /// "the second occurrence of 'word' in John 1:1".
    /// </summary>
    /// 
    public interface RelativePlace
    {
        string Key { get; }

        Zone Zone { get; }

        string Text { get; }

        int Occurrence { get; }
    }


    public interface SegmentInstance
    {
        string Key { get; }

        string Text { get; }

        Place Place { get; }
    }



    //public interface Token
    //{
    //    Guid Id { get; }

    //    Guid Corpus { get; }

    //    Zone Zone { get; }

    //    int IndexInZone { get; }

    //    string Text { get; }

    //    int TextIndexInZone { get; }
    //}


    //public interface Alignment
    //{
    //    IEnumerable<Matching> Matchings { get; }
    //}


    //public interface Matching
    //{
    //    IEnumerable<Token> SourceTokens { get; }

    //    IEnumerable<Token> TargetTokens { get; }

    //    MatchingKind Kind { get; }
    //}


    public enum MatchingKind
    {
        Auto,
        Manual
    }
}
