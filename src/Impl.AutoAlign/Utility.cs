﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using WordInfo = GBI_Aligner.WordInfo;
using MappedWords = GBI_Aligner.MappedWords;


namespace ClearBible.Clear3.Impl.AutoAlign
{
    using ClearBible.Clear3.API;
    using ClearBible.Clear3.Impl.Data;
    using ClearBible.Clear3.Impl.TreeService;
    using ClearBible.Clear3.Miscellaneous;

    public class AutoAlignUtility
    {
        public static Dictionary<string, WordInfo> BuildWordInfoTable(
            XElement tree)
        {           
            return
                GetTerminalXmlNodes(tree)
                .ToDictionary(
                    node => GetSourceIdFromTerminalXmlNode(node),
                    node => GetWordInfoFromTerminalXmlNode(node));
        }

        public static List<XElement> GetTerminalXmlNodes(XElement treeNode)
        {
            return treeNode
                .Descendants()
                .Where(e => e.FirstNode is XText)
                .ToList();
        }

        public static string GetSourceIdFromTerminalXmlNode(XElement node)
        {
            string sourceId = node.Attribute("morphId").Value;
            if (sourceId.Length == 11) sourceId += "1";
            return sourceId;
        }

        public static WordInfo GetWordInfoFromTerminalXmlNode(XElement node)
        {
            string language = node.Attribute("Language").Value;

            string type =
                node.AttrAsString(language == "G" ? "Type" : "NounType");

            string category = node.Attribute("Cat").Value;
            if (category == "noun" && type == "Proper")
                category = "Name";

            return new WordInfo()
            {
                Lang = language,
                Strong = node.Attribute("StrongNumberX").Value,
                Surface = node.Attribute("Unicode").Value,
                Lemma = node.Attribute("UnicodeLemma").Value,
                Cat = category,
                Morph = node.Attribute("Analysis").Value
            };               
        }


        public static List<MappedWords> GetLinkedSiblings(
            XElement treeNode,
            Dictionary<string, MappedWords> linksTable)
        {
            if (treeNode.Parent != null &&
                treeNode.Parent.Name.LocalName != "Tree")
            {
                List<MappedWords> linkedSiblings =
                    treeNode.Parent.Elements()
                    .Where(child => child != treeNode)
                    .SelectMany(child => GetTerminalXmlNodes(child))
                    .Select(term => term.SourceId())
                    .Select(sourceId => linksTable.GetValueOrDefault(sourceId))
                    .Where(x => !(x is null))
                    .ToList();

                if (linkedSiblings.Count == 0)
                {
                    return GetLinkedSiblings(treeNode.Parent, linksTable);
                }
                else
                {
                    return linkedSiblings;
                }
            }
            else
            {
                return new List<MappedWords>();
            }          
        }


        public static MappedWords GetPreNeighbor(MappedWords unLinked, List<MappedWords> linkedSiblings)
        {
            int limit = unLinked.SourceNode.BetterTreeNode.AttrAsInt("Start");

            int end(MappedWords mw) =>
                mw.SourceNode.BetterTreeNode.AttrAsInt("End");

            return
                linkedSiblings
                .Select(mw => new { mw, distance = limit - end(mw) })
                .Where(x => x.distance > 0)
                .OrderBy(x => x.distance)
                .Select(x => x.mw)
                .FirstOrDefault();
        }


        public static MappedWords GetPostNeighbor(MappedWords unLinked, List<MappedWords> linkedSiblings)
        {
            int limit = unLinked.SourceNode.BetterTreeNode.AttrAsInt("End");

            int end(MappedWords mw) =>
                mw.SourceNode.BetterTreeNode.AttrAsInt("End");

            return
                linkedSiblings
                .Where(mw => end(mw) > limit)
                .FirstOrDefault();
        }
    }
}
