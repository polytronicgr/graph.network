using graph.network.core;
using System.Linq;

namespace graph.network.ld
{
    public class UriNode : Node
    {
        private string shortId;

        public UriNode(string uri) : base(uri)
        {
            var trimmed = uri.TrimEnd('#', '/');
            var start = trimmed.Contains('#') ? trimmed.LastIndexOf('#') : trimmed.LastIndexOf('/');
            shortId = trimmed.Substring(start + 1).Trim();
            Result = shortId;
        }

        //public override bool IsPathValid(GraphNet graph, NodePath path)
        //{
        //    return true;
        //}

        public override string ShortId
        {
            get
            {
                return shortId;
            }
        }
    }
}
