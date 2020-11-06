﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using WordInfo = GBI_Aligner.WordInfo;
using Utils = Utilities.Utils;
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
            Dictionary<string, MappedWords> linksTable, // key is source morphId
            ref bool stopped)
        {
            List<MappedWords> linkedSiblings = new List<MappedWords>();

            if (treeNode.Parent == null || treeNode.Parent.Name.LocalName == "Tree") stopped = true;

            while (!stopped && treeNode.Parent != null && linkedSiblings.Count == 0)
            {
                foreach (XElement childNode in treeNode.Parent.Elements())
                {
                    if (childNode != treeNode)
                    {
                        List<XElement> terminals = GetTerminalXmlNodes(childNode);
                        foreach (XElement terminal in terminals)
                        {
                            string morphID = terminal.Attribute("morphId").Value;
                            if (morphID.Length == 11) morphID += "1";
                            if (linksTable.ContainsKey(morphID))
                            {
                                MappedWords map = linksTable[morphID];
                                linkedSiblings.Add(map);
                            }
                        }
                    }
                }

                if (linkedSiblings.Count == 0)
                {
                    linkedSiblings = GetLinkedSiblings(treeNode.Parent, linksTable, ref stopped);
                }

            }

            return linkedSiblings;
        }


        public static MappedWords GetPreNeighbor(MappedWords unLinked, List<MappedWords> linkedSiblings)
        {
            MappedWords preNeighbor = null;

            int startPosition = Int32.Parse(unLinked.SourceNode.BetterTreeNode.Attribute("Start").Value);
            int currDistance = 100;

            foreach (MappedWords map in linkedSiblings)
            {
                int position = Int32.Parse(map.SourceNode.BetterTreeNode.Attribute("End").Value);
                if (position < startPosition)
                {
                    if (preNeighbor == null)
                    {
                        preNeighbor = map;
                        currDistance = startPosition - position;
                    }
                    else if ((startPosition - position) < currDistance)
                    {
                        preNeighbor = map;
                    }
                }
            }

            return preNeighbor;
        }


        public static MappedWords GetPostNeighbor(MappedWords unLinked, List<MappedWords> linkedSiblings)
        {
            MappedWords postNeighbor = null;

            int endPosition = Int32.Parse(unLinked.SourceNode.BetterTreeNode.Attribute("End").Value);

            foreach (MappedWords map in linkedSiblings)
            {
                int position = Int32.Parse(map.SourceNode.BetterTreeNode.Attribute("End").Value);
                if (position > endPosition)
                {
                    postNeighbor = map;
                    break;
                }
            }

            return postNeighbor;
        }
    }
}
