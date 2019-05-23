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
                var paths = GetPaths(example);
                if (paths != null && paths.Count != 0)
                {
                    net.AddToTraining(paths, example.Output);
                }
            }
            net.Train();
        }

        private List<NodePath> GetPaths(Node input, Node output)
        {
            return GetPaths(new Example(input, output));
        }
        private List<NodePath> GetPaths(Example example)
        {
            var inputAdded = false;
            var outputAdded = false;
            Node input = example.Input;
            Node output = example.Output;

            //add inputs and outputs on the fly if they are not in the graph
            if (!graph.ContainsVertex(input))
            {
                inputAdded = true;
                Add(input);
            }
            if (!graph.ContainsVertex(output))
            {
                outputAdded = true;
                Add(output);
            }

            //get all the paths from the exposed interface of the input node to 
            //the exposed interface of the output node
            List<NodePath> results = new List<NodePath>();
            foreach (var i in input.GetInterface())
            {
                foreach (var o in output.GetInterface())
                {
                    AddPath(i, o, results);
                }
            }
                      
            //remove any temp inputs and outputs that were added on the fly
            if (inputAdded)
            {
                Remove(input);
            }
            if (outputAdded)
            {
                Remove(output);
            }

            return results;
        }

        private void AddPath(Node inputInternceNode, Node outputInterfaceNode,List<NodePath> paths)
        {
            graph.ShortestPathsDijkstra(n => 1, inputInternceNode)(outputInterfaceNode, out IEnumerable<Edge> path);
            if (path != null)
            {
                NodePath nodePath = new NodePath(path);
                //if a path travels through another output then it is not valid
                var passesThroughAnOutput = nodePath.Take(nodePath.Count - 1).Any(n => outputs.Contains(n));
                if (!passesThroughAnOutput)
                {
                    paths.Add(nodePath);
                }
            }
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

        public Node Predict(string input)
        {
            return Predict(new Node(input));
        }

        public Node Predict(Node input)
        {
            List<Result> results = Rank(input);
            return results[0].Output;
        }

        public List<Result> Rank(Node input)
        {
            var results = new List<Result>();
            outputs.ForEach(ouput => AddResult(input, ouput, results));
            results.Sort((r1, r2) => r2.Probabilty.CompareTo(r1.Probabilty));
            return results;
        }

        private void AddResult(Node input, Node ouput, List<Result> results)
        {
            var paths = GetPaths(input, ouput);
            var probabilty = net.GetProbabilty(paths, ouput);
            var result = new Result(input, ouput, paths, probabilty);
            results.Add(result);
        }
    }
}