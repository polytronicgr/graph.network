using System.Collections.Generic;

namespace graph.network.core
{
    public class NodePath : List<Node>
    {
        public NodePath(IEnumerable<Edge> collection)
        {
            foreach (var edge in collection)
            {
                Add(edge.Source);
                Add(edge.Predicate);
                Add(edge.Obj);
            }
        }
    }
}