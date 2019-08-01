using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace graph.network.core
{
    public class Node : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Dictionary<string, object> properties = new Dictionary<string, object>();

        public bool UseEdgesAsInterface { get; set; } = true;

        public object Value { get; }

        public virtual List<Edge> Edges { get; set; } = new List<Edge>();
        private Dictionary<GraphNet,List<Node>> dependentNodes { get;} = new Dictionary<GraphNet, List<Node>>();

        public virtual bool RemoveOrphanedEdgeObjs { get; set; } = true;
        public object Result { get; set; }

        public double CurrentProbabilty { get; set; } = 1;

        public Node(object value)
        {
            Value = value;
            Result = value;
            ShortId = ToString();
        }

        public virtual void OnAdd(GraphNet graph)
        {
            BaseOnAdd(this, graph);
        }

        public virtual void Train(params Example[] examples)
        {

        }

        public static void BaseOnAdd(Node node, GraphNet graph)
        {
            node.GetDependentNodes(graph).Clear();
            node.Edges.ForEach(e =>
            {
                if (e.Obj != node && !graph.ContainsNode(e.Obj))
                {
                    node.GetDependentNodes(graph).Add(e.Obj);
                }
                if (e.Subject != node && !graph.ContainsNode(e.Subject))
                {
                    node.GetDependentNodes(graph).Add(e.Subject);
                }
                graph.Add(e);
            });
        }

        public virtual List<Node> GetDependentNodes(GraphNet graph)
        {
            if (!dependentNodes.ContainsKey(graph))
            {
                dependentNodes[graph] = new List<Node>();
            }
            return dependentNodes[graph];
        }

        public virtual void OnProcess(GraphNet graph, Node input, List<NodePath> results)
        {
        }

        public virtual void OnRemove(GraphNet graph)
        {
            BaseOnRemove(this, graph);
        }

        public virtual bool IsGraphNet => false;

        public static void BaseOnRemove(Node node, GraphNet graph)
        {
            foreach (var e in node.Edges)
            {
                graph.Remove(e);
            }

            node.GetDependentNodes(graph).ForEach(n =>
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

        public static bool BaseIsPathValid(Node node, GraphNet graph, NodePath path)
        {
            if (path.HasLoop) return false;
            if (path.Count ==0) return false;
            if (path[0].Value.Equals(path[path.Count-1].Value)) return false;
            var middle = path.Skip(1).Take(path.Count - 2);
            var passesThroughAnOutputOrThisNode = middle.Any(n => n.Equals(node) || graph.Outputs.Contains(n) || graph.Inputs.Contains(n));
            return !passesThroughAnOutputOrThisNode;
        }



        /// <summary>
        /// A node exposes an interface that paths will be calulated from by default the nodes interface is 
        /// itself if you add Edges then their objects become this nodes interface but the method is virtual 
        /// so nodes can expose any interface that they wish
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<Node> GetInterface()
        {
            //NOTE: could cache this 
            return Edges.Count > 0 && UseEdgesAsInterface ? Edges.Where(e=> !e.Internal).Select(e => e.Obj) : new List<Node> { this };
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

        public virtual string ShortId { get; set; }

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
