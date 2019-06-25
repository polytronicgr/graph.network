using graph.network.core;
using System;
using System.Collections.Generic;
using System.IO;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;

namespace graph.network.ld
{
    public class LinkedDataNet : GraphNet
    {
        public static readonly string IRI_PREFIX = "http://graph.network.com/ld/";

        public bool InParseMode { get; set; }
        public Dictionary<string, string> Prefixes { get; set; } = new Dictionary<string, string>();

        public LinkedDataNet(string name, int maxPathLenght = 20, int maxNumberOfPaths = 10) : base(name, maxPathLenght, maxNumberOfPaths)
        {
        }

        public static string R(string value)
        {
            return IRI_PREFIX + value;
        }

        public virtual void TrainFromQueries(params string[] queries)
        {
            InParseMode = false;
            var examples = new List<NodeExample>();

            foreach (var query in queries)
            {
                var tokeniser = new SparqlTokeniser(ParsingTextReader.Create(new StringReader(query)), SparqlQuerySyntax.Sparql_1_1);


                var lastWord = Node("");
                var token = tokeniser.GetNextToken();
                while (token != null && !(token is EOFToken))
                {
                    token = tokeniser.GetNextToken();
                    var word = token.Value.ToLower();
                    var node = Node(word);
                    examples.Add(new NodeExample(Node(lastWord), node));
                    lastWord = node;
                }
            }

            Train(examples.ToArray());

            InParseMode = true;
        }

        public override Node Node(object value)
        {
            var node = value as Node;
            if (node == null)
            {
                var str = value.ToString();
                str = ToFullUri(str);
                var iri = str.StartsWith("http") ? str : R(str);
                return base.Node(iri);
            }
            else
            {
                return base.Node(node);
            }
        }

        public string ToFullUri(string uri)
        {
            foreach (var prefix in Prefixes)
            {
                var fullPrefix = prefix.Key.TrimEnd(':') + ":";
                if (uri.StartsWith(fullPrefix))
                {
                    uri = uri.Replace(fullPrefix, prefix.Value);
                }
            }

            return uri;
        }

        protected override Node NewNode(object value)
        {
            if (InParseMode)
            {
                return new TextMatchingNode(value.ToString());
            }
            else
            {
                return new UriNode(value.ToString());
            }

        }
    }
}
