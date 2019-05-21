using NUnit.Framework;

namespace graph.network.core.tests
{
    [TestFixture]
    public class GraphNetTests
    {
        [Test]
        public void Example()
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
    }
}
