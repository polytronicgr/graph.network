using graph.network.core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace graph.network.ld
{
    public class PredicateNet : LinkedDataNet
    {
        public string Prefix { get; }
        public PredicateNet(string prefix, Uri ontology, Action<Uri, GraphNet> loadEdges = null) : base(ontology.ToString())
        {
            Prefixes.Add(prefix, ontology.ToString());
            Add(this, "prefix", prefix);

            if (loadEdges == null)
            {
                //call uri and load
            }
            else
            {
                loadEdges(ontology, this);
            }
            var predicates = AllEdges()
                .Where(e => e.Predicate.Value.ToString() == "http://www.w3.org/2000/01/rdf-schema#domain")
                .Select(e => e.Subject);
            Outputs.AddRange(predicates);
            Prefix = prefix;
        }

        public override void TrainFromQueries(params string[] queries)
        {
            InParseMode = false;
            var examples = new List<NodeExample>();
            var queryId = 0;
            foreach (var query in queries)
            {
                queryId++;
                var words = query.Split(' ');


                var beforeLastWord = Node("");
                var lastWord = "";
                foreach (var w in words)
                {
                    var word = w.StartsWith("?") ? queryId + "." + w : w;
                    var node = Node(word);
                    node.UseEdgesAsInterface = false;
                    if (word.Contains(Prefix) && Outputs.Contains(node))
                    {
                        var last = Node(lastWord);
                        examples.Add(new NodeExample(last, node));
                    }
                    if (beforeLastWord.ShortId.Contains("?") && ( lastWord == "a" || lastWord.Contains(Prefix)))
                    {
                        beforeLastWord.AddEdge(Node(lastWord), node);
                    }
                    
                    beforeLastWord = Node(lastWord);
                    lastWord = word;
                }

            }

            Train(examples.ToArray());

            InParseMode = true;
        }
    }
}