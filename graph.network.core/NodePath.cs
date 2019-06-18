using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace graph.network.core
{
    public class NodePath : List<Node>
    {
        public NodePath(IEnumerable<Edge> collection)
        {
            this.Edges = collection;
            Node lastObj = null;
            foreach (var edge in collection)
            {
                if (lastObj  == null || !lastObj.Equals(edge.Source))
                {
                    MarkLooped(edge.Source);
                    Add(edge.Source);
                }
                Add(edge.Predicate);
                MarkLooped(edge.Obj);
                Add(edge.Obj);
                lastObj = edge.Obj;
                
            }
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
            var sb = new StringBuilder();
            foreach (var item in this)
            {
                sb.Append($">{item.ShortId}");
            }
            return sb.ToString().TrimStart('>');
        }

        private void MarkLooped(Node node)
        {
            if (this.Any(n=> node.Value.Equals(n.Value)))
            {
                HasLoop = true;
            }
        }

        public bool HasLoop { get; private set; }
        public IEnumerable<Edge> Edges { get; private set; }

        public List<Node> GetVertices()
        {
            var result = new List<Node>();
            for (int i = 0; i < Count; i=i+2)
            {
                result.Add(this[i]);
            }
            return result;
        }
    }
}