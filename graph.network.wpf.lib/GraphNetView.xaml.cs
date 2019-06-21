using graph.network.core;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace graph.network.wpf.lib
{
    public class GraphNetViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private string _input;
        private GraphNet net;
        private Node _output;
        private List<Result> results;

        public BidirectionalGraph<Node, Edge> Graph { get; private set; } = new BidirectionalGraph<Node, Edge>();
        public ObservableCollection<Node> Outputs { get; private set; }

        public List<Result> Examples { get; private set; }

        public GraphNet Net
        {
            get => net;
            set
            {
                net = value;
                Graph = new BidirectionalGraph<Node, Edge>();
                Graph.AddVerticesAndEdgeRange(net.AllEdges());
                Outputs = new ObservableCollection<Node>(net.Outputs);
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Graph)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Outputs)));
            }
        }

        public string Input
        {
            get { return _input; }
            set
            {
                //if (_input == value) return;
                _input = value;
                var input = Net.Node(value);
                results = Net.Rank(input);
                Outputs = new ObservableCollection<Node>(Net.Outputs);
                var orderedOuputs = results.Select(r => r.Output).ToList();
                Outputs = new ObservableCollection<Node>(Outputs.OrderBy(i =>
                {
                    var index = orderedOuputs.IndexOf(i);
                    if (index == -1) return int.MaxValue;
                    return index;
                }).ToList());
                if (results.Count > 0)
                {
                    var result = results[0];
                    SetResult(result);
                }
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Outputs)));

            }
        }

        private void SetResult(Result result)
        {
            Graph = new BidirectionalGraph<Node, Edge>();
            if (result != null)
            {
                foreach (var path in result.Paths)
                {
                    Graph.AddVerticesAndEdgeRange(path.Edges);
                    Graph.AddVerticesAndEdge(new Edge(result.Input, Net.Node("in"), path[0]));
                    Graph.AddVerticesAndEdge(new Edge(path[path.Count - 1], Net.Node("out"), result.Output));
                    if (result.Output.Result != null && result.Output.Result.ToString() != result.Output.ShortId)
                    {
                        Graph.AddVerticesAndEdge(new Edge(result.Output, Net.Node("result"), new Node(result.Output.Result)));
                    }
                }
            }

            PropertyChanged(this, new PropertyChangedEventArgs(nameof(Graph)));
            Examples = Net.GetExamples(result);
            PropertyChanged(this, new PropertyChangedEventArgs(nameof(Examples)));
        }

        public Node Output
        {
            get { return _output; }
            set
            {
                _output = value;
                if (results != null)
                {
                    var result =  results.FirstOrDefault(r => r.Output == value);
                    SetResult(result);
                }
            }
        }

        public void OnNodeDoubleClick(Node node)
        {
            if (node != null && node.IsGraphNet)
            {
                var graphNet = (GraphNet)node;
                var g = new BidirectionalGraph<Node, Edge>();
                //TODO: need to find a way of just gettin paths from the inputs (need to think about this)
                g.AddVerticesAndEdgeRange(graphNet.AllEdges());
                Graph = g;
                Outputs = new ObservableCollection<Node>(graphNet.Outputs);
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Graph)));
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Outputs)));

            }
        }
    }

    public class PocGraphLayout : GraphSharp.Controls.GraphLayout<Node, Edge, BidirectionalGraph<Node, Edge>> { };
    /// <summary>
    /// Interaction logic for GraphNetView.xaml
    /// </summary>
    public partial class GraphNetView : UserControl
    {
        private GraphNetViewModel model = new GraphNetViewModel();
        public GraphNetView()
        {
            InitializeComponent();
            DataContext = model;
            model.PropertyChanged += Model_PropertyChanged;
        }

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(model.Graph))
            {
                graphLayout.Graph = model.Graph;
            }
            if (e.PropertyName == nameof(model.Outputs))
            {
                outputs.ItemsSource = model.Outputs;
            }
            if (e.PropertyName == nameof(model.Examples))
            {
                examples.ItemsSource = model.Examples;
            }
        }

        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                model.Input = inputText.Text;
            }
        }
        private void OnNodeDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1) return;
            var conrol = sender as StackPanel;
            var node = conrol?.DataContext as Node;
            model.OnNodeDoubleClick(node);
        }

        public GraphNet Net
        {
            get => model.Net;
            set
            {
                model.Net = value;
            }
        }
    }
}
