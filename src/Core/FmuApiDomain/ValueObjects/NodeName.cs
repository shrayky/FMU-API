using CSharpFunctionalExtensions;

namespace FmuApiDomain.ValueObjects
{
    public sealed record NodeName
    {
        public string Value { get; }

        private NodeName(string value)
        {
            Value = value;
        }

        public static Result<NodeName> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Failure<NodeName>("Node name cannot be empty");

            if (value.Length > 100)
                return Result.Failure<NodeName>("Node name is too long");

            // Дополнительные правила валидации
            if (!value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                return Result.Failure<NodeName>("Node name contains invalid characters");

            return Result.Success(new NodeName(value));
        }

        public static implicit operator string(NodeName name) => name.Value;
    }
}