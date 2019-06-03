using graph.network.core.nodes;

namespace graph.network.core.tests
{
    public class Example
    {
        public Example(Node input, Node output)
        {
            this.Input = input;
            this.Output = output;
        }

        public Node Output { get; }
        public Node Input { get; }
    }
}