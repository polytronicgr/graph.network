namespace graph.network.core
{
    public class NodeExample
    {
        public NodeExample(Node input, Node output)
        {
            Input = input;
            Output = output;
        }

        public Node Output { get; }
        public Node Input { get; }
    }
}