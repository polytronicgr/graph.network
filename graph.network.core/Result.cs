using System.Collections.Generic;

namespace graph.network.core
{
    public class Result
    {
        public Result(Node input, Node output, NodePath path, double probabilty)
        {
            Input = input;
            Output = output;
            Path = path;
            Probabilty = probabilty;
        }

        public double Probabilty { get; }
        public NodePath Path { get; }
        public Node Output { get; }
        public Node Input { get; }
    }
}