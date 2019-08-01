using graph.network.core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;

namespace graph.network.ld
{
    public class QueryGraphNet : LinkedDataNet
    {
        private readonly List<TokenNode> nodes = new List<TokenNode>();
        private string queryText;
        public Action<Uri, GraphNet> PrefixLoader { get; set; }

        public QueryGraphNet(string name, int maxPathLenght = 20, int maxNumberOfPaths = 20) : base(name, maxPathLenght, maxNumberOfPaths)
        {
            Add(new SparqlSyntaxNet(), true);
        }

        public string QueryText { get { return queryText; } set { queryText = value; ParseQuery(value); } }

        public Exception CurrentError { get; private set; }

        public override void TrainFromQueries(params string[] queries)
        {
            //TODO: use the ParseQuery method here somehow as they are much the same
            InParseMode = false;
            var examples = new List<Example>();

            foreach (var query in queries)
            {
                var tokeniser = new SparqlTokeniser(ParsingTextReader.Create(new StringReader(query)), SparqlQuerySyntax.Sparql_1_1);
                var token = tokeniser.GetNextToken();
                PredicateNet currentPrefix = null;
                while (token != null && !(token is EOFToken))
                {
                    token = tokeniser.GetNextToken();
                    if (token is PrefixToken)
                    {
                        var prefix = token.Value;
                        var ontology = new Uri(tokeniser.GetNextToken().Value);
                        currentPrefix = new PredicateNet(prefix, ontology, this.PrefixLoader);
                        Add(currentPrefix, true);
                    }
                    else
                    {
                        var w = token.Value.ToLower();
                        var words = w.Split(':');
                        var wordSuffix = words.Length > 1 ? ":" : "";
                        var offset = 0;
                        foreach (var word in words)
                        {
                            var fullWord = word + wordSuffix;
                            var node = Node(fullWord);
                            wordSuffix = "";
                            if (query.Trim().Length > token.StartPosition)
                            {
                                var partQuery = query.Substring(0, (token.StartPosition - 1) + offset).Trim();
                                var inputNode = Node(partQuery);
                                examples.Add(new Example(inputNode, fullWord));
                                offset = fullWord.Length;
                            }
                        }
                    }
                }
            }

            Train(examples.ToArray());
            InParseMode = true;
        }

        private void ParseQuery(string query)
        {
            nodes.Clear();
            var tokeniser = new SparqlTokeniser(ParsingTextReader.Create(new StringReader(query)), SparqlQuerySyntax.Sparql_1_1);
            var token = tokeniser.GetNextToken();
            while (token != null && !(token is EOFToken))
            {  try
                {
                    token = tokeniser.GetNextToken();
                    if (token is PrefixDirectiveToken)
                    {
                        token = tokeniser.GetNextToken();
                        var prefix = token.Value;
                        var ontology = new Uri(tokeniser.GetNextToken().Value);
                        var prefixNode = new PredicateNet(prefix, ontology, this.PrefixLoader);
                        Add(prefixNode, true);
                        var linkToPrefix = new TokenNode(new Uri(R(token.Value.ToLower())), token);
                        nodes.Add(linkToPrefix);
                    }
                    else if (token is QNameToken)
                    {
                        var node = new TokenNode(new Uri(R(token.Value.ToLower().Replace(":", "_namespace"))), token);
                        var link = Node(R(token.Value.ToLower()));
                        nodes.Add(node);
                        Add(node, "link", link);
                    }
                    else
                    {
                        var node = new TokenNode(new Uri(R(token.Value.ToLower())), token);
                        nodes.Add(node);
                    }

                }
                catch (Exception ex)
                {
                    CurrentError = ex;
                    return;
                }
                
            }
        }

        public Node LastWordAsNode()
        {
            return WordAtIndexAsNode(QueryText.Length);
        }

        public Node WordAtIndexAsNode(int index)
        {
            var node = nodes.Where(n => n.Token?.StartPosition <= index && n.Token?.EndPosition >= index).FirstOrDefault();
            return node;
        }
    }
}