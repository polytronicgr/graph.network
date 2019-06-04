﻿using graph.network.core.nodes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace graph.network.core.tests
{
    [TestFixture]
    public class GraphNetTests
    {
        //TODO: look at test and see how things can be simplifed with prototypes
        //TODO: review calculator and try to simplify it again 
        //TODO: review all todos 
        //TODO: add a test of a nested GraphNet
        //TODO: add UI
        //TODO: performace test
        //TODO: paris is the capital of france

        [Test]
        public void SuperHeros()
        {
            var gn = new GraphNet(maxPathLenght:10, maxNumberOfPaths: 5);
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

            Assert.AreEqual("good", gn.Predict("hulk").ToString());
            Assert.AreEqual("bad", gn.Predict("red_king").ToString());
        }

        [Test]
        public void SimpleQuestionAndAnswer()
        {
            //cerate a small knowlage graph with information about areas
            var gn = new GraphNet(maxNumberOfPaths: 10);
            gn.Add("london", "is_a", "city");
            gn.Add("london", "capital_of", "uk");
            gn.Add("paris", "is_a", "city");
            gn.Add("york", "is_a", "city");
            gn.Add("paris", "capital_of", "france");
            gn.Add("uk", "is_a", "country");
            gn.Add("france", "is_a", "country");
            //it also has a couple of output nodes for answering yes/no questions
            gn.Add(gn.Node(true), true);
            gn.Add(gn.Node(false), true);

            //NLP tokeniser that creates an node for each word and 
            //also adds these words to the true and false output nodes so that we can
            //map the paths between words: (london >> is_a >> city >> true)
            Action<Node, GraphNet> tokeniser = (node, graph) =>
            {
                var words = node.Value.ToString().Split(' ');
                gn.Node(node, "word", words);
                Node.BaseOnAdd(node, graph);
                gn.Node(true, "word", words);
                gn.Node(false, "word", words);
            };

            //gn.RegisterDynamic("ask", tokeniser);
            //could make gn a dynamic: gn.ask("london is a city")

            //train some examples of true and false statments 
            gn.Train(
                  new Example(new DynamicNode("london is a city", tokeniser), gn.Node(true))
                , new Example(new DynamicNode("london is the caplital of uk", tokeniser), gn.Node(true))
                , new Example(new DynamicNode("london is the caplital of france", tokeniser), gn.Node(false))
                , new Example(new DynamicNode("london is a country", tokeniser), gn.Node(false))
                , new Example(new DynamicNode("uk is a country", tokeniser), gn.Node(true))
                , new Example(new DynamicNode("uk is a city", tokeniser), gn.Node(false))
            );

            //now we can ask questions about entities that are in the knowlage graph but the training has not seen
            Assert.AreEqual("True", gn.Predict(new DynamicNode("paris is a city", tokeniser)).ToString());
            Assert.AreEqual("False", gn.Predict(new DynamicNode("paris is a country", tokeniser)).ToString());
            Assert.AreEqual("True", gn.Predict(new DynamicNode("is france a country ?", tokeniser)).ToString());
            Assert.AreEqual("False", gn.Predict(new DynamicNode("france is a city", tokeniser)).ToString());
            Assert.AreEqual("True", gn.Predict(new DynamicNode("york is a city", tokeniser)).ToString());
            Assert.AreEqual("False", gn.Predict(new DynamicNode("ding-dong is a city", tokeniser)).ToString());
            //TODO: Assert.AreEqual("True", gn.Predict(new DynamicNode("paris is the capital of france", tokeniser)).ToString());
            //TODO:Assert.AreEqual("False", gn.Predict(new DynamicNode("paris is the capital of the uk", tokeniser)).ToString());
        }

        [Test]
        public void Calculator()
        {
            //small graph of operators
            var gn = new GraphNet(maxNumberOfPaths: 10);
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
                
                //mark any of those words that are numbers
                var numbers = node.Edges
                    .Where(e => e.Obj.Value.ToString().All(char.IsDigit))
                    .Select(e=> e.Obj);
                foreach (var word in numbers)
                {
                    word.AddEdge(gn.Node("a"), gn.Node("number"));
                }

                Node.BaseOnAdd(node, graph);
            };

            //(2) some output nodes that will be able to do the maths
            Action<Node, GraphNet, List<NodePath>> calculate = (node, graph, paths) =>
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
            };

            //(3) some special nodes for the params that are only valid if they start with numbers ...
            Func<Node, GraphNet, NodePath, bool> isNumber = (node, graph, path) =>
            {
                return Node.BaseIsPathValid(node, graph, path) && path[0].Value.ToString().All(char.IsDigit);
            };

            //... and they can pull those numbers out and store them in the nodes result
            Action<Node, GraphNet, List<NodePath>> extractNumber = (node, graph, paths) =>
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

            //teach it the basic math funcitons
            gn.Train(
                    new Example(new DynamicNode("1 + 2", tokeniser), add),
                    new Example(new DynamicNode("1 - 2", tokeniser), minus)
            );

            //test
            Assert.AreEqual(15, gn.Predict(new DynamicNode("5 + 10", tokeniser)).Result);
            Assert.AreEqual(2, gn.Predict(new DynamicNode("5 - 3", tokeniser)).Result);
            Assert.AreEqual(4, gn.Predict(new DynamicNode("what is 5 - 1", tokeniser)).Result);
        }

        [Test]
        public void BasicExample()
        {
            //set up a simple graph
            var gn = new GraphNet(maxPathLenght: 10, maxNumberOfPaths: 5);
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
            Assert.AreEqual("out_a", gn.Predict(x).ToString());
            //same for 'b'
            var y = gn.Node("in_y", "to", "b");
            Assert.AreEqual("out_b", gn.Predict(y).ToString());
            //same for 'b'
            var z = gn.Node("in_z", "to", "c");
            Assert.AreEqual("out_c", gn.Predict(z).ToString());
        }

        [Test]
        public void MultiPathExample()
        {
            //set up a simple graph
            var gn = new GraphNet(maxPathLenght: 10, maxNumberOfPaths: 5);
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

            Assert.AreEqual("out_b", gn.Predict(gn.Node("test", "to", "x")).ToString());
            Assert.AreEqual("out_b", gn.Predict(gn.Node("test", "to", "b")).ToString());
            Assert.AreEqual("out_c", gn.Predict(gn.Node("test", "to", "c")).ToString());
        }
    }
 }