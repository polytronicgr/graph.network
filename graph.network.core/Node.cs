using System.Collections.Generic;
using System.Linq;

namespace graph.network.core
{
    public class Node
    {
        public Node(string subject, string predicate, string obj): this(subject) {
            AddEdge(predicate, obj);
        }
        public Node(object value)
        {
            Value = value;
        }

        public object Value { get; }

        public List<Edge> Edges { get; } = new List<Edge>();

        /// <summary>
        /// A node exposes an interface that paths will be calulated from by default the nodes interface is 
        /// itself if you add Edges then their objects become this nodes interface but the method is virtual 
        /// so nodes can expose any interface that they wish
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Node> GetInterface()
        {
            //TODO: could cache this 
            return Edges.Count > 0 ? Edges.Select(e => e.Obj) : new List<Node> { this };
        }

        public void AddEdge(string predicate, object obj)
        {
            Edges.Add(new Edge(this,new Node(predicate), new Node(obj)));
        }

        public override string ToString()
        {
            return Value?.ToString();
        }

        public override bool Equals(object obj)
        {
            if (Value==null && obj != null) return false;
            if (Value!=null && obj == null) return false;
            if (Value==null && obj == null) return true;
            if (Value.Equals(obj)) return true;
            Node node = obj as Node;
            if (node?.Value != null && Value.Equals(node.Value)) return true;

            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
