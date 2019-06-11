using graph.network.core;
using graph.network.core.nodes;
using QuickGraph;
using System.Windows.Controls;

namespace graph.network.wpf.lib
{
    public class PocGraphLayout : GraphSharp.Controls.GraphLayout<Node, Edge, BidirectionalGraph<Node, Edge>> { };
    /// <summary>
    /// Interaction logic for GraphNetView.xaml
    /// </summary>
    public partial class GraphNetView : UserControl
    {
        public GraphNetView()
        {
            InitializeComponent();
            /*
            var gn = new GraphNet("gn", maxPathLenght: 10, maxNumberOfPaths: 5);
            gn.Add("spider_man", "is_a", "super_hero");
            gn.Add("hulk", "is_a", "super_hero");
            gn.Add("green_goblin", "is_a", "super_villain");
            gn.Add("red_king", "is_a", "super_villain");
            gn.Add("super_villain", "is_a", "villain");
            gn.Add("super_hero", "is_a", "hero");
            gn.Add("hero", "is", "good", true);
            gn.Add("hero", "is_not", "bad", true);
            gn.Add("villain", "is", "bad", true);
            gn.Add("villain", "is_not", "good", true);

            //train it with some expected answers
            gn.Train(gn.NewExample("spider_man", "good"), gn.NewExample("green_goblin", "bad"));

            //and it can now predict answers to entities it has not been trained on
           var result = gn.Rank(gn.Node("hulk"))[0];
           */


            //create a small knowlage graph with information about areas and a couple of true/false output nodes
            var gn = new GraphNet("gn", maxNumberOfPaths: 5);
            gn.Add("london", "is_a", "city");
            gn.Add("london", "capital_of", "uk");
            gn.Add("paris", "is_a", "city");
            gn.Add("york", "is_a", "city");
            gn.Add("paris", "capital_of", "france");
            gn.Add("uk", "is_a", "country");
            gn.Add("france", "is_a", "country");
            gn.Add(gn.Node(true), true);
            gn.Add(gn.Node(false), true);

            //register a NLP tokeniser node that creates an edge for each word and 
            //also add these words to the true and false output nodes so that we can
            //map the paths between words: (london >> is_a >> city >> true)
            gn.RegisterDynamic("ask", (node, graph) =>
            {
                var words = node.Value.ToString().Split(' ');
                gn.Node(node, "word", words);
                Node.BaseOnAdd(node, graph);
                gn.Node(true, "word", words);
                gn.Node(false, "word", words);
            });

            //set new nodes to default to creating this 'ask' node
            gn.DefaultInput = "ask";

            //train some examples of true and false statments using the NLP 'ask' node as the input 
            gn.Train(
                  new NodeExample(gn.Node("london is a city"), gn.Node(true))
                , new NodeExample(gn.Node("london is the caplital of uk"), gn.Node(true))
                , new NodeExample(gn.Node("london is the caplital of france"), gn.Node(false))
                , new NodeExample(gn.Node("london is a country"), gn.Node(false))
                , new NodeExample(gn.Node("uk is a country"), gn.Node(true))
                , new NodeExample(gn.Node("uk is a city"), gn.Node(false))
            );

            //now we can ask questions about entities that are in the knowlage graph but the training has not seen
            var result = gn.Rank(gn.Node("paris is a country"))[0];

            Graph = new BidirectionalGraph<Node, Edge>();
            foreach (var path in result.Paths)
            {
                Graph.AddVerticesAndEdgeRange(path.Edges);
                Graph.AddVerticesAndEdge(new Edge(result.Input, gn.Node("in"), path[0]));
                Graph.AddVerticesAndEdge(new Edge(result.Output, gn.Node("out"), path[path.Count -1]));
            }

            graphLayout.DataContext = this;
        }

        public BidirectionalGraph<Node, Edge> Graph { get; private set; }
    }
}
