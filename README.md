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