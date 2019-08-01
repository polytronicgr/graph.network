using System.Collections.Generic;
using graph.network.core;

namespace graph.network.ld
{

    public class SparqlSyntaxNet : LinkedDataNet
    {
        public SparqlSyntaxNet() : this(R("sparql"))
        { }

        public SparqlSyntaxNet(string name) : base(name)
        {
            Add("select", "after", "");
            Add("*", "after", "select");
            Add("distinct", "after", "select");
            Add("where", "after", "*");
            Add("{", "after", "where");
            Add("?s", "is-a", "subejct");
            Add("?p", "is-a", "predicate");
            Add("?o", "is-a", "object");
            Add("subejct", "is-a", "variable");
            Add("predicate", "is-a", "variable");
            Add("object", "is-a", "variable");
            Add("subejct", "after", "{");
            Add("variable", "after", "distinct");
            Add("predicate", "after", "subejct");
            Add("object", "after", "predicate");
            Add("}", "after", ".");
            Add("}", "after", "object");
            Add(".", "after", "object");
            Add("a", "after", "subejct");


            Outputs.Add(Node("select"));
            Outputs.Add(Node("*"));
            Outputs.Add(Node("{"));
            Outputs.Add(Node("}"));
            Outputs.Add(Node("where"));
            Outputs.Add(Node("?s"));
            Outputs.Add(Node("?p"));
            Outputs.Add(Node("?o"));
            Outputs.Add(Node("distinct"));
            Outputs.Add(Node("a"));
            Outputs.Add(Node("."));
        }

        public override IEnumerable<Node> GetInterface()
        {
            return Outputs.AsReadOnly();
        }
    }
}
