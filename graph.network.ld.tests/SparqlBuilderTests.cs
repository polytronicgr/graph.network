using NUnit.Framework;

namespace graph.network.ld.tests
{
    [TestFixture]
    public class SparqlBuilderTests
    {

        [Test]
        public void TestBasicSyntax()
        {
            var gn = new SparqlSyntaxNet();

            gn.TrainFromQueries(
                "select * where { ?s ?p ?o }", 
                "select distinct ?p where { ?s ?p ?o }",
                "select * where { ?s <http://test.com/name> ?name }"
                );

            gn.CurrentQuery = "";
            Assert.AreEqual("*", gn.Predict("select"));
            Assert.AreEqual("where", gn.Predict("*"));
            Assert.AreEqual("{", gn.Predict("where"));
            Assert.AreEqual("?s", gn.Predict("{"));
            Assert.AreEqual("?p", gn.Predict("?s"));
            Assert.AreEqual("?o", gn.Predict("?p"));
            Assert.AreEqual("}", gn.Predict("?o"));
            Assert.AreEqual("?p", gn.Predict("distinct"));

        }
    }
}
