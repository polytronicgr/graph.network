using graph.network.core.tests;
using QuickGraph;
using QuickGraph.Algorithms;
using System.Collections.Generic;
using System.Linq;

namespace graph.network.core
{
    public class GraphNet
    {
        UndirectedGraph<Node, Edge> graph = new UndirectedGraph<Node, Edge>();
        List<Node> outputs = new List<Node>();
        Net net;
        public void Add(object subject , string predicate, object obj)
        {
            Add(new Edge(new Node(subject), new Node(predicate), new Node(obj)));
        }

        public void Add(object subject, string predicate, object obj, bool isOutput)
        {
            var output = new Node(obj);
            Add(new Edge(new Node(subject), new Node(predicate), output));
            if (isOutput && !outputs.Contains(output))
            {
                outputs.Add(output);
            }
        }

        public void Add(Node node)
        {
            graph.AddVertex(node);
            node.Edges.ForEach(e => Add(e));
        }

        public void Add(Edge edge)
        {
            graph.AddVerticesAndEdge(edge);
        }

        public void Train(params Example[] examples)
        {
            var allNodes = new List<Node>(graph.Vertices);
            allNodes.AddRange(graph.Edges.Select(e => e.Predicate).Distinct());
            allNodes = allNodes.Distinct().ToList();
            net = new Net(allNodes, outputs);
            foreach (Example example in examples) {
                var path = GetPath(example);
                if (path != null)
                {
                    net.AddToTraining(path, example.Output);
                }
            }
            net.Train();
        }

        private NodePath GetPath(Node input, Node output)
        {
            return GetPath(new Example(input, output));
        }
        private NodePath GetPath(Example example)
        {
            var inputAdded = false;
            if (!graph.ContainsVertex(example.Input))
            {
                inputAdded = true;
                Add(example.Input);
            }

            graph.ShortestPathsDijkstra(n => 1, example.Input)(example.Output, out IEnumerable<Edge> path);
            if (inputAdded)
            {
                Remove(example.Input);
            }
            
            if (path == null) return null;
            return new NodePath(path);
        }

        public void Remove(Node node)
        {
            node.Edges.ForEach(e => Remove(e));
            graph.RemoveVertex(node);
        }

        public void Remove(Edge edge)
        {
            if (graph.ContainsEdge(edge))
            {
                graph.RemoveEdge(edge);
            }
        }

        public Node Predict(Node input)
        {
            List<Result> results = Rank(input);
            return results[0].Output;
        }

        public List<Result> Rank(Node input)
        {
            var results = new List<Result>();
            outputs.ForEach(ouput =>
            {
                var path = GetPath(input, ouput);
                var probabilty = net.GetProbabilty(path, ouput);
                var result = new Result(input, ouput, path, probabilty);
                results.Add(result);
            });

            results.Sort((r1, r2) => r2.Probabilty.CompareTo(r1.Probabilty));
            return results;
        }
    }
}