using System;
using System.Collections.Generic;
using System.Linq;

namespace graph.network.core.nodes
{
    public class Node
    {
        Dictionary<string, object> properties = new Dictionary<string, object>();
 
        public Node(object value)
        {
            Value = value;
            Result = value;
        }


        public object Value { get; }

        public virtual void OnAdd(GraphNet graph)
        {
            BaseOnAdd(this, graph);
        }

        public static void BaseOnAdd(Node node, GraphNet graph)
        {
            node.Edges.ForEach(e =>
            {
                if (e.Obj != node && !graph.ContainsNode(e.Obj))
                {
                    node.DependentNodes.Add(e.Obj);
                }
                if (e.Subject != node && !graph.ContainsNode(e.Subject))
                {
                    node.DependentNodes.Add(e.Subject);
                }
                graph.Add(e);
            });
        }

        public virtual void OnProcess(GraphNet graph, List<NodePath> results)
        {
        }

        public virtual void OnRemove(GraphNet graph)
        {
            BaseOnRemove(this, graph);
        }

        public static void BaseOnRemove(Node node, GraphNet graph)
        {
            foreach (var e in node.Edges)
            {
                graph.Remove(e);
            }

            node.DependentNodes.ForEach(n =>
            {
                if (graph.ContainsNode(n))
                {
                    graph.Remove(n);
                }
            });
        }
        public virtual bool IsPathValid(GraphNet graph, NodePath path)
        {
            return BaseIsPathValid(this, graph, path);
        }

        public static bool BaseIsPathValid(Node node,  GraphNet graph, NodePath path)
        {
            if (path.HasLoop) return false;
            var passesThroughAnOutputOrThisNode = path.Skip(1).Take(path.Count - 2).Any(n => n== node || graph.Outputs.Contains(n) );
            return !passesThroughAnOutputOrThisNode;
        }

        public virtual List<Edge> Edges { get; set; } = new List<Edge>();
        public virtual List<Node> DependentNodes { get; private set; } = new List<Node>();


        public virtual bool RemoveOrphanedEdgeObjs { get; set; } = true;
        public object Result { get; set; }

        /// <summary>
        /// A node exposes an interface that paths will be calulated from by default the nodes interface is 
        /// itself if you add Edges then their objects become this nodes interface but the method is virtual 
        /// so nodes can expose any interface that they wish
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Node> GetInterface()
        {
            //TODO: could cache this 
            return Edges.Count > 0 ? Edges.Select(e => e.Obj) : new List<Node> { this };
        }

        public Node AddEdge(string predicate, object obj, GraphNet gn)
        {
            return AddEdge(predicate, gn.Node(obj), gn);
        }

        public Node AddEdge(string predicate, Node obj, GraphNet gn)
        {
            return AddEdge(gn.Node(predicate), obj);
        }

        public Node AddEdge(Node predicate, Node obj)
        {
            Edges.Add(new Edge(this, predicate, obj));
            return obj;
        }

        public Node AddEdge(Edge edge)
        {
            Edges.Add(edge);
            return edge.Obj;
        }

        public override string ToString()
        {
            return Value?.ToString();
        }

        /*
        public override bool Equals(object obj)
        {
            if (Value == null && obj != null) return false;
            if (Value != null && obj == null) return false;
            if (Value == null && obj == null) return true;
            if (Value.Equals(obj)) return true;
            Node node = obj as Node;
            if (node?.Value != null && Value.Equals(node.Value)) return true;

            return false;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        */

        public Edge GetEdgeByPredicate(object value)
        {
            return Edges.First(e => e.Predicate.Value == value);
        }

        public void SetProp(string name, object value)
        {
            properties[name] = value;
        }

        public T GetProp<T>(string name)
        {
            if (!properties.ContainsKey(name)) return default;
            return (T)properties[name];
        }
    }
}
