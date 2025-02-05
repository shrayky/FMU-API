namespace Common
{
    public abstract record BaseResponse
    {
        public string? ErrorMessage { get; init; }
        public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);

        protected static T CreateError<T>(string message) where T : BaseResponse, new()
        {
            return new() { ErrorMessage = message };
        }
    }
}
