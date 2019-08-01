using graph.network.core;
using NUnit.Framework;
using System;

namespace graph.network.ld.tests
{
    [TestFixture]
    public class SparqlBuilderTests
    {
        [Test]
        public void TestBasicSyntax()
        {
            var gn = new SparqlSyntaxNet();
            gn.MaxNumberOfPaths = 5;
            gn.MaxPathLenght = 10;
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
            gn.MaxNumberOfPaths = 5;
            gn.MaxPathLenght = 10;
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
            //TODO: this is a bit nasty think about it (perhaps the method return nodes and then the graph does the adding??)
            var gn = new PredicateNet("p:", new Uri("http://test.com/places/"), (uri,graph) => {
                var net = graph as PredicateNet;
                graph.Add("p:name", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:code", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:flag", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Country");
                graph.Add("p:mayor", "http://www.w3.org/2000/01/rdf-schema#domain", "p:City");
                graph.Add("p:Country", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
                graph.Add("p:City", "http://www.w3.org/2000/01/rdf-schema#subClassof", "p:Place");
                net.Add(net.GetPrefixNamespaceNode(), net.Node("has"), graph.Node("p:name"));
                net.Add(net.GetPrefixNamespaceNode(), net.Node("has"), graph.Node("p:code"));
                net.Add(net.GetPrefixNamespaceNode(), net.Node("has"), graph.Node("p:flag"));
                net.Add(net.GetPrefixNamespaceNode(), net.Node("has"), graph.Node("p:mayor"));
            });
            gn.MaxNumberOfPaths = 5;
            //gn.MaxPathLenght = 10;
            gn.TrainFromQueries(
                "select * where { ?s a p:City . ?s p:mayor ?mayor }",
                "select * where { ?x a p:City . ?x p:mayor 'ted' }",
                //TODO: this should not throw the prediction off "select * where { ?s a p:City . ?s p:name ?name }",
                "select * where { ?s a p:Country . ?s p:flag ?flag }",
                "select * where { ?x a p:Country . ?x p:flag 'xxx' }"
                );

            //var cityQuery = new Node("select *");
            var city = new UriNode("?x");
            city.UseEdgesAsInterface = false;
            //cityQuery.AddEdge("next", city, gn);
            city.AddEdge(new UriNode("a"), new UriNode("http://test.com/places/City"));
            Assert.AreEqual("mayor", gn.Predict(city));

            //var countryQuery = new Node("select *");
            var country = new UriNode("?x");
            country.UseEdgesAsInterface = false;
            //countryQuery.AddEdge("next", country, gn);
            country.AddEdge(new UriNode("a"), new UriNode("http://test.com/places/Country"));
            Assert.AreEqual("flag", gn.Predict(country));
        }

        //node to return return valiable names and short predicate names etc from the existing graph

        [Test]
        public void TestSimpleQuery()
        {
            var gn = new QueryGraphNet("Test_Query", maxNumberOfPaths:10, maxPathLenght:20);
            gn.LimitNumberOfPaths = true;
            gn.PrefixLoader = (uri, graph) =>
            {
                graph.Add("p:name", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                graph.Add("p:code", "http://www.w3.org/2000/01/rdf-schema#domain", "p:Place");
                var net = graph as PredicateNet;
                net.Add(net.GetPrefixNamespaceNode(), net.Node("has"), graph.Node("p:name"));
                net.Add(net.GetPrefixNamespaceNode(), net.Node("has"), graph.Node("p:code"));
            };

            //TODO: move this code into the QueryGraphNet as really it is core to how it works
            gn.RegisterDynamic("parse", (node, graph) => {
                gn.QueryText = node.ShortId.ToString().Replace("http://graph.network.com/ld/", "");
                var lastWord = gn.LastWordAsNode();
                if (lastWord != null && node.Edges.Count == 0)
                {
                    node.AddEdge(graph.Node("current"), lastWord);
                }
                Node.BaseOnAdd(node, graph);
            });
            gn.DefaultInput = "parse";

            gn.TrainFromQueries(
             "PREFIX p: <http://test.com/places/> select * where { ?s a p:City . ?s p:name ?mayor }",
             "PREFIX p: <http://test.com/places/> select * where { ?s a p:Country . ?s p:name ?flag }"
             );

            Assert.AreEqual("where", gn.Predict("PREFIX p: <http://test.com/places/> SELECT *"));
            Assert.AreEqual("{", gn.Predict("PREFIX p: <http://test.com/places/> SELECT * WHERE"));
            Assert.AreEqual("name", gn.Predict("PREFIX p: <http://test.com/places/> SELECT * WHERE {?s p:}"));
            //TODO: Assert.AreEqual("p:name", gn.Predict("PREFIX p: <http://test.com/places/> SELECT * WHERE {?s }"));
        }
    }
}
