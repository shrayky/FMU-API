namespace FmuApiApplication.InternetConnectionCheck.Interfaces;

/// <summary>
/// Проверяет доступность интернета по списку хостов и обновляет статус приложения.
/// </summary>
public interface IInternetConnectionCheckService
{
    Task PerformCheck(CancellationToken cancellationToken);
}
