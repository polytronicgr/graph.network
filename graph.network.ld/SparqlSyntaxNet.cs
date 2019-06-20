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
    }
}
