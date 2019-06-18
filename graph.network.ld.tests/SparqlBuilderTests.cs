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

            Assert.AreEqual("*", gn.Predict("select"));
            Assert.AreEqual("where", gn.Predict("*"));
            Assert.AreEqual("{", gn.Predict("where"));
            Assert.AreEqual("?s", gn.Predict("{"));
            Assert.AreEqual("?p", gn.Predict("?s"));
            Assert.AreEqual("?o", gn.Predict("?p"));
            Assert.AreEqual("}", gn.Predict("?o"));
            Assert.AreEqual("?p", gn.Predict("distinct"));

        }

        [Test]
        public void TestTextMatcher()
        {
            var gn = new SparqlSyntaxNet();

            gn.TrainFromQueries(
                "select * where { ?s ?p ?o }",
                "select distinct ?p where { ?s ?p ?o }",
                "select * where { ?s <http://test.com/name> ?name }"
                );

            Assert.AreEqual("select", gn.Predict("s"));
        }

        [Test,Ignore("TODO")]
        public void TestTextMatcherFiltersSyntax()
        {
            var gn = new SparqlSyntaxNet();

            gn.TrainFromQueries(
                "select * where { ?s ?p ?o }",
                "select distinct ?p where { ?s ?p ?o }",
                "select * where { ?s <http://test.com/name> ?name }"
                );

            Assert.AreEqual("distinct", gn.Predict("select d"));
        }
    }
}
