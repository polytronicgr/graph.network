using graph.network.core.nodes;
using System.Collections.Generic;

namespace graph.network.core
{
    public class NodePath : List<Node>
    {
        public NodePath(IEnumerable<Edge> collection)
        {
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

        private void MarkLooped(Node node)
        {
            if (Contains(node))
            {
                HasLoop = true;
            }
        }

        public bool HasLoop { get; private set; }

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