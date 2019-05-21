using System.Collections.Generic;

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
