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

        [Test]
        public void SuperHeros()
        {
            var gn = new GraphNet();
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

            gn.Train(new Example("spider_man", "good"), new Example("green_goblin", "bad"));

            Assert.AreEqual("good", gn.Predict("hulk").ToString());
            Assert.AreEqual("bad", gn.Predict("red_king").ToString());
        }
        [Test]
        public void BasicExample()
        {
            //set up a simple graph
            var gn = new GraphNet();
            gn.Add("a", "to", "z");
            gn.Add("b", "to", "z");
            gn.Add("c", "to", "z");
            gn.Add("z", "to", "out_a", true);
            gn.Add("z", "to", "out_b", true);
            gn.Add("z", "to", "out_c", true);

            //create some inputs
            var a = new Node("in_a", "to", "a");
            var b = new Node("in_b", "to", "b");
            var c = new Node("in_c", "to", "c");

            //train the net with examples of these inputs to outputs
            gn.Train(new Example(a, "out_a"), new Example(b, "out_b"), new Example(c, "out_c"));

            //create a new input that it has not see but connects to the 'a' node in the graph
            var x = new Node("in_x", "to", "a");
            //the prediction should be that the output is out_a
            Assert.AreEqual("out_a", gn.Predict(x).ToString());
            //same for 'b'
            var y = new Node("in_y", "to", "b");
            Assert.AreEqual("out_b", gn.Predict(y).ToString());
            //same for 'b'
            var z = new Node("in_z", "to", "c");
            Assert.AreEqual("out_c", gn.Predict(z).ToString());
        }

        [Test]
        public void MultiPathExample()
        {
            //set up a simple graph
            var gn = new GraphNet();
            gn.Add("a", "to", "z");
            gn.Add("b", "to", "z");
            gn.Add("c", "to", "z");
            gn.Add("z", "to", "out_a", true);
            gn.Add("x", "to", "out_b", true);
            gn.Add("z", "to", "out_b", true);
            gn.Add("z", "to", "out_c", true);

            //create some inputs
            var a = new Node("in_a", "to", "a");
            var b = new Node("in_b", "to", "b");
            a.AddEdge("to", "x");
            var c = new Node("in_c", "to", "c");

            //train the net with examples of these inputs to outputs
            gn.Train(new Example(a, "out_a"), new Example(b, "out_b"), new Example(c, "out_c"));

            Assert.AreEqual("out_b", gn.Predict(new Node("test", "to", "x")).ToString());
            Assert.AreEqual("out_b", gn.Predict(new Node("test", "to", "b")).ToString());
            Assert.AreEqual("out_c", gn.Predict(new Node("test", "to", "c")).ToString());
        }

        [Test] 
        public void SimpleQuestionAndAnswer()
        {
            //cerate a small knowlage graph with information about areas
            var gn = new GraphNet(maxNumberOfPaths:100);
            gn.Add("london", "is_a", "city");
            gn.Add("london", "capital_of", "uk");
            gn.Add("paris", "is_a", "city");
            gn.Add("paris", "capital_of", "france");
            gn.Add("uk", "is_a", "country");
            gn.Add("britain", "same_as", "uk");
            gn.Add("lon", "same_as", "london");
            gn.Add("france", "is_a", "country");
            //it also has a couple of output nodes for answering yes/no questions
            gn.Add(new Node(true), true);
            gn.Add(new Node(false), true);

            //nodes can be complex and add and remove other nodes and edges from the graph
            //this is a NLP tokeniser that creates an node for each word in a string and 
            //then also adds these words to the true and false output nodes so that we can
            //map the paths between words and thus say if a statement is true or false based
            //on the knowlage contained in the graph
            Action<Node, GraphNet> tokeniser = (node, graph) => {
                //create an edge for every word
                List<Edge> words = node.Value.ToString().Split(' ')
                .Select(
                    s => new Edge(node, new Node("word"), new Node(s)))
                .ToList();
                //set these to be the edges of this node
                node.Edges = words;
                //and add them to the graph
                node.Edges.ForEach(e => graph.Add(e.Obj, false));

                //we also need a way to link words to the true false outputs
                var trueNode = graph.GetNode(true);
                trueNode.Edges = words.Select(e => new Edge(trueNode, e.Predicate, e.Obj)).ToList();
                trueNode.Edges.ForEach(e => graph.Add(e));
                var falseNode = graph.GetNode(false);
                falseNode.Edges = words.Select(e => new Edge(falseNode, e.Predicate, e.Obj)).ToList();
                falseNode.Edges.ForEach(e => graph.Add(e));
            };

            //we also need to remove these nodes afterwards
            Action<Node, GraphNet> onRemove = (node, graph) => {
                //clear the edges from this
                node.Edges.ForEach(e => graph.Remove(e));
                //clear the mirrored dynamic output edges too
                graph.GetNode(true).Edges.ForEach(e => graph.Remove(e));
                graph.GetNode(false).Edges.ForEach(e => graph.Remove(e));
                //finally clear the word nodes themselves
                node.Edges.ForEach(e => 
                {
                    //(as long as they are not entities in the knowlage graph!)
                    if (graph.IsEdgesEmpty(e.Obj))
                    {
                        graph.Remove(e.Obj);
                    }
                });
            };

            //train some examples of true and falase statments 
            gn.Train(
                 new Example(new DynamicNode("london is a city", tokeniser, onRemove), new Node(true))
                ,new Example(new DynamicNode("london is the caplital of uk", tokeniser, onRemove), new Node(true))
                ,new Example(new DynamicNode("london is a country", tokeniser, onRemove), new Node(false))
                ,new Example(new DynamicNode("uk is a country", tokeniser, onRemove), new Node(true))
                ,new Example(new DynamicNode("britain is a country", tokeniser, onRemove), new Node(true))
                ,new Example(new DynamicNode("britain is a city", tokeniser, onRemove), new Node(false))
                ,new Example(new DynamicNode("uk is a city", tokeniser, onRemove), new Node(false))
            );
            
            //now we can ask questions about entities that are in the knowlage graph but the training has not seen
            Assert.AreEqual("True", gn.Predict(new DynamicNode("paris is a city", tokeniser, onRemove)).ToString());
            Assert.AreEqual("False", gn.Predict(new DynamicNode("paris is a country", tokeniser, onRemove)).ToString());
            Assert.AreEqual("True", gn.Predict(new DynamicNode("is france a country ?", tokeniser, onRemove)).ToString());
            Assert.AreEqual("False", gn.Predict(new DynamicNode("france is a city", tokeniser, onRemove)).ToString());
            Assert.AreEqual("True", gn.Predict(new DynamicNode("lon is a city", tokeniser, onRemove)).ToString());
            //TOOD: this example may require path convolution and a deeper net (lon is london + london not a country) Assert.AreEqual("False", gn.Predict(new DynamicNode("lon is a country", tokeniser, onRemove)).ToString());
        }

        
    }
}
