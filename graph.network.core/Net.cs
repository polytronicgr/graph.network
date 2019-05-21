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
        private readonly int pathLenght;
        private readonly int numberOfPaths;
        private readonly int numberOfClasses;
        private readonly int width;
        private readonly int height;
        private readonly int depth;
        private List<(NodePath, Node)> _data = new List<(NodePath, Node)>();
        private List<int> lables = new List<int>();
        private List<double[,,]> featureData = new List<double[,,]>();
        private Net<double> net;

        public Net(List<Node> nodeIndex, List<Node> outputNodes)
        {
            this.nodeIndex = nodeIndex;
            this.outputNodes = outputNodes;
            this.numberOfNodes = nodeIndex.Count;
            this.pathLenght = 20;
            this.numberOfPaths = 1;
            this.numberOfClasses = outputNodes.Count;
            this.width = pathLenght;
            this.height = numberOfNodes;
            this.depth = numberOfPaths;
        }

        public void AddToTraining(NodePath path, Node output)
        {
            _data.Add((path, output));
            double[,,] data = GetFeatureData(path);
            int lable = outputNodes.IndexOf(output);
            lables.Add(lable);
            featureData.Add(data);
        }

        public double[,,] GetFeatureData(NodePath path)
        {
            var result = new double[pathLenght, numberOfNodes, numberOfPaths];
            for (int i = 0; i < path.Count; i++)
            {
                Node node = path[i];
                var index = nodeIndex.IndexOf(node);
                if (index != -1)
                {
                    result[i, index, 0] = 1;
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
            if (!outputNodes.Contains(output)) return 0;
            var featureData = GetFeatureData(path);

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
