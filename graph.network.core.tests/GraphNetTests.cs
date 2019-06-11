using graph.network.core.nodes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace graph.network.core.tests
{
    [TestFixture]
    public class GraphNetTests
    {
        //TODO: simplify tests again (new way of doing calc + use new default input)
        //TODO: run through todos
        //TODO: add UI
        //TODO: paris is the capital of france and 3 x 5
        //TODO: A/B testing of nodes (training based on hard coded)
        //TODO: think about issues with add/registering nodes 
        //TODO: review calculator and try to simplify it again
        //TODO: performace test

        [Test]
        public void SuperHeros()
        {
            //create a small knowlage graph
            var gn = new GraphNet("gn", maxPathLenght:10, maxNumberOfPaths: 5);
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
            Assert.AreEqual("good", gn.Predict("hulk"));
            Assert.AreEqual("bad", gn.Predict("red_king"));
        }

        [Test]
        public void SimpleQuestionAndAnswer()
        {
            //create a small knowlage graph with information about areas and a couple of true/false output nodes
            var gn = new GraphNet("gn", maxNumberOfPaths: 10);
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
            Assert.AreEqual(true, gn.Predict("paris is a city"));
            Assert.AreEqual(false, gn.Predict("paris is a country"));
            Assert.AreEqual(true, gn.Predict("is france a country ?"));
            Assert.AreEqual(false, gn.Predict("france is a city"));
            Assert.AreEqual(true, gn.Predict("york is a city"));
            Assert.AreEqual(false, gn.Predict("ding-dong is a city"));
            //TODO: Assert.AreEqual("True", gn.Predict(new DynamicNode("paris is the capital of france", tokeniser)).Result());
            //TODO:Assert.AreEqual("False", gn.Predict(new DynamicNode("paris is the capital of the uk", tokeniser)).Result());
        }

        [Test]
        public void Calculator()
        {
            //create small graph of operators
            var gn = new GraphNet("calc");
            gn.Add("add_opp", "lable", "+");
            gn.Add("times_opp", "lable", "*");
            gn.Add("times_opp", "lable", "x");
            gn.Add("minus_opp", "lable", "-");
            gn.Add(new Node("number"));

            //and some output nodes that can do maths opperations (add,subtract and multiply)
            //these nodes pull all the numbers from the paths through the graph and apply their operation on them
            Func<List<NodePath>, IEnumerable<int>> pullNumbers = (paths) =>
            {
                var numbers = paths.Where(p => p[0].Value.ToString().All(char.IsDigit) && p[2].Equals(gn.Node("number")))
                            .Select(p => int.Parse(p[0].Value.ToString()));
                return numbers;
            };

            //add
            gn.Add(new DynamicNode("sum"
                , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Sum())
                , true);
            gn.Node("sum").AddEdge("input", "number", gn);
            gn.Node("sum").AddEdge("opp", "add_opp", gn);

            //multiply
            gn.Add(new DynamicNode("times"
                , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Aggregate(1, (acc, val) => acc * val))
                , true);
            gn.Node("times").AddEdge("input", "number", gn);
            gn.Node("times").AddEdge("opp", "times_opp", gn);

            //subtract
            gn.Add(new DynamicNode("minus"
            , onProcess: (node, graph, input, paths) => {
                var n = pullNumbers(paths);
                if (n.Count() > 0)
                {
                    node.Result = n.Aggregate((a, b) => a - b);
                }
            })
            , true);
            gn.Node("minus").AddEdge("input", "number", gn);
            gn.Node("minus").AddEdge("opp", "minus_opp", gn);

            //then we register a tokeniser that adds the word nodes and marks any numbers
            gn.RegisterDynamic("parse", (node, graph) =>
            {
                //add nodes for words
                var words = node.Value.ToString().Split(' ');
                gn.Node(node, "word", words);
                //mark any of those words that are numbers by adding an edge to the number node
                foreach (var e in node.Edges.Where(e => e.Obj.Value.ToString().All(char.IsDigit)))
                {
                    e.Obj.AddEdge(gn.Node("a"), gn.Node("number"));
                }

                Node.BaseOnAdd(node, graph);
            });

            //teach it the basic math funcitons
            gn.Train(
                      new NodeExample(gn.DynamicNode("parse")("1 + 2"), gn.Node("sum"))
                    , new NodeExample(gn.DynamicNode("parse")("1 - 2"), gn.Node("minus"))
                    , new NodeExample(gn.DynamicNode("parse")("1 * 2"), gn.Node("times"))
            );

            //test
            Assert.AreEqual(15, gn.Predict(gn.DynamicNode("parse")("5 + 10")));
            Assert.AreEqual(2, gn.Predict(gn.DynamicNode("parse")("5 - 3")));
            Assert.AreEqual(4, gn.Predict(gn.DynamicNode("parse")("what is 5 - 1")));
            Assert.AreEqual(21, gn.Predict(gn.DynamicNode("parse")("3 * 7")));
            Assert.AreEqual(15, gn.Predict(gn.DynamicNode("parse")("3 x 5")));
        }

        [Test]
        public void TestNestedGraphNets()
        {
            //GraphNets are nodes themselves so they can be added into another GraphNet 

            //1: create a GraphNet for predicting if a super is good or bad
            var supers = new GraphNet("supers");
            supers.RegisterDynamic("enitiy", (node, graph) => {
                graph.Node(node, "word", node.ToString().Split('_'));
                Node.BaseOnAdd(node, graph);
            });
            supers.AddDynamic("enitiy", "spider_man", "is_a", "hero");
            supers.AddDynamic("enitiy", "hulk", "is_a", "hero");
            supers.AddDynamic("enitiy", "green_goblin", "is_a", "villain");
            supers.AddDynamic("enitiy", "red_king", "is_a", "villain");
            supers.Add("hero", "is", "good", true);
            supers.Add("villain", "is", "bad", true);

            //2: create a GraphNet that can do caculations
            var calc = new GraphNet("calc");
            calc.Add("add_opp", "lable", "+");
            calc.Add(new Node("number"));
            Func<List<NodePath>, IEnumerable<int>> pullNumbers = (paths) =>
            {
                var numbers = paths.Where(p => p[0].Value.ToString().All(char.IsDigit) && p[2].Equals(calc.Node("number")))
                            .Select(p => int.Parse(p[0].Value.ToString()));
                return numbers;
            };
            calc.Add(new DynamicNode("sum"
                , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Sum())
                , true);
            calc.Node("sum").AddEdge("input", "number", calc);
            calc.Node("sum").AddEdge("opp", "add_opp", calc);

            //3: create a GraphNet for parsing text
            var nlp = new GraphNet("nlp");
            nlp.Add(new DynamicNode("nlp_out", (node, graph) => {
                node.Result = graph.AllEdges();
            }), true);

            nlp.RegisterDynamic("parse", (node, graph) =>
            {
                //add word nodes and mark if they are numbers
                nlp.Node(node, "word", node.ToString().Split(' '));
                foreach (var word in node.Edges.Select(e => e.Obj).ToList())
                {
                    var type = word.ToString().All(char.IsDigit) ? "number" : "word";
                    word.AddEdge(nlp.Node("a"), nlp.Node( type));
                }
                Node.BaseOnAdd(node, graph);
            });

            //4: create the master GraphNet that contains the others as nodes within it
            //TODO: why are there so many paths???
            var gn = new GraphNet("gn", maxNumberOfPaths: 25);
            gn.RegisterDynamic("ask", (node , graph) => {
                Node ask = nlp.DynamicNode("parse")(node.Value.ToString());
                //TODO: this would be better: node.AddEdge(graph.Node("nlp"), ask);
                nlp.Add(ask);
                node.Edges = nlp.AllEdges();
                nlp.Remove(ask);
                Node.BaseOnAdd(node, graph);
            });
            gn.DefaultInput = "ask";
            gn.Add(supers, true);
            gn.Add(calc, true);

            //train the master GraphNet with some examples
            gn.Train(
                  new Example(gn.Node("4 + 1"), 5)
                , new Example(gn.Node("spider man"), "good")
                , new Example(gn.Node("green goblin"), "bad")
            );

            //this master GraphNet can parse text and answer different types of questions:
            Assert.AreEqual("good", gn.Predict("hulk"));
            Assert.AreEqual("bad", gn.Predict("red king"));
            Assert.AreEqual(17, gn.Predict("5 + 12"));
            Assert.AreEqual(27, gn.Predict("7 + 20"));
            //TODO: Assert.AreEqual(27, gn.Predict("7 + 7"));
        }

        [Test]
        public void BasicExample()
        {
            //set up a simple graph
            var gn = new GraphNet("gn", maxPathLenght: 10, maxNumberOfPaths: 5);
            gn.Add("a", "to", "z");
            gn.Add("b", "to", "z");
            gn.Add("c", "to", "z");
            gn.Add("z", "to", "out_a", true);
            gn.Add("z", "to", "out_b", true);
            gn.Add("z", "to", "out_c", true);

            //create some inputs
            var a = gn.Node("in_a", "to", "a");
            var b = gn.Node("in_b", "to", "b");
            var c = gn.Node("in_c", "to", "c");

            //train the net with examples of these inputs to outputs
            gn.Train(gn.NewExample(a, "out_a"), gn.NewExample(b, "out_b"), gn.NewExample(c, "out_c"));

            //create a new input that it has not see but connects to the 'a' node in the graph
            var x = gn.Node("in_x", "to", "a");
            //the prediction should be that the output is out_a
            Assert.AreEqual("out_a", gn.Predict(x));
            //same for 'b'
            var y = gn.Node("in_y", "to", "b");
            Assert.AreEqual("out_b", gn.Predict(y));
            //same for 'b'
            var z = gn.Node("in_z", "to", "c");
            Assert.AreEqual("out_c", gn.Predict(z));
        }

        [Test]
        public void MultiPathExample()
        {
            //set up a simple graph
            var gn = new GraphNet("gn", maxPathLenght: 10, maxNumberOfPaths: 5);
            gn.Add("a", "to", "z");
            gn.Add("b", "to", "z");
            gn.Add("c", "to", "z");
            gn.Add("z", "to", "out_a", true);
            gn.Add("x", "to", "out_b", true);
            gn.Add("z", "to", "out_b", true);
            gn.Add("z", "to", "out_c", true);

            //create some inputs
            var a = gn.Node("in_a", "to", "a");
            var b = gn.Node("in_b", "to", "b");
            var c = gn.Node("in_c", "to", "c");

            //train the net with examples of these inputs to outputs
            gn.Train(gn.NewExample(a, "out_a"), gn.NewExample(b, "out_b"), gn.NewExample(c, "out_c"));

            Assert.AreEqual("out_b", gn.Predict(gn.Node("test", "to", "x")));
            Assert.AreEqual("out_b", gn.Predict(gn.Node("test", "to", "b")));
            Assert.AreEqual("out_c", gn.Predict(gn.Node("test", "to", "c")));
        }
    }
 }