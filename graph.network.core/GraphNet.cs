using graph.network.core.nodes;
using graph.network.core.tests;
using QuickGraph;
using QuickGraph.Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace graph.network.core
{
    public class GraphNet : Node
    {
        private BidirectionalGraph<Node, Edge> graph = new BidirectionalGraph<Node, Edge>();
        private Dictionary<string, Action<Node, GraphNet>> dynamicPrototypes = new Dictionary<string, Action<Node, GraphNet>>();
        private Net net;
        private readonly int maxPathLenght;
        private readonly int maxNumberOfPaths;


        public List<Node> Outputs { get; set; } = new List<Node>();

        public Dictionary<object, Node> NodeIndex { get; set; } = new Dictionary<object, Node>();
        public string DefaultInput { get; set; }

        public GraphNet(string name, int maxPathLenght = 20, int maxNumberOfPaths = 10) : base(name)
        {
            this.maxPathLenght = maxPathLenght;
            this.maxNumberOfPaths = maxNumberOfPaths;
        }

        public override void OnAdd(GraphNet graph)
        {
            Edges = AllEdges();
            BaseOnAdd(this, graph);
        }

       public override void OnProcess(GraphNet graph, Node input, List<NodePath> results)
       {
            Result = Predict(input);
       }

        public Node Node(Node subject, string predicate, params string[] objs)
        {
            return Node(subject.Value, predicate, objs);
        }

        public Node Node(string subject, string predicate, Node obj)
        {
            var node = Node(subject);
            node.AddEdge(Node(predicate), obj);
            return node;
        }



        public Node Node(object subject, string predicate, params string[] objs)
        {
            if (NodeIndex.ContainsKey(subject))
            {
                Node node = NodeIndex[subject];
                node.Edges.Clear();
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

            var node = DefaultInput == null ? new Node(value) : DynamicNode(DefaultInput)(value);
            NodeIndex[node.Value] = node;
            return node;
        }

        public NodeExample NewExample(string input, string output)
        {
            return new NodeExample(Node(input), Node(output) );
        }

        public NodeExample NewExample(Node input, string output) 
        {
            return new NodeExample(input, Node(output));
        }

        public void Add(Node subject, string predicate, object obj)
        {
            Add(new Edge(subject, Node(predicate), Node(obj)));
        }

        public void AddDynamic(string name, string subject, string predicate, object obj)
        {
            Add(DynamicNode(name)(subject), predicate, obj);
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
                NodeIndex[node.Value] = node; //TODO: this could be really bad!!!
                //throw new InvalidOperationException($"a node with the value '{node.Value}' already exists!");
            }
            graph.AddVertex(node);
            node.OnAdd(this);
        }

        public void RegisterDynamic(string name, Action<Node, GraphNet> onAdd = null)
        {
            dynamicPrototypes[name] = onAdd;
        }
        
        public Func<object, DynamicNode> DynamicNode(string name)
        {
            
            var onAdd = dynamicPrototypes[name];
            Func<object, DynamicNode> result = (value) => new DynamicNode(value, onAdd);
            return result;
        }

        public void Add(Edge edge)
        {
            Add(edge.Subject, false);
            Add(edge.Obj, false);
            graph.AddVerticesAndEdge(edge);
            Edge backlink = GetBackLink(edge);
            if (edge.Subject != edge.Obj && !graph.ContainsEdge(backlink))
            {
                graph.AddVerticesAndEdge(backlink);
            }
        }

        private static Edge GetBackLink(Edge edge)
        {
            return new Edge(edge.Obj, edge.Predicate, edge.Subject);
        }


        public  override void Train(params Example[] examples)
        {
            List<Node> allNodes = GetNodeIndex();
            net = new Net(allNodes, Outputs, maxPathLenght, maxNumberOfPaths);
            foreach (var ouputNode in Outputs)
            {
                ouputNode.Train(examples);
            }

            foreach (Example example in examples)
            {
                var inputNode = example.Input as Node; //TODO: what if the input is not a node
                foreach (var ouputNode in Outputs)
                {
                    var nodeExample = new NodeExample(inputNode, ouputNode);
                    var paths = GetPaths(nodeExample);

                  
                    /*
                    //TODO: ****** if the ouput is a GraphNet how does it get its result set to be its prediction for this???????????? this is very messy!!
                    if (ouputNode is GraphNet)
                    {
                        var result = ((GraphNet)ouputNode).Predict(inputNode);
                        if (result == example.Output)
                        {
                            //net.AddToTraining(paths, ouputNode);
                            //ouputNode.Result = result;
                        }
                        
                    }
                 */
                   
                    
                    if (paths != null && paths.Count != 0 && ouputNode.Result != null && ouputNode.Result.Equals(example.Output)   ) //TODO: what about learning and getting closer
                    {
                        net.AddToTraining(paths, ouputNode);
                    }
                }
            }
            net.Train();
        }
        
        public void Train(params NodeExample[] examples)
        {
            List<Node> allNodes = GetNodeIndex();

            net = new Net(allNodes, Outputs, maxPathLenght, maxNumberOfPaths);
            foreach (NodeExample example in examples)
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
            return GetPaths(new NodeExample(input, output));
        }
        private List<NodePath> GetPaths(NodeExample example)
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
            results = results.Distinct().ToList();
            //allow all nodes in the paths to perform any custom processing
            var all = results.SelectMany(n => n).ToList();
            all.Add(input);
            all.Add(output);
            foreach (var n in all.Distinct())
            {
                n.OnProcess(this, input, results);
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
            //TOD: think about this - it can happen if you have an orphend edge 
            if (!NodeIndex.ContainsKey(inputInterfaceNode.Value)) return;
            if (!NodeIndex.ContainsKey(outputInterfaceNode.Value)) return;
            inputInterfaceNode = NodeIndex[inputInterfaceNode.Value];
            outputInterfaceNode = NodeIndex[outputInterfaceNode.Value];
            //if (inputInterfaceNode == outputInterfaceNode)
            //{
            //    paths.Add(new NodePath(new Edge(inputInterfaceNode, Node("direct"), outputInterfaceNode)));
            //    return;
            //}
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


        public object Predict(string input)
        {
            return Predict(Node(input));
        }

        public object Predict(Node input)
        {
            List<Result> results = Rank(input);
            return results[0].Output.Result;
        }


        public Node PredictNode(Node input)
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

        public List<Edge> AllEdges()
        {
            return graph.Edges.ToList();
        }
    }
}