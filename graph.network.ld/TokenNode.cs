using System;
using VDS.RDF.Parsing.Tokens;

namespace graph.network.ld
{
    public class TokenNode : UriNode
    {
        public TokenNode(Uri uri, IToken token) : base(uri.ToString())
        {
            Token = token;
        }

        public IToken Token { get; }
    }
}