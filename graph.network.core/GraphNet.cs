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

        public List<Node> Outputs { get; set; } = new List<Node>();
        public List<Node> Inputs { get; set; } = new List<Node>();

        public Dictionary<object, Node> NodeIndex { get; set; } = new Dictionary<object, Node>();
        public string DefaultInput { get; set; }
        public bool LimitNumberOfPaths { get;  set; }
        public bool AddBiDirectionalLinks { get; set; } = true;
        public int MaxPathLenght { get; set; }
        public int MaxNumberOfPaths { get; set; }

        public Action<BidirectionalGraph<Node, Edge>> OnGraphSnapshot { get; set; }

        public GraphNet(string name, int maxPathLenght = 20, int maxNumberOfPaths = 10) : base(name)
        {
            MaxPathLenght = maxPathLenght;
            MaxNumberOfPaths = maxNumberOfPaths;
        }

        public override void OnAdd(GraphNet graph)
        {
            if (Edges.Count == 0)
            {
                Edges = AllEdges();
            }
            
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
                var node = NewNode(subject);
                foreach (var obj in objs)
                {
                    node.AddEdge(Node(predicate), Node(obj));
                }
                NodeIndex[node.Value] = node;
                return node;
            }
        }
        
        public virtual Node Node(object value)
        {
            if (value is Node n && NodeIndex.ContainsKey(n.Value)) return NodeIndex[n.Value];
            if (NodeIndex.ContainsKey(value)) return NodeIndex[value];

            var node = DefaultInput == null ? NewNode(value) : DynamicNode(DefaultInput)(value);
            NodeIndex[node.Value] = node;
            return node;
        }

        protected virtual Node NewNode(object value)
        {
            return new Node(value);
        }

        public List<Result> GetExamples(Result result)
        {
            if (result == null) return new List<Result>(); 
            var data = net.GetTraingData(result.Output);
            var withInput = data.Where(d => d[0][0].Value == result.Input.Value)
                .Select(p => new Result(result.Input, result.Output, p, result.Probabilty))
                .ToList();
            withInput.Insert(0,new Result(result.Input, result.Output, null, result.Probabilty));
            return withInput;
        }

        public NodeExample NewExample(string input, string output)
        {
            return new NodeExample(Node(input), Node(output) );
        }

        public NodeExample NewExample(Node input, string output) 
        {
            return new NodeExample(input, Node(output));
        }

        public void Add(Node subject, string predicate, Node obj)
        {
            Add(subject, Node(predicate), obj);
        }
        public void Add(Node subject, Node predicate, Node obj)
        {
            Add(new Edge(subject, predicate, obj));
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

        public void Add(Node node, bool isOutput = false, bool isInput = false)
        {
            if (isOutput && !Outputs.Contains(node))
            {
                Outputs.Add(node);
            }
            if (isInput && !Inputs.Contains(node))
            {
                Inputs.Add(node);
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
            if (AddBiDirectionalLinks && !edge.Inverted)
            {
                Edge backlink = GetBackLink(edge);
                backlink.Inverted = true;
                if (edge.Subject != edge.Obj && !graph.ContainsEdge(backlink))
                {
                    graph.AddVerticesAndEdge(backlink);
                }
            }
        }

        private Edge GetBackLink(Edge edge)
        {
            return new Edge(edge.Obj, Node(edge.Predicate.Value.ToString() + "-inv"), edge.Subject);
        }


        public  override void Train(params Example[] examples)
        {
            List<Node> allNodes = GetNodeIndex();
            net = new Net(allNodes, Outputs, MaxPathLenght, MaxNumberOfPaths);
            foreach (var ouputNode in Outputs)
            {
                ouputNode.Train(examples);
            }
            
            foreach (Example example in examples)
            {
                var paths = GetPaths(example);
                //TODO: check that this trim is safe
                if (paths.Count >= MaxNumberOfPaths && LimitNumberOfPaths)
                {
                    paths = paths.OrderBy(p => p.Count).Take(MaxNumberOfPaths - 1).ToList();
                }
                if (paths.Count > 0)
                {
                    var first = paths[0];
                    var output = Node(first[first.Count - 1]);
                    if (!Outputs.Contains(output))
                    {
                        throw new InvalidOperationException($"{output} is not an output in { this}");
                    }
                    paths.ForEach(p => {
                        var o = Node(p[p.Count - 1]);
                        if (o != output) throw new InvalidOperationException($"multiple outputs");
                    });
                    net.AddToTraining(paths, output);
                }
            }
            net.Train();
        }

        /// <summary>
        /// Gets all the paths for an example
        /// </summary>
        /// <param name="example"></param>
        /// <returns></returns>
        public List<NodePath> GetPaths(Example example)
        {
            var results = new List<NodePath>();
            var inputNode = example.Input as Node; //TODO: what if the input is not a node
            //check each output
            foreach (var outputNode in Outputs.ToList())
            {
                var nodeExample = new NodeExample(inputNode, outputNode);
                //TODO: perhaps node could have a getPaths (that takes an example and graph) that graphnet overrides - this could simplify this code
                if (!(outputNode is GraphNet subGraph))
                {
                    //if this is not a sub graph then get the paths
                    var paths = GetPaths(nodeExample);
                    //add if the result matches the exampe then add this paths as an example
                    if (paths != null && paths.Count != 0 && outputNode.Result != null && outputNode.Result.Equals(example.Output)) //TODO: what about learning and getting closer
                    {
                        results.AddRange(paths);
                    }
                }
                else
                {
                    //if the output is an graph then get the paths from that
                    var paths = subGraph.GetPaths(example);
                    if (paths.Count > 0)
                    {
                        //get it to make a prediction and if that matches the example then add these paths
                        var result = subGraph.Predict(inputNode);
                        if (result != null && result.Equals(example.Output)) //TODO: what about learning and getting closer
                        {
                            //add a link in the path to this subgraph
                            //TODO: should subgraphs be transparent like this?? or should this graph calculate its path to this subgraph and then the subgraph appends its part???
                            paths.ForEach(p =>
                            {
                                if (!p.Last().Equals(subGraph))
                                {
                                    p.Add(Node("in"));
                                    p.Add(subGraph);
                                }
                            });
                            results.AddRange(paths);
                        }
                    }
                }
            }
            return results;
        }
        
        public void Train(params NodeExample[] examples)
        {
            List<Node> allNodes = GetNodeIndex();

            net = new Net(allNodes, Outputs, MaxPathLenght, MaxNumberOfPaths);
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
            allNodes = allNodes.OrderBy(n => n.Value.ToString()).ToList(); //TODO: simple sort - think about nodes in word vetor space
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
                Add(input, false, true);
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
                    AddPath(i, o, input, output, results);
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

            OnGraphSnapshot?.Invoke(graph);

            //remove any temp inputs and outputs that were added on the fly
            if (inputAdded)
            {
                Remove(input);
            }
            if (outputAdded)
            {
                Remove(output);
            }

            //TODO: check that this trim is safe
            if (results.Count >= MaxNumberOfPaths && LimitNumberOfPaths)
            {
                results = results.OrderBy(p => p.Count).Take(MaxNumberOfPaths - 1).ToList();
            }
            return results;
        }

        public override bool IsGraphNet => true;

        public Edge GetEdge(Node a, Node b)
        {
            var first = graph.Edges.Where(e => e.Subject.Value.ToString().Equals(a.Value.ToString()) && e.Obj.Value.ToString().Equals(b.Value.ToString())).FirstOrDefault();
            if (first != null) return first;
            return graph.Edges.Where(e => e.Obj.Value.ToString().Equals(a.Value.ToString()) && e.Subject.Value.ToString().Equals(b.Value.ToString())).FirstOrDefault();
        }

        private void AddPath(Node inputInterfaceNode, Node outputInterfaceNode, Node masterInputNode, Node masterOutNode, List<NodePath> paths)
        {
            if (masterInputNode.Value.Equals(masterOutNode.Value)) return;
            //TODO: think about this - it can happen if you have an orphend edge 
            if (inputInterfaceNode == null || !NodeIndex.ContainsKey(inputInterfaceNode.Value)) return;
            if (outputInterfaceNode == null || !NodeIndex.ContainsKey(outputInterfaceNode.Value)) return;
            inputInterfaceNode = NodeIndex[inputInterfaceNode.Value];
            outputInterfaceNode = NodeIndex[outputInterfaceNode.Value];
            masterInputNode = NodeIndex[masterInputNode.Value];
            masterOutNode = NodeIndex[masterOutNode.Value];
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
                        var p = path.ToList();
                        //TODO: make these calls simpler and neater
                        if (!inputInterfaceNode.Value.Equals(masterInputNode.Value))
                        {
                            Edge edge = GetEdge(masterInputNode, inputInterfaceNode);
                          
                            if (edge != null)
                            {
                                p.Insert(0, edge);
                            }
                            else
                            {
                                //p.Insert(0,new Edge(masterInputNode, Node("to"), inputInterfaceNode));
                                throw new InvalidOperationException($"could not link {masterInputNode} to {inputInterfaceNode}");
                            }
                            
                        }
                        if (outputInterfaceNode != masterOutNode)
                        {
                            Edge edge = GetEdge(outputInterfaceNode, masterOutNode);
                            if (edge != null)
                            {
                                p.Add(edge);
                            }
                            else
                            {
                                if (masterOutNode.IsGraphNet)
                                {
                                    p.Add(new Edge(outputInterfaceNode, Node("in"), masterOutNode));
                                }
                                else
                                {
                                    throw new InvalidOperationException($"could not link {masterOutNode} to {outputInterfaceNode}");
                                }
                            }
                        }
                        paths.Add(new NodePath(p));
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
            if (Inputs.Contains(node))
            {
                Inputs.Remove(node);
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
            if (results.Count == 0) return null;
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
            if (input == null) return results;
            Outputs.ToList().ForEach(ouput => AddResult(input, ouput, results,1));
            var ordered = results.OrderByDescending(r => r.Probabilty).ThenBy(r => r.Paths.Count == 0 ? int.MaxValue : r.Paths.Count);
            return ordered.ToList();
        }

  

        private void AddResult(Node input, Node ouput, List<Result> results, double parentProbabilty)
        {
            var paths = GetPaths(input, ouput);
            if (net == null)
            {
                net = new Net(GetNodeIndex(), Outputs, MaxPathLenght, MaxNumberOfPaths);
            }
            if (paths.Count >= MaxNumberOfPaths) throw new InvalidOperationException($"too many paths: {paths.Count} > {MaxNumberOfPaths} for {ShortId}");
            var p = net.GetProbabilty(paths, ouput);
            var probabilty = p * parentProbabilty;
            var nested = ouput as GraphNet;
            if (nested == null)
            {
                var result = new Result(input, ouput, paths, probabilty);
                results.Add(result);
            }
            else
            {
                nested.Outputs.ToList().ForEach(o => nested.AddResult(input, o, results, probabilty));
            }
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

        public List<Node> AllOutputs()
        {
            var result = new List<Node>();
            Outputs.ForEach(o =>
            {
                if (o.IsGraphNet)
                {
                    result.AddRange(((GraphNet)o).AllOutputs());
                }
                else
                {
                    result.Add(o);
                }
            });

            return result;
        }
    }
}