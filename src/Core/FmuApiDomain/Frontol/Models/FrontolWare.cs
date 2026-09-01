namespace FmuApiDomain.Frontol.Models;

public record FrontolWare
{
    public int Id { get; init; }
    public int Code { get; init; }
    public string Name { get; init; } = string.Empty;
}
