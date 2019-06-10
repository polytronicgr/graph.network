namespace graph.network.core
{
    public class Example
    {
        public object Output { get; }
        public object Input { get; }
        public Example(object input, object output)
        {
            Input = input;
            Output = output;
        }
    }
}