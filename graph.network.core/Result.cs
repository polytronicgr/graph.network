using System.Collections.Generic;

namespace graph.network.core
{
    public class Result
    {
        public Result(Node input, Node output, List<NodePath> paths, double probabilty)
        {
            Input = input;
            Output = output;
            Paths = paths;
            Probabilty = probabilty;
        }

        public double Probabilty { get; }
        public List<NodePath> Paths { get; }
        public Node Output { get; }
        public Node Input { get; }

        public override string ToString()
        {
            if (Paths == null || Paths.Count == 0)
            {
                return $"prob={Probabilty}";
            }
            else
            {
                return $"{Paths[0].ToString()}"; 
            }
            
        }
    }
}