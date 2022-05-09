using System.Xml.Linq;

using ClearBible.Engine.Exceptions;
using ClearBible.Engine.SyntaxTree.Corpora;

namespace ClearBible.Engine.SyntaxTree.Aligner.Corpora
{
    public static class SyntaxTreeExtensions
    {
        public static int Start(this XElement leaf)
        {
            bool successful = int.TryParse(
                leaf.Attribute("Start")?.Value ?? throw new InvalidTreeEngineException($"leaf missing attribute.", new Dictionary<string, string>
                {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "Start" }
                }), out int endInt);
            if (!successful)
            {
                throw new InvalidTreeEngineException($"leaf attribute not parsable to int.", new Dictionary<string, string>
                {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "Start" },
                            {"value", leaf.Attribute("Start")?.Value ?? "<Start attribute also missing>"}
                });
            }
            else
            {
                return endInt;
            }
        }
        public static int End(this XElement leaf)
        {
            bool successful = int.TryParse(
                leaf.Attribute("End")?.Value ?? throw new InvalidTreeEngineException($"leaf missing attribute.", new Dictionary<string, string>
                {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "End" }
                }), out int endInt);
            if (!successful)
            {
                throw new InvalidTreeEngineException($"attribute not parsable to int.", new Dictionary<string, string>
                {
                            {"nodeId", leaf.NodeId() ?? "<nodeId attribute also missing>"},
                            {"attribute", "End" },
                            {"value", leaf.Attribute("End")?.Value ?? "<End attribute also missing>"}
                });
            }
            else
            {
                return endInt;
            }
        }

        public static string NodeStackID(this XElement node)
        {
            string nodeIdString = node.NodeId() ?? throw new InvalidTreeEngineException($"node missing attribute.", new Dictionary<string, string>
            {
                    {"xelement node name", node.Name.LocalName },
                    {"attribute", "nodeId" }
            });
            if (nodeIdString.Length < 14)
            {
                throw new InvalidTreeEngineException($"node attribute value incorrect length.", new Dictionary<string, string>
                {
                        {"xelement node name", node.Name.LocalName },
                        {"attribute", "nodeId" },
                        {"value", nodeIdString },
                        {"requiredLength", "at least 14 characters" },
                });
            }
            return nodeIdString.Substring(0, 14);
        }
    }
}
