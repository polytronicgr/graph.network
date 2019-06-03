using graph.network.core.nodes;
using QuickGraph;

namespace graph.network.core
{
    public class Edge : TaggedEdge<Node, Node>
    {
        public Edge(Node subject, Node predicate, Node obj) : base(subject, obj, predicate)
        {
            Subject = subject;
            Predicate = predicate;
            Obj = obj;
        }

        public Node Subject { get; }
        public Node Predicate { get; }
        public Node Obj { get; }
        public bool Internal { get; set; }
    }
}
