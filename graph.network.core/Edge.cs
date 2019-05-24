using graph.network.core.nodes;
using QuickGraph;

namespace graph.network.core
{
    public class Edge : TaggedUndirectedEdge<Node, Node>
    {
        public Edge(string subject, string predicate, string obj) : this(new Node(subject), new Node(predicate), new Node(obj)) { }

        public Edge(Node subject, Node predicate, Node obj) : base(subject, obj, predicate)
        {
            Subject = subject;
            Predicate = predicate;
            Obj = obj;
            //QuickGraph.TaggedUndirectedEdge<string, string> edge;
            //this.edge = new QuickGraph.TaggedUndirectedEdge<string, string>()
        }

        public Node Subject { get; }
        public Node Predicate { get; }
        public Node Obj { get; }
    }
}
