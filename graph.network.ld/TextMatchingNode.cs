using System.Collections.Generic;
using System.Linq;
using graph.network.core;

namespace graph.network.ld
{
    public class TextMatchingNode : UriNode
    {
        public TextMatchingNode(string uri) : base(uri)
        {
        }

        public override void OnAdd(GraphNet net)
        {
            string id = ToString();
            var contains = net.Outputs.Where(o =>
            {
                return o.ToString().Contains(id) && o.ToString() != id;
            });

            foreach (var n in contains)
            {
                AddEdge(net.Node("couldBe"), n);
            }
            base.OnAdd(net);
        }

        public override IEnumerable<Node> GetInterface()
        {
            return new List<Node> {this};
        }
    }
}
