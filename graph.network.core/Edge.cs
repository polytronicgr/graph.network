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

        public string ShortId
        {
            get { return Predicate?.ShortId; }
        }


        public override bool Equals(object obj)
        {
            var id = ToString();
            if (id == null && obj != null) return false;
            if (id != null && obj == null) return false;
            if (id == null && obj == null) return true;
            if (id.Equals(obj.ToString())) return true;


            return false;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            //NOTE: could cache this
            return$"{Subject.Value}|{Predicate.Value}|{Obj.Value}";
        }
    }
}
