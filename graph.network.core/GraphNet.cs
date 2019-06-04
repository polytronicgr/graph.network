using graph.network.core.nodes;
using graph.network.core.tests;
using QuickGraph;
using QuickGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace graph.network.core
{
    public class GraphNet
    {
        private BidirectionalGraph<Node, Edge> graph = new BidirectionalGraph<Node, Edge>();
        private Net net;
        private readonly int maxPathLenght;
        private readonly int maxNumberOfPaths;

        public List<Node> Outputs { get; set; } = new List<Node>();
        public Dictionary<object, Node> NodeIndex { get; set; } = new Dictionary<object, Node>();

        public GraphNet(int maxPathLenght = 20, int maxNumberOfPaths = 10)
        {
            this.maxPathLenght = maxPathLenght;
            this.maxNumberOfPaths = maxNumberOfPaths;
        }

        public Node Node(string subject, string predicate, params string[] objs)
        {
            if (NodeIndex.ContainsKey(subject))
            {
                Node node = NodeIndex[subject];
                foreach (var obj in objs)
                {
                    node.AddEdge(Node(predicate), Node(obj));
                }
                return node;
            }
            else
            {
                var node = new Node(subject);
                foreach (var obj in objs)
                {
                    node.AddEdge(Node(predicate), Node(obj));
                }
                NodeIndex[node.Value] = node;
                return node;
            }
        }
        public Node Node(object value)
        {
            if(NodeIndex.ContainsKey(value))  return NodeIndex[value];
            var node = new Node(value);
            NodeIndex[node.Value] = node;
            return node;
        }

        public Example NewExample(string input, string output)
        {
            return new Example(Node(input), Node(output) );
        }

        public Example NewExample(Node input, string output) 
        {
            return new Example(input, Node(output));
        }

        public void Add(object subject, string predicate, object obj)
        {
            Add(new Edge(Node(subject), Node(predicate), Node(obj)));
        }

        public void Add(object subject, string predicate, object obj, bool isOutput)
        {
            var output = Node(obj);
            Add(output, isOutput);
            Add(new Edge(Node(subject), Node(predicate), output));
        }

        public void Add(Node node, bool isOutput = false)
        {
            if (isOutput && !Outputs.Contains(node))
            {
                Outputs.Add(node);
            }
            if (graph.ContainsVertex(node)) return;
            if (!NodeIndex.ContainsKey(node.Value))
            {
                NodeIndex[node.Value] = node;
            }
            else if (NodeIndex[node.Value] != node)
            {
                throw new InvalidOperationException($"a node with the value '{node.Value}' already exists!");
            }
            graph.AddVertex(node);
            node.OnAdd(this);
        }
        
        public void Add(Edge edge)
        {
            Add(edge.Subject, false);
            Add(edge.Obj, false);
            graph.AddVerticesAndEdge(edge);
            Edge backlink = GetBackLink(edge);
            if (!graph.ContainsEdge(backlink))
            {
                graph.AddVerticesAndEdge(backlink);
            }
        }

        private static Edge GetBackLink(Edge edge)
        {
            return new Edge(edge.Obj, edge.Predicate, edge.Subject);
        }

        public void Train(params Example[] examples)
        {
            List<Node> allNodes = GetNodeIndex();
            net = new Net(allNodes, Outputs, maxPathLenght, maxNumberOfPaths);
            foreach (Example example in examples)
            {
                var paths = GetPaths(example);
                if (paths != null && paths.Count != 0)
                {
                    net.AddToTraining(paths, example.Output);
                }
            }
            net.Train();
        }

        private List<Node> GetNodeIndex()
        {
            var allNodes = new List<Node>(graph.Vertices);
            allNodes.AddRange(graph.Edges.Select(e => e.Predicate).Distinct());
            allNodes = allNodes.Distinct().ToList();
            return allNodes;
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
                Add(input, false);
            }
            if (!graph.ContainsVertex(output))
            {
                outputAdded = true;
                Add(output, true);
            }

            input = GetNode(input.Value); //TODO: remove this
            output = GetNode(output.Value); //TODO: remove this

            //get all the paths from the exposed interface of the input node to 
            //the exposed interface of the output node
            List<NodePath> results = new List<NodePath>();
            foreach (var i in input.GetInterface())
            {
                foreach (var o in output.GetInterface())
                {
                    AddPath(i, o, input, results);
                }
            }

            //allow all nodes in the paths to perform any custom processing
            var all = results.SelectMany(n => n).ToList();
            all.Add(input);
            all.Add(output);
            foreach (var n in all.Distinct())
            {
                n.OnProcess(this, results);
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
        
        private void AddPath(Node inputInterfaceNode, Node outputInterfaceNode, Node masterInputNode, List<NodePath> paths)
        {
            IEnumerable <IEnumerable<Edge>> found = graph.RankedShortestPathHoffmanPavley(n => 1, inputInterfaceNode, outputInterfaceNode, 10);
            foreach (var path in found)
            {
                if (path != null)
                {
                    NodePath nodePath = new NodePath(path);
                    if (masterInputNode.IsPathValid(this, nodePath) && inputInterfaceNode.IsPathValid(this, nodePath) && outputInterfaceNode.IsPathValid(this, nodePath))
                    {
                        paths.Add(nodePath);
                        return;
                    }
                }
            }

        }

        public void Remove(Node node)
        {
            node.OnRemove(this);
            graph.RemoveVertex(node);
            NodeIndex.Remove(node.Value);
            if (Outputs.Contains(node))
            {
                Outputs.Remove(node);
            }
        }

        public void Remove(Edge edge)
        {
            if (graph.ContainsEdge(edge))
            {
                graph.RemoveEdge(edge);
            }
            Edge backlink = GetBackLink(edge);
            if (graph.ContainsEdge(backlink))
            {
                graph.RemoveEdge(backlink);
            }
        }

        public Node Predict(string input)
        {
            return Predict(Node(input));
        }

        public Node Predict(Node input)
        {
            List<Result> results = Rank(input);
            return results[0].Output;
        }

        public List<Result> Rank(Node input)
        {
            var results = new List<Result>();
            Outputs.ForEach(ouput => AddResult(input, ouput, results));
            results.Sort((r1, r2) => r2.Probabilty.CompareTo(r1.Probabilty));
            return results;
        }

        private void AddResult(Node input, Node ouput, List<Result> results)
        {
            var paths = GetPaths(input, ouput);
            if (net == null)
            {
                net = new Net(GetNodeIndex(), Outputs, maxPathLenght, maxNumberOfPaths);
            }
            var probabilty = net.GetProbabilty(paths, ouput);
            var result = new Result(input, ouput, paths, probabilty);
            results.Add(result);
        }

        public Node GetNode(object value)
        {
            Node node = graph.Vertices.First(v => v.Value.Equals(value));
            return node;
        }

        public bool IsEdgesEmpty(object value)
        {
            Node node = graph.Vertices.First(v => v.Value.Equals(value));
            return IsEdgesEmpty(node);

        }

        public bool ContainsNode(Node node)
        {
            return graph.ContainsVertex(node);
        }

        public bool IsEdgesEmpty(Node node)
        {
            return graph.IsOutEdgesEmpty(node) && graph.IsInEdgesEmpty(node);
        }
    }
}