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

            gn.Train(gn.NewExample("spider_man", "good"), gn.NewExample("green_goblin", "bad"));

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


            //train some examples of true and false statments using the NLP 'ask' node as the input 
            gn.Train(
                  new NodeExample(gn.DynamicNode("ask")("london is a city"), gn.Node(true))
                , new NodeExample(gn.DynamicNode("ask")("london is the caplital of uk"), gn.Node(true))
                , new NodeExample(gn.DynamicNode("ask")("london is the caplital of france"), gn.Node(false))
                , new NodeExample(gn.DynamicNode("ask")("london is a country"), gn.Node(false))
                , new NodeExample(gn.DynamicNode("ask")("uk is a country"), gn.Node(true))
                , new NodeExample(gn.DynamicNode("ask")("uk is a city"), gn.Node(false))
            );

            //now we can ask questions about entities that are in the knowlage graph but the training has not seen
            Assert.AreEqual(true, gn.Predict(gn.DynamicNode("ask")("paris is a city")));
            Assert.AreEqual(false, gn.Predict(gn.DynamicNode("ask")("paris is a country")));
            Assert.AreEqual(true, gn.Predict(gn.DynamicNode("ask")("is france a country ?")));
            Assert.AreEqual(false, gn.Predict(gn.DynamicNode("ask")("france is a city")));
            Assert.AreEqual(true, gn.Predict(gn.DynamicNode("ask")("york is a city")));
            Assert.AreEqual(false, gn.Predict(gn.DynamicNode("ask")("ding-dong is a city")));
            //TODO: Assert.AreEqual("True", gn.Predict(new DynamicNode("paris is the capital of france", tokeniser)).Result());
            //TODO:Assert.AreEqual("False", gn.Predict(new DynamicNode("paris is the capital of the uk", tokeniser)).Result());
        }

        [Test]
        public void Calculator()
        {
            //small graph of operators
            var gn = new GraphNet("gn", maxNumberOfPaths: 10);
            gn.Add(gn.Node("number"));
            gn.Add("+", "a", "operand");
            gn.Add("-", "a", "operand");
            gn.Add("*", "a", "operand");
            gn.Add("x", "same_as", "*");

            //to run the calculations we will need:

            //(1) a NLP the input with numbers marked
            Action<Node, GraphNet> tokeniser = (node, graph) =>
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
            };

            //(2) some output nodes that will be able to do the maths. 
            //Note they store the restuls in the Result property of the node
            Action<Node, GraphNet, Node, List<NodePath>> calculate = (node, graph, input, paths) =>
            {
                //get the x and y edges and their results
                var x1 = node.GetEdgeByPredicate("param1").Obj;
                var x = (decimal?)node.GetEdgeByPredicate("param1").Obj.Result;
                var y = (decimal?)node.GetEdgeByPredicate("param2").Obj.Result;
                //if they do not have numser then this node is not valid
                if (x == null || y == null)
                {
                    paths.Clear(); // none of these paths is valid
                }
                //otherwise do the maths and store the result
                else if (node.Value.ToString() == "add")
                {
                    node.Result = x + y;
                }
                else if (node.Value.ToString() == "minus")
                {
                    node.Result = x - y;
                }
                else if (node.Value.ToString() == "times")
                {
                    node.Result = x * y;
                }
            };

            //(3) some special nodes for the params that are only valid if they start with numbers ...
            Func<Node, GraphNet, NodePath, bool> isNumber = (node, graph, path) =>
            {
                return Node.BaseIsPathValid(node, graph, path) && path[0].Value.ToString().All(char.IsDigit);
            };

            //... and they can pull those numbers out and store them in the nodes result
            Action<Node, GraphNet, Node, List<NodePath>> extractNumber = (node, graph, input, paths) =>
            {
                //get any paths that end in this node whose input has not been used by another node
                var firstPathToThisNode = paths.Where(p=> p[p.Count-1]==node && !p[0].GetProp<bool>("used")).FirstOrDefault(); 
                if (firstPathToThisNode != null)
                {
                    //take the unput number and mark it as used
                    var start = firstPathToThisNode[0];
                    node.Result = decimal.Parse(start.Value.ToString());
                    start.SetProp("used", true);
                }
            };

            //add these complex ouput nodes
            var add = new DynamicNode("add", onProcess: calculate);
            add.AddEdge("param1", new DynamicNode("add_x", isPathValid: isNumber, onProcess: extractNumber), gn).AddEdge("must_be", "number", gn);
            add.AddEdge("param2", new DynamicNode("add_y", isPathValid: isNumber, onProcess: extractNumber), gn).AddEdge("must_be", "number", gn);
            add.AddEdge("opp", "+", gn);
            gn.Add(add, true);

            var minus = new DynamicNode("minus", onProcess: calculate);
            minus.AddEdge("param1", new DynamicNode("minus_x", isPathValid: isNumber, onProcess: extractNumber), gn).AddEdge("must_be", "number", gn);
            minus.AddEdge("param2", new DynamicNode("minus_y", isPathValid: isNumber, onProcess: extractNumber), gn).AddEdge("must_be", "number", gn);
            minus.AddEdge("opp", "-", gn);
            gn.Add(minus, true);

            var times = new DynamicNode("times", onProcess: calculate);
            times.AddEdge("param1", new DynamicNode("times_x", isPathValid: isNumber, onProcess: extractNumber), gn).AddEdge("must_be", "number", gn);
            times.AddEdge("param2", new DynamicNode("times_y", isPathValid: isNumber, onProcess: extractNumber), gn).AddEdge("must_be", "number", gn);
            times.AddEdge("opp", "*", gn);
            gn.Add(times, true);

            //teach it the basic math funcitons
            gn.Train(
                      new NodeExample(new DynamicNode("1 + 2", tokeniser), add)
                    , new NodeExample(new DynamicNode("1 - 2", tokeniser), minus)
                    , new NodeExample(new DynamicNode("1 * 2", tokeniser), times)
                    //, new Example(new DynamicNode("1 x 2", tokeniser), times)
            );

            //test
            Assert.AreEqual(15, gn.Predict(new DynamicNode("5 + 10", tokeniser)));
            Assert.AreEqual(2, gn.Predict(new DynamicNode("5 - 3", tokeniser)));
            Assert.AreEqual(4, gn.Predict(new DynamicNode("what is 5 - 1", tokeniser)));
            Assert.AreEqual(21, gn.Predict(new DynamicNode("3 * 7", tokeniser)));
            //Assert.AreEqual(15, gn.Predict(new DynamicNode("3 x 5", tokeniser)).Result);
        }

        [Test]
        public void TestNestedGraphNets()
        {
            //=================================================================
            //create a GraphNet for predicting if a super is good or bad
            //=================================================================
            var supers = new GraphNet("supers");
            supers.RegisterDynamic("enitiy", (node, graph) => {
                var words = node.Value.ToString().Split('_');
                graph.Node(node, "word", words);
                node.AddEdge("entity_name", graph.Node(node.Value.ToString().Replace("_", " ")), graph);
                Node.BaseOnAdd(node, graph);
            });
            supers.AddDynamic("enitiy", "spider_man", "is_a", "super_hero");
            supers.AddDynamic("enitiy", "hulk", "is_a", "super_hero");
            supers.AddDynamic("enitiy", "green_goblin", "is_a", "super_villain");
            supers.AddDynamic("enitiy", "red_king", "is_a", "super_villain");
            supers.AddDynamic("enitiy", "donald_trump", "is_a", "super_villain");
            supers.Add("super_hero", "is", "good", true);
            supers.Add("super_villain", "is", "bad", true);

            //=================================================================
            //create another GN that can do simple caculations
            //=================================================================
            var calc = new GraphNet("calc");
            calc.Add("add_opp", "lable", "+");
            calc.Add("times_opp", "lable", "*");
            calc.Add("minus_opp", "lable", "-");
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

            calc.Add(new DynamicNode("times"
                , onProcess: (node, graph, input, paths) => node.Result = pullNumbers(paths).Aggregate(1, (acc, val) => acc * val))
                , true);
            calc.Node("times").AddEdge("input", "number", calc);
            calc.Node("times").AddEdge("opp", "times_opp", calc);

            calc.Add(new DynamicNode("minus"
            , onProcess: (node, graph, input, paths) => {
                var n = pullNumbers(paths);
                if (n.Count() > 0) {
                    node.Result = n.Aggregate((a, b) => a - b);
                }
            })
            , true);
            calc.Node("minus").AddEdge("input", "number", calc);
            calc.Node("minus").AddEdge("opp", "minus_opp", calc);

            //=================================================================
            //create a simple NLP GN for parsing text
            //=================================================================
            var nlp = new GraphNet("nlp");
            nlp.Add(new DynamicNode("nlp_out", (node, graph) => {
                node.Result = graph.AllEdges();
            }), true);

            nlp.RegisterDynamic("text", (node, graph) =>
            {
                //add nodes for each word
                var words = node.Value.ToString().Split(' ');
                nlp.Node(node, "word", words);
                //mark any of those words that are numbers by adding an edge to the number node
                foreach (var e in node.Edges.Where(e => e.Obj.Value.ToString().All(char.IsDigit)))
                {
                    e.Obj.AddEdge(nlp.Node("a"), nlp.Node("number"));
                }

                Node.BaseOnAdd(node, graph);
            });
            //=================================================================
            //now we create the master GraphNet and add these sub ones to it
            //=================================================================
            //TODO: why are there so many paths???
            var gn = new GraphNet("gn", maxNumberOfPaths: 40);
            gn.RegisterDynamic("ask", (node , graph) => {
                Node ask = nlp.DynamicNode("text")(node.Value.ToString());
                //TODO: this would be better: node.AddEdge(graph.Node("nlp"), ask);
                nlp.Add(ask);
                node.Edges = nlp.AllEdges();
                nlp.Remove(ask);
                Node.BaseOnAdd(node, graph);
            });
            gn.DefaultInput = "ask";
            gn.Add(supers, true);
            gn.Add(calc, true);

            //we train it with some examples
            gn.Train(
                  new Example(gn.Node("2 * 3"), 6) 
                , new Example(gn.Node("4 + 1"), 5)
                , new Example(gn.Node("6 - 5"), 1)
                , new Example(gn.Node("spider man"), "good")
                , new Example(gn.Node("green goblin"), "bad")
            );

            //and then this one net can aswer both typoes of question
            Assert.AreEqual("good", gn.Predict("hulk"));
            Assert.AreEqual("bad", gn.Predict("red king"));
            Assert.AreEqual(17, gn.Predict("5 + 12"));
            Assert.AreEqual(60, gn.Predict("5 * 12"));
            Assert.AreEqual(1, gn.Predict("what is 10 - 9"));
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