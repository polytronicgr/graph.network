namespace graph.network.core.tests
{
    public class Example
    {
        public Example(string input, string output) : this(new Node(input), new Node(output))
        {
        }
        public Example(Node input, string output):this(input, new Node(output)) {
        }
        public Example(Node input, Node output)
        {
            this.Input = input;
            this.Output = output;
        }

        public Node Output { get; }
        public Node Input { get; }
    }
}