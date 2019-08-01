using graph.network.core;
using System.Linq;

namespace graph.network.ld
{
    public class UriNode : Node
    {
        private string shortId;

        public UriNode(string uri) : base(uri)
        {
            shortId = GetShortId(uri);
            Result = shortId;
        }

        public static string GetShortId(string uri)
        {
            var trimmed = uri.TrimEnd('#', '/');
            var start = trimmed.Contains('#') ? trimmed.LastIndexOf('#') : trimmed.LastIndexOf('/');
            return trimmed.Substring(start + 1).Trim();
        }

        public override string ShortId
        {
            get
            {
                return shortId;
            }
        }
    }
}
