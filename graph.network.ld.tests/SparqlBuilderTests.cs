using graph.network.core;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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

        [Test]
        public void TestVocab()
        {
            var gn = new PredicateNet("p:", new Uri("http://test.com/places/"), (uri,graph) => {
                graph.Add("p:name", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:code", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:flag", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Country");
                graph.Add("p:mayor", "http://www.w3.org/2000/01/rdf-schema#domain", "p:City");
                graph.Add("p:Country", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
                graph.Add("p:City", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
            });

            gn.TrainFromQueries(
                "select * where { ?s a p:City . ?s p:mayor ?mayor }",
                "select * where { ?s a p:Country . ?s p:flag ?flag }" //,"select * where { ?s p:name ?name }"
                );

            var city = new UriNode("?s");
            city.AddEdge(new UriNode("a"), new UriNode("http://test.com/places/City"));
            Assert.AreEqual("mayor", gn.Predict(city));

            var country = new UriNode("?s");
            country.AddEdge(new UriNode("a"), new UriNode("http://test.com/places/Country"));
            Assert.AreEqual("flag", gn.Predict(country));
        }

        //node to return return valiable names and shortend predicate names etc from the existing graph
    }
}
