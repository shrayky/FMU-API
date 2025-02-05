using CSharpFunctionalExtensions;
using FmuApiDomain.Cdn;
using FmuApiDomain.Configuration;
using FmuApiDomain.ValueObjects;

namespace FmuApiDomain.Node.Models
{
    public sealed record NodeDataRequest
    {
        public NodeName Name { get; }
        public NodeToken Token { get; }
        public Parameters Parameters { get; }
        public IReadOnlyList<TrueSignCdn> CdnData { get; }

        private NodeDataRequest(NodeName name, NodeToken token, Parameters parameters, IReadOnlyList<TrueSignCdn> cdnData)
        {
            Name = name;
            Token = token;
            Parameters = parameters;
            CdnData = cdnData;
        }

        public static Result<NodeDataRequest> Create(
            string name,
            string token,
            Parameters parameters,
            IReadOnlyList<TrueSignCdn> cdnData)
        {
            var nodeName = NodeName.Create(name);
            if (nodeName.IsFailure)
                return Result.Failure<NodeDataRequest>(nodeName.Error);

            var nodeToken = NodeToken.Create(token);
            if (nodeToken.IsFailure)
                return Result.Failure<NodeDataRequest>(nodeToken.Error);

            if (parameters is null)
                return Result.Failure<NodeDataRequest>("Parameters cannot be null");

            if (cdnData is null)
                return Result.Failure<NodeDataRequest>("CdnData cannot be null");

            return Result.Success(new NodeDataRequest(
                nodeName.Value,
                nodeToken.Value,
                parameters,
                cdnData));
        }
    }
}
