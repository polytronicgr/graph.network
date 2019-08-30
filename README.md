# graph.network
c# graph based neural network library 

You can model any domain as a graph and then train a model to predict answers to questions that draw upon the knowledge contained in that graph.

```csharp
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
```