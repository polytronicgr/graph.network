# graph.network

graph.network is a graph convultional neural network library written in C#.

It lets you model any domain as a graph and then train that model to predict answers:

```csharp
//1 - create a graph of superheroes and villains
var gn = new GraphNet("supers");
gn.Add("spider_man", "is_a", "super_hero");
gn.Add("hulk", "is_a", "super_hero");
gn.Add("green_goblin", "is_a", "super_villain");
gn.Add("red_king", "is_a", "super_villain");
gn.Add("super_villain", "is_a", "villain");
gn.Add("super_hero", "is_a", "hero");
gn.Add("hero", "is", "good");
gn.Add("hero", "is_not", "bad");
gn.Add("villain", "is", "bad");
gn.Add("villain", "is_not", "good");

//3 - mark some nodes as possible answers
gn.SetOutputs("good","bad");

//4 - train the model
gn.Train(gn.NewExample("spider_man", "good"), gn.NewExample("green_goblin", "bad"));

//5 - predict answers to questions that have not been directly trained
Assert.AreEqual("good", gn.Predict("hulk"));
Assert.AreEqual("bad", gn.Predict("red_king"));
```

Nodes in the graph can be full C# objects with rich functionally, so traditional object-oriented code exists inside a machine learning framework/graph:
```csharp
            //create a small knowledge graph with information about areas and a couple of true/false output nodes
            var gn = new GraphNet("cities", maxNumberOfPaths: 5);
            gn.Add("london", "is_a", "city");
            gn.Add("london", "capital_of", "uk");
            gn.Add("ny", "capital_of", "usa");
            gn.Add("paris", "is_a", "city");
            gn.Add("york", "is_a", "city");
            gn.Add("paris", "capital_of", "france");
            gn.Add("uk", "is_a", "country");
            gn.Add("france", "is_a", "country");
            gn.Add(gn.Node(true), true);
            gn.Add(gn.Node(false), true);

            //register an NLP tokeniser node that creates an edge for each word and 
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

            //train some examples of true and false statements using the NLP 'ask' node as the input 
            gn.Train(
                  new NodeExample(gn.Node("london is a city"), gn.Node(true))
                , new NodeExample(gn.Node("london is the caplital of uk"), gn.Node(true))
                , new NodeExample(gn.Node("london is the caplital of france"), gn.Node(false))
                , new NodeExample(gn.Node("london is the caplital of usa"), gn.Node(false))
                , new NodeExample(gn.Node("ny is the caplital of usa"), gn.Node(true))
                , new NodeExample(gn.Node("ny is the caplital of uk"), gn.Node(false))
                , new NodeExample(gn.Node("london is a country"), gn.Node(false))
                , new NodeExample(gn.Node("uk is a country"), gn.Node(true))
                , new NodeExample(gn.Node("uk is a city"), gn.Node(false))
                , new NodeExample(gn.Node("unknown is a city"), gn.Node(false))

            );

            //now we can ask questions about entities that are in the knowledge graph but the training has not seen
            Assert.AreEqual(true, gn.Predict("paris is a city"));
            Assert.AreEqual(false, gn.Predict("paris is a country"));
            Assert.AreEqual(true, gn.Predict("is france a country ?"));
            Assert.AreEqual(false, gn.Predict("france is a city"));
            Assert.AreEqual(true, gn.Predict("york is a city"));
            Assert.AreEqual(true, gn.Predict("paris is the capital of france"));
            Assert.AreEqual(false, gn.Predict("paris is the capital of uk"));
```
