using CSharpFunctionalExtensions;

namespace ValueObjects
{
    public sealed record NodeToken
    {
        public string Value { get; }

        private NodeToken(string value)
        {
            Value = value;
        }

        public static Result<NodeToken> Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Result.Failure<NodeToken>("Node token cannot be empty");

            if (value.Length != 36)
                return Result.Failure<NodeToken>("Node token must be 36 characters long");

            // Дополнительные правила валидации
            if (!value.All(char.IsLetterOrDigit))
                return Result.Failure<NodeToken>("Node token can only contain letters and numbers");

            return Result.Success(new NodeToken(value));
        }

        public static implicit operator string(NodeToken token) => token.Value;
    }
}