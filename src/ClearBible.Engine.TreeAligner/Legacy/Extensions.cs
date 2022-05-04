using ClearBible.Engine.Corpora;
using ClearBible.Engine.Exceptions;
using System;
using System.Xml.Linq;

namespace ClearBible.Engine.TreeAligner.Legacy
{
    public static class Extensions
    {
        #region Node element atributes
        public static int Start(this XElement node)
        {
            bool successful = int.TryParse(
                node.Attribute("Start")?.Value ?? throw new InvalidTreeEngineException($"textNode missing attribute.", new Dictionary<string, string>
                {
                            {"nodeId", node.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "Start" }
                }), out int endInt);
            if (!successful)
            {
                throw new InvalidTreeEngineException($"textNode attribute not parsable to int.", new Dictionary<string, string>
                {
                            {"nodeId", node.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "Start" },
                            {"value", node.Attribute("Start")?.Value ?? "<Start attribute also missing>"}
                });
            }
            else
            {
                return endInt;
            }
        }
        public static int End(this XElement node)
        {
            bool successful = int.TryParse(
                node.Attribute("End")?.Value ?? throw new InvalidTreeEngineException($"textNode missing attribute.", new Dictionary<string, string>
                {
                            {"nodeId", node.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "End" }
                }), out int endInt);
            if (!successful)
            {
                throw new InvalidTreeEngineException($"textNode attribute not parsable to int.", new Dictionary<string, string>
                {
                            {"nodeId", node.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "End" },
                            {"value", node.Attribute("End")?.Value ?? "<End attribute also missing>"}
                });
            }
            else
            {
                return endInt;
            }
        }
        public static TreeNodeStackID TreeNodeStackID(this XElement node)
        {
            string nodeIdString = node.NodeId() ?? throw new InvalidTreeEngineException($"textNode missing attribute.", new Dictionary<string, string>
            {
                    {"xelement node name", node.Name.LocalName },
                    {"attribute", "nodeId" }
            });
            if (nodeIdString.Length < 14)
            {
                throw new InvalidTreeEngineException($"textNode attribute value incorrect length.", new Dictionary<string, string>
                {
                        {"xelement node name", node.Name.LocalName },
                        {"attribute", "nodeId" },
                        {"value", nodeIdString },
                        {"requiredLength", "at least 14 characters" },
                });
            }
            var treeNodeStackIdString = nodeIdString.Substring(0, 14);
            return new TreeNodeStackID(treeNodeStackIdString);
        }
        #endregion

        #region Node that has XText ('leaf' or 'terminal' node) attributes
        public static SourceID SourceID(this XElement term)
        {
            return new SourceID(term.MorphId());
        }

        #endregion
    }
}
