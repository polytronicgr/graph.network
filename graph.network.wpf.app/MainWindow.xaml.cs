using graph.network.core;
using graph.network.ld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace graph.network.wpf.app
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //test.Net = LoadGraphNet();
            //test.Net = LoadSparqlGraphNet();
            test.Net = LoadPredicateGraph();
        }
        private GraphNet LoadSparqlGraphNet()
        {
            var gn = new SparqlSyntaxNet();

            gn.TrainFromQueries(
                "select * where { ?s ?p ?o }",
                "select ?p where { ?s ?p ?o }",
                "select * where { ?s <http://test.com/name> ?name }",
                "select * where { ?s <http://test.com/other> ?other }"
                );

            return gn;
        }

        private GraphNet LoadPredicateGraph()
        {
            var gn = new PredicateNet("p:", new Uri("http://test.com/places/"), (uri, graph) => {
                graph.Add("p:name", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:code", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:flag", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Country");
                graph.Add("p:national-anthem", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Country");
                graph.Add("p:capital-city", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Country");
                graph.Add("p:mayor", "http://www.w3.org/2000/01/rdf-schema#domain", "p:City");
                graph.Add("p:Country", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
                graph.Add("p:City", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
                graph.Add("p:Town", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
            });

            gn.TrainFromQueries(
                "select * where { ?s a p:City . ?s p:mayor ?mayor }",
                "select * where { ?s a p:Country . ?s p:flag ?flag }" ,
                "select * where { ?s a p:Country . ?s p:national-anthem ?national-anthem }",
                "select * where { ?s p:name ?name }"
                );

            gn.Add("london", "a", "p:City");
            gn.Add("france", "a", "p:Country");
            gn.Add("farnham", "a", "p:Town");

            return gn;
        }

        private GraphNet LoadGraphNet()
        {

            //1: create a GraphNet for predicting if a super is good or bad
            var supers = new GraphNet("supers", maxPathLenght: 10);
            supers.RegisterDynamic("enitiy", (node, graph) =>
            {
                graph.Node(node, "word", node.ToString().Split('_'));
                Node.BaseOnAdd(node, graph);
            });
            supers.AddDynamic("enitiy", "spider_man", "is_a", "hero");
            supers.AddDynamic("enitiy", "hulk", "is_a", "hero");
            supers.AddDynamic("enitiy", "green_goblin", "is_a", "villain");
            supers.AddDynamic("enitiy", "red_king", "is_a", "villain");
            supers.Add("hero", "is", "good", true);
            supers.Add("villain", "is", "bad", true);

            //2: create a GraphNet that knows about cities
            var cities = new GraphNet("cities", maxNumberOfPaths: 5);
            cities.Add("london", "is_a", "city");
            cities.Add("paris", "is_a", "city");
            cities.Add("uk", "is_a", "country");
            cities.Add("france", "is_a", "country");
            cities.Add(cities.Node(cities.Node(true), "input", "country", "city"));
            cities.Add(cities.Node(cities.Node(false), "input", "country", "city"));
            cities.Outputs.Add(cities.Node(true));
            cities.Outputs.Add(cities.Node(false));

            //3: create a GraphNet that can do caculations
            var calc = new GraphNet("calc", maxPathLenght: 10);
            calc.Add("add_opp", "lable", "+");
            calc.Add("times_opp", "lable", "*");
            calc.Add("times_opp", "lable", "x");
            calc.Add("minus_opp", "lable", "-");
            calc.Add(new Node("number"));
            calc.Add(new Node("a"));
            calc.Add(new Node("word"));

            Func<List<NodePath>, IEnumerable<int>> pullNumbers = (paths) =>
            {
                var numbers = paths.Where(p => p[0].Value.ToString() != "" && p[0].Value.ToString().All(char.IsDigit) && p[2].Equals(calc.Node("number")))
                            .Select(p => int.Parse(p[0].Value.ToString()));
                return numbers;
            };
            calc.Add(new DynamicNode("sum"
                , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Sum())
                , true);
            calc.Node("sum").AddEdge("input", "number", calc);
            calc.Node("sum").AddEdge("opp", "add_opp", calc);

            //subtract
            calc.Add(new DynamicNode("minus"
            , onProcess: (node, graph, input, paths) =>
            {
                var n = pullNumbers(paths);
                if (n.Count() > 0)
                {
                    node.Result = n.Aggregate((a, b) => a - b);
                }
            })
            , true);
            calc.Node("minus").AddEdge("input", "number", calc);
            calc.Node("minus").AddEdge("opp", "minus_opp", calc);

            //multiply
            calc.Add(new DynamicNode("times"
                , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Aggregate(1, (acc, val) => acc * val))
                , true);
            calc.Node("times").AddEdge("input", "number", calc);
            calc.Node("times").AddEdge("opp", "times_opp", calc);

            //4: create a GraphNet for parsing text
            var nlp = new GraphNet("nlp", maxNumberOfPaths: 5, maxPathLenght: 10);
            nlp.Add(new DynamicNode("nlp_out", (node, graph) =>
            {
                node.Result = graph.AllEdges();
            }), true);
            nlp.RegisterDynamic("parse", (node, graph) =>
            {
                //add word nodes and mark if they are numbers
                nlp.Node(node, "word", node.ToString().Split(' '));
                foreach (var word in node.Edges.Select(e => e.Obj).ToList())
                {
                    if (word.ToString().All(char.IsDigit))
                    {
                        word.AddEdge(nlp.Node("a"), nlp.Node("number"));
                    }
                }
                Node.BaseOnAdd(node, graph);
            });

            //5: create the master GraphNet that contains the other GraphNets as nodes within it
            var gn = new GraphNet("gn", maxNumberOfPaths: 5, maxPathLenght: 10);
            gn.RegisterDynamic("ask", (node, graph) =>
            {
                Node ask = nlp.DynamicNode("parse")(node.Value.ToString());
                //TODO: this would be better: node.AddEdge(graph.Node("nlp"), ask);
                nlp.Add(ask);
                node.Edges = nlp.AllEdges();
                nlp.Remove(ask);
                Node.BaseOnAdd(node, graph);
            });
            gn.DefaultInput = "ask";
            gn.Add(supers, true);
            gn.Add(cities, true);
            gn.Add(calc, true);
            gn.LimitNumberOfPaths = true;

            //train the master GraphNet with some examples
            gn.Train(

                new Example(gn.Node("spider man"), "good"),
                new Example(gn.Node("4 + 1"), 5),
                new Example(gn.Node("12 - 10"), 2),
                new Example(gn.Node("5 * 3"), 15),
                new Example(gn.Node("green goblin"), "bad"),
                new Example(gn.Node("london is a city"), true),
                new Example(gn.Node("london is a country"), false),
                new Example(gn.Node("uk is a country"), true),
                new Example(gn.Node("uk is a city"), false)
            );

            return gn;
        }
    }
}
