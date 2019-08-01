using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.Double;
using System;
using System.Collections.Generic;
using System.Linq;

namespace graph.network.core
{
    public class Net
    {
        private readonly List<Node> nodeIndex;
        private readonly List<Node> outputNodes;
        private readonly int numberOfNodes;
        private readonly int maxPathLenght;
        private readonly int maxNumberOfPaths;
        private readonly int numberOfClasses;
        private readonly int width;
        private readonly int height;
        private readonly int depth;
        private List<(List<NodePath>, Node)> _data = new List<(List<NodePath>, Node)>();
        private List<int> lables = new List<int>();
        private List<double[,,]> featureData = new List<double[,,]>();
        private Net<double> net;

        public Net(List<Node> nodeIndex, List<Node> outputNodes, int maxPathLenght, int maxNumberOfPaths)
        {
            this.nodeIndex = nodeIndex;
            this.outputNodes = outputNodes;
            this.numberOfNodes = nodeIndex.Count;
            this.maxPathLenght = maxPathLenght;
            this.maxNumberOfPaths = maxNumberOfPaths;
            this.numberOfClasses = outputNodes.Count;
            this.width = maxPathLenght;
            this.height = numberOfNodes;
            this.depth = maxNumberOfPaths;
        }


        public void AddToTraining(NodePath path, Node output)
        {
            AddToTraining(new List<NodePath>{path}, output);
        }
        public void AddToTraining(List<NodePath> paths, Node output)
        {
            _data.Add((paths, output));
            double[,,] data = GetFeatureData(paths);
            int lable = outputNodes.IndexOf(output);
            lables.Add(lable);
            featureData.Add(data);
        }

        public IEnumerable<List<NodePath>> GetTraingData(Node output)
        {
            var data = _data.Where(e => e.Item2 == output).Select(e => e.Item1);
            return data;
        }

        public double[,,] GetFeatureData(List<NodePath> paths)
        {
            var result = new double[maxPathLenght, numberOfNodes, maxNumberOfPaths];
            if (paths.Count >= maxNumberOfPaths) throw new InvalidOperationException($"too many paths: {paths.Count} > {maxNumberOfPaths}");
            for (int p = 0; p < paths.Count; p++)
            {
                var path = paths[p];
                for (int i = 0; i < path.Count; i++)
                {
                    if (path.Count >= maxPathLenght) throw new InvalidOperationException($"path is too long {path.Count} > {maxPathLenght}");
                    Node node = path[i];
                    var index = nodeIndex.IndexOf(node);
                    if (index != -1)
                    {
                        result[i, index, p] = 1; //just one hot vector at the mo
                    }
                }
            }
            return result;
        }

        public void Train()
        {
            var batchSize = lables.Count;
            if (batchSize == 0) return;
            // specifies a 2-layer neural network with one hidden layer of 20 neurons
            net = new Net<double>();
            net.AddLayer(new InputLayer(width, height, depth));

            // declare 20 neurons
            net.AddLayer(new FullyConnLayer(20));

            // declare a ReLU (rectified linear unit non-linearity)
            net.AddLayer(new ReluLayer());

            // declare a fully connected layer that will be used by the softmax layer
            net.AddLayer(new FullyConnLayer(numberOfClasses));

            // declare the linear classifier on top of the previous hidden layer
            net.AddLayer(new SoftmaxLayer(numberOfClasses));

            var trainer = new AdamTrainer<double>(net) { LearningRate = 0.01, BatchSize = batchSize};

            var netx = BuilderInstance.Volume.SameAs(new Shape(width, height, depth, batchSize));
            var hotLabels = BuilderInstance.Volume.SameAs(new Shape(1, 1, numberOfClasses, batchSize));

            for (int i = 0; i < batchSize; i++)
            {
                var lable = lables[i];
                hotLabels.Set(0, 0, lable, i, 1.0);
                for (int w = 0; w < width; w++)
                {
                    for (int h = 0; h < height; h++)
                    {
                        for (int d = 0; d < depth; d++)
                        {
                            var dataItem = featureData[i][w, h, d];
                            netx.Set(w,h,d,i,dataItem);
                        }
                    }
                }
            }

            for (int i = 0; i < 50; i++)
            {
                trainer.Train(netx, hotLabels);
                //Console.WriteLine($"loss {trainer.Loss}");
            }
        }
        public double GetProbabilty(NodePath path, Node output)
        {
            if (path == null) return 0;
            return GetProbabilty(new List<NodePath> { path }, output);
        }
        public double GetProbabilty(List<NodePath> paths, Node output)
        {
            if (net == null) return 0;
            if (paths.Count == 0) return 0;
            if (!outputNodes.Contains(output)) return 0;
            var featureData = GetFeatureData(paths);

            var shape = new Shape(width, height, depth);
            var data = new double[shape.TotalLength];
            var vol = BuilderInstance.Volume.From(data, shape);
            for (int w = 0; w < width; w++)
            {
                for (int h = 0; h < height; h++)
                {
                    for (int d = 0; d < depth; d++)
                    {
                        var dataItem = featureData[w, h, d];
                        vol.Set(w, h, d, dataItem);
                    }
                }
            }

            var forward = net.Forward(vol);
            var lable = outputNodes.IndexOf(output);
            var result = forward.Get(lable);
            return result;
        }
    }
}
