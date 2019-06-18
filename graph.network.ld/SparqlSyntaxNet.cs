using graph.network.core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace graph.network.ld
{
    public class SparqlSyntaxNet : GraphNet
    {
        private static string IRI_PREFIX = "http://graph.network.com/ld/";

        public string CurrentQuery { get; set; }
        public bool InParseMode { get; private set; }

        public SparqlSyntaxNet() : this(R("sparql"))
        { }

        public SparqlSyntaxNet(string name) : base(name)
        {
            Add("select", "after", "");
            Add("*", "after", "select");
            Add("distinct", "after", "select");
            Add("where", "after", "*");
            Add("{", "after", "where");
            Add("?s", "a", "variable");
            Add("?p", "a", "variable");
            Add("?o", "a", "variable");
            Add("variable", "after", "{");
            Add("variable", "after", "distinct");
            Add("variable", "after", "variable");
            Add("}", "after", ".");
            Add(".", "after", "variable");


            Outputs.Add(Node("select"));
            Outputs.Add(Node("*"));
            Outputs.Add(Node("{"));
            Outputs.Add(Node("}"));
            Outputs.Add(Node("where"));
            Outputs.Add(Node("?s"));
            Outputs.Add(Node("?p"));
            Outputs.Add(Node("?o"));
            Outputs.Add(Node("distinct"));
        }

        public void TrainFromQueries(params string[] queries)
        {
            InParseMode = false;
            var examples = new List<NodeExample>();
            
            foreach (var query in queries)
            {
                var words = query.Split(' ');
                var lastWord = Node("");
                foreach (var word in words)
                {
                    var node = Node(word);
                    examples.Add(new NodeExample(Node(lastWord), node));
                    lastWord = node;
                }
            }

            Train(examples.ToArray());

            InParseMode = true;
        }


        public static string R(string value)
        {
            return IRI_PREFIX + value;
        }

        public override Node Node(object value)
        {
            var node = value as Node;
            if (node == null)
            {
                var str = value.ToString();
                var iri = str.StartsWith("http") ? str : R(str);
                return base.Node(iri);
            }
            else
            {
                return base.Node(node);
            }

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
