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
                    Add(edge.Source);
                }
                Add(edge.Predicate);
                Add(edge.Obj);
                lastObj = edge.Obj;
            }
        }
    }
}