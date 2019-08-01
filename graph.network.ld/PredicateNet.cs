using graph.network.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;

namespace graph.network.ld
{
    public class PredicateNet : LinkedDataNet
    {
        public string Prefix { get; }
        public PredicateNet(string prefix, Uri ontology, Action<Uri, GraphNet> loadEdges = null) : base(IRI_PREFIX + prefix)
        {
            Prefixes.Add(prefix, ontology.ToString());
            Prefix = prefix;
            Add(this, Prefix, GetPrefixNamespaceNode());

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
                .Select(e => e.Subject)
                .Distinct();
            Outputs.AddRange(predicates);


        }

        public Node GetPrefixNamespaceNode()
        {
            return Node(IRI_PREFIX + Prefix.Replace(":", "_namespace"));
        }

        public override string ShortId
        {
            get
            {
                return Prefix;
            }
        }

        public override void TrainFromQueries(params string[] queries)
        {
            InParseMode = false;
            var examples = new List<NodeExample>();
            var queryId = 0;
            foreach (var query in queries)
            {
                queryId++;
                var tokeniser = new SparqlTokeniser(ParsingTextReader.Create(new StringReader(query)), SparqlQuerySyntax.Sparql_1_1);
                var token = tokeniser.GetNextToken();
                IToken beforeLastToken = null;
                IToken lastToken = null;

                while (token != null && !(token is EOFToken))
                {
                    token = tokeniser.GetNextToken();
                    var tokenValue = token is VariableToken ? queryId + "." + token.Value : token.Value;
                    var node = Node(tokenValue);
                    node.UseEdgesAsInterface = false;
                    if (tokenValue.Contains(Prefix) && Outputs.Contains(node))
                    {
                        var last = Node(queryId + "." + lastToken.Value);
                        examples.Add(new NodeExample(last, node));
                    }
                    if (beforeLastToken is VariableToken && (lastToken.Value.Contains("a") || lastToken.Value.Contains(Prefix)))
                    {
                        var n = Node(queryId + "." + beforeLastToken.Value);
                       n.AddEdge(Node("a"), node);
                    }

                    beforeLastToken = lastToken;
                    lastToken = token;
                }
            }

            Train(examples.ToArray());

            InParseMode = true;
        }
    }
}