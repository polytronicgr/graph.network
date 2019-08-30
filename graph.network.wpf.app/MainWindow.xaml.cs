using graph.network.core;
using graph.network.ld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
        }

        private GraphNet LoadQueryNet()
        {
            var gn = new QueryGraphNet("Test_Query");
            gn.LimitNumberOfPaths = true;
            gn.PrefixLoader = (uri, graph) =>
            {
                //TODO: I think this loader should just return the nodes should this
                var net = graph as PredicateNet;
                if (net != null)
                {
                    net.Add("p:name", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                    net.Add("p:code", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                    net.Add(net.GetPrefixNamespaceNode(), net.Node("has"), graph.Node("p:name"));
                    net.Add(net.GetPrefixNamespaceNode(), net.Node("has"), graph.Node("p:code"));
                }
    
            };

            gn.RegisterDynamic("parse", (node, graph) => {
                gn.QueryText = node.ShortId.ToString().Replace("http://graph.network.com/ld/", "");
                node.ShortId = gn.QueryText;
                var lastWord = gn.LastWordAsNode();
                if (lastWord != null && node.Edges.Count == 0)
                {
                    node.AddEdge(graph.Node("current"), lastWord);
                }
                Node.BaseOnAdd(node, graph);
            });
            gn.DefaultInput = "parse";

            gn.TrainFromQueries(
             "PREFIX p: <http://test.com/places/> select * where { ?s a p:City . ?s p:mayor ?mayor }",
             "PREFIX p: <http://test.com/places/> select * where { ?s a p:Country . ?s p:flag ?flag }" ,
             "select * where { ?s p:name ?name }",
             "select * where { ?s ?p ?o }"
             );

            return gn;
        }
        private GraphNet LoadSparqlGraphNet()
        {
            var gn = new SparqlSyntaxNet();
            //gn.MaxNumberOfPaths = 5;
            //gn.MaxPathLenght = 10;
            gn.RegisterDynamic("parse", (node, graph) => {
                node.ShortId = node.ShortId.Replace(SparqlSyntaxNet.IRI_PREFIX, "");
                var q = new QueryGraphNet("x") { QueryText = node.ShortId };
                var current = q.LastWordAsNode();
                if (current != null)
                {
                    node.AddEdge(graph.Node("current"), graph.Node(current.ShortId));
                }
                
                Node.BaseOnAdd(node, graph);
            });
            gn.DefaultInput = "parse";

            gn.TrainFromQueries(
                "select * where { ?s ?p ?o }",
                "select ?p where { ?s ?p ?o }",
                "select * where { ?s <http://test.com/name> ?name }",
                "select * where { ?s <http://test.com/other> ?other }"
                );


            return gn;
        }

        private GraphNet LoadCities()
        {
            var gn = new GraphNet("gn", maxNumberOfPaths: 5);
            gn.Add("london", "is_a", "city");
            gn.Add("london", "capital_of", "uk");
            gn.Add("britain", "another_name_for", "uk");
            gn.Add("paris", "is_a", "city");
            gn.Add("york", "is_a", "city");
            gn.Add("york", "is_in", "UK");
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
                Node.BaseOnAdd(gn.Node(true), graph);
                Node.BaseOnAdd(gn.Node(false), graph);
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
                , new NodeExample(gn.Node("birtain is a country"), gn.Node(true))
                , new NodeExample(gn.Node("birtain is a city"), gn.Node(false))
            );
            return gn;
        }

        private GraphNet LoadPredicateGraph()
        {
            var gn = new PredicateNet("p:", new Uri("http://test.com/places/"), (uri, graph) => {
                graph.Add("p:name", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:code", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:flag", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Country");
                graph.Add("p:nationalAnthem", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Country");
                graph.Add("p:capitalCity", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Country");
                graph.Add("p:mayor", "http://www.w3.org/2000/01/rdf-schema#domain", "p:City");
                graph.Add("p:Country", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
                graph.Add("p:City", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
                graph.Add("p:Town", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
            });

            gn.TrainFromQueries(
                "select * where { ?s a p:City . ?s p:mayor ?mayor }",
                "select * where { ?s a p:City . ?s p:code ?code }",
                "select * where { ?s a p:Country . ?s p:flag ?flag }" ,
                "select * where { ?s a p:Country . ?s p:nationalAnthem ?nationalAnthem }",
                "select distinct * where { ?s a p:Country . ?s p:name ?name }",
                "select distinct * where { ?s p:name ?name }",
                "select * where { ?s a p:Place . ?s p:code ?code }"
                );

            gn.Add("london", "a", "p:City");
            gn.Add("france", "a", "p:Country");
            gn.Add("farnham", "a", "p:Town");

            return gn;
        }

        private GraphNet LoadCalcGraph()
        {
            var calc = new GraphNet("calc", maxPathLenght: 20, maxNumberOfPaths: 20);
            calc.LimitNumberOfPaths = true;
            calc.Add("add_opp", "lable", "+");
            calc.Add("times_opp", "lable", "*");
            calc.Add("times_opp", "lable", "x");
            calc.Add("minus_opp", "lable", "-");
            calc.Add(new Node("number"));
            calc.Add(new Node("a"));
            calc.Add(new Node("word"));

            Func<List<NodePath>, IEnumerable<int>> pullNumbers = (paths) =>
            {
                var numbers = paths.Where(p => p[2].Value.ToString() != "" && p[2].Value.ToString().All(char.IsDigit) && p[4].Equals(calc.Node("number")))
                            .Select(p => int.Parse(p[2].Value.ToString())).Distinct();
                return numbers;
            };
            var sum = new DynamicNode("sum"
                        , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Sum());
            sum.AddEdge("input", "number", calc);
            sum.AddEdge("opp", "add_opp", calc);
            calc.Add(sum, true);


            //subtract
            var minus = (new DynamicNode("minus"
            , onProcess: (node, graph, input, paths) =>
            {
                var n = pullNumbers(paths);
                if (n.Count() > 0)
                {
                    node.Result = n.Aggregate((a, b) => a - b);
                }
            }));
            minus.AddEdge("input", "number", calc);
            minus.AddEdge("opp", "minus_opp", calc);
            calc.Add(minus, true);

            //multiply
            var times = new DynamicNode("times"
                , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Aggregate(1, (acc, val) => acc * val));
            times.AddEdge("input", "number", calc);
            times.AddEdge("opp", "times_opp", calc);
            calc.Add(times, true);
      
            calc.RegisterDynamic("parse", (node, graph) =>
            {
                //add nodes for words
                var words = node.Value.ToString().Split(' ');
                graph.Node(node, "word", words);
                //mark any of those words that are numbers by adding an edge to the number node
                foreach (var e in node.Edges.ToArray().Where(e => e.Obj.ToString().All(char.IsDigit)))
                {
                    e.Obj.AddEdge(graph.Node("a"), graph.Node("number"));
                }

                Node.BaseOnAdd(node, graph);
            });

            calc.DefaultInput = "parse";

            calc.Train(new Example(calc.Node("4 + 1"), 5),
          new Example(calc.Node("12 - 10"), 2),
          new Example(calc.Node("5 * 3"), 15));


            return calc;
        }

        public GraphNet LoadSupers()
        {
            var supers = new GraphNet("supers", maxPathLenght: 20);
            supers.LimitNumberOfPaths = true;
            supers.Add("spider_man", "is_a", "hero");
            supers.Add("hulk", "is_a", "hero");
            supers.Add("green_goblin", "is_a", "villain");
            supers.Add("red_king", "is_a", "villain");
            supers.Add("trump", "is_a", "villain");
            supers.Add("hero", "is", "good", true);
            supers.Add("villain", "is", "bad", true);

            supers.Train(new NodeExample(supers.Node("spider_man"), supers.Node("good")),
                new NodeExample(supers.Node("green_goblin"), supers.Node("bad")));
            return supers;
        }

        private GraphNet LoadGraphNet()
        {

            //1: create a GraphNet for predicting if a super is good or bad
            var supers = new GraphNet("supers", maxPathLenght: 20);
            supers.LimitNumberOfPaths = true;
            supers.RegisterDynamic("enitiy", (node, graph) =>
            {
                graph.Node(node, "word", node.ToString().Split('_'));
                Node.BaseOnAdd(node, graph);
            });
            supers.AddDynamic("enitiy", "spider_man", "is_a", "hero");
            supers.AddDynamic("enitiy", "hulk", "is_a", "hero");
            supers.AddDynamic("enitiy", "green_goblin", "is_a", "villain");
            supers.AddDynamic("enitiy", "red_king", "is_a", "villain");
            supers.Add("red", "a", "colour");
            supers.Add("green", "a", "colour");
            supers.Add("blue", "a", "colour");
            supers.Add("hero", "is", "good", true);
            supers.Add("villain", "is", "bad", true);

            //2: create a GraphNet that knows about cities
            var cities = new GraphNet("cities", maxNumberOfPaths: 20);
            cities.Add("london", "is_a", "city");
            cities.Add("paris", "is_a", "city");
            cities.Add("uk", "is_a", "country");
            cities.Add("france", "is_a", "country");
            cities.Add("paris", "capital_of", "france");
            cities.Add("london", "capital_of", "uk");
            var yes = cities.Node(cities.Node(true), "input", "country", "city", "london", "paris", "uk", "france");
            var no = cities.Node(cities.Node(false), "input", "country", "city", "london", "paris", "uk", "france");
            cities.Add(yes, true);
            cities.Add(no, true);

            //3: create a GraphNet that can do caculations
            var calc = new GraphNet("calc", maxPathLenght: 20, maxNumberOfPaths: 20);
            calc.LimitNumberOfPaths = true;
            calc.Add("add_opp", "lable", "+");
            calc.Add("times_opp", "lable", "*");
            calc.Add("times_opp", "lable", "x");
            calc.Add("minus_opp", "lable", "-");
            calc.Add(new Node("number"));
            calc.Add(new Node("a"));
            calc.Add(new Node("word"));

            Func<List<NodePath>, IEnumerable<int>> pullNumbers = (paths) =>
            {
                var numbers = paths.Where(p => p[2].Value.ToString() != "" && p[2].Value.ToString().All(char.IsDigit) && p[4].Equals(calc.Node("number")))
                            .Select(p => int.Parse(p[2].Value.ToString())).Distinct();
                return numbers;
            };
            var sum = new DynamicNode("sum"
                        , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Sum());
            sum.AddEdge("input", "number", calc);
            sum.AddEdge("opp", "add_opp", calc);
            calc.Add(sum, true);


            //subtract
            var minus = (new DynamicNode("minus"
            , onProcess: (node, graph, input, paths) =>
            {
                var n = pullNumbers(paths);
                if (n.Count() > 0)
                {
                    node.Result = n.Aggregate((a, b) => a - b);
                }
            }));
            minus.AddEdge("input", "number", calc);
            minus.AddEdge("opp", "minus_opp", calc);
            calc.Add(minus, true);

            //multiply
            var times = new DynamicNode("times"
                , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Aggregate(1, (acc, val) => acc * val));
            times.AddEdge("input", "number", calc);
            times.AddEdge("opp", "times_opp", calc);
            calc.Add(times, true);

            //4: create a GraphNet for parsing text
            var nlp = new GraphNet("nlp", maxNumberOfPaths: 10, maxPathLenght: 10);
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
                        var edge = new Edge(word, nlp.Node("a"), nlp.Node("number"))
                        {
                            Internal = true
                        };
                        word.Edges.Add(edge);
                    }
                }
                Node.BaseOnAdd(node, graph);
            });

            //5: create the master GraphNet that contains the other GraphNets as nodes within it
            var gn = new GraphNet("Master", maxNumberOfPaths: 20, maxPathLenght: 20);
            gn.LimitNumberOfPaths = true;
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
                new Example(gn.Node("green goblin"), "bad"),
                new Example(gn.Node("4 + 1"), 5),
                new Example(gn.Node("12 - 10"), 2),
                new Example(gn.Node("5 * 3"), 15),
                new Example(gn.Node("london is a city"), true),
                new Example(gn.Node("london is a country"), false),
                new Example(gn.Node("uk is a country"), true),
                new Example(gn.Node("uk is a city"), false)
            );

            return gn;
        }

        private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            /*
            <ComboBoxItem  Name="cbi1">Super Heros (type: spider man)</ComboBoxItem>
            <ComboBoxItem  Name="cbi2">Cities (type: paris is city)</ComboBoxItem>
            <ComboBoxItem  Name="cbi3">Calulator (type: 4 + 3)</ComboBoxItem>
            <ComboBoxItem  Name="cbi4">Multi Graph Example (type: london is a city or type: 3 * 4 etc)</ComboBoxItem>
            <ComboBoxItem  Name="cbi5">Simple Sparql Syntax (type: select * where)</ComboBoxItem>
            <ComboBoxItem  Name="cbi6">Sparql Query Builder (type: select * where)</ComboBoxItem>
             */
            if (e.AddedItems.Contains(cbi1))
            {
                Dispatcher.BeginInvoke(new Action(()=> test.Net = LoadSupers() ));
            }
            else if (e.AddedItems.Contains(cbi2))
            {
                Dispatcher.BeginInvoke(new Action(() => test.Net = LoadCities()));
            }
            else if (e.AddedItems.Contains(cbi3))
            {
                Dispatcher.BeginInvoke(new Action(() => test.Net = LoadCalcGraph()));
            }
            else if (e.AddedItems.Contains(cbi4))
            {
                Dispatcher.BeginInvoke(new Action(() => test.Net = LoadGraphNet()));
            }
            else if (e.AddedItems.Contains(cbi5))
            {
                Dispatcher.BeginInvoke(new Action(() => test.Net = LoadSparqlGraphNet()));
            }
            else if (e.AddedItems.Contains(cbi6))
            {
                Dispatcher.BeginInvoke(new Action(() => test.Net = LoadQueryNet()));
            }

            var item = modelCombo.SelectedItem as ComboBoxItem;
            if (item == null)
            {
                test.InputText = "";
            }
            else
            {
                test.InputText = item.Tag.ToString();
            }
            
        }
    }
}
