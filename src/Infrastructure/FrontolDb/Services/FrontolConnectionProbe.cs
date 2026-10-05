using CSharpFunctionalExtensions;
using FmuApiDomain.Configuration.Options;
using FmuApiDomain.Frontol.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FrontolDb.Services;

/// <summary>
/// Проверяет доступность базы Frontol, открывая соединение Firebird по параметрам подключения.
/// </summary>
public class FrontolConnectionProbe : IFrontolConnectionProbe
{
    private const int ConnectionTimeoutSeconds = 5;

    private const string ConnectionTimeoutParameter = "Connection Timeout";

    public async Task<Result> Probe(FrontolConnectionSettings connection, CancellationToken cancellationToken)
    {
        var connectionString = connection.ConnectionStringBuild();

        if (string.IsNullOrEmpty(connectionString))
            return Result.Failure("Укажите путь, пользователя и пароль");

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(ConnectionTimeoutSeconds));

        try
        {
            using var context = new FrontolDbContext(WithConnectionTimeout(connectionString));

            // CanConnectAsync глушит исключения соединения и возвращает false,
            // поэтому соединение открывается напрямую: пользователю нужен текст ошибки Firebird.
            await context.Database.GetDbConnection().OpenAsync(timeoutSource.Token);

            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // останавливается сама служба, а не истёк таймаут проверки
            throw;
        }
        catch (OperationCanceledException)
        {
            // Connection Timeout в строке подключения ожидание недоступного сервера не ограничивает
            return Result.Failure($"Превышено время ожидания подключения ({ConnectionTimeoutSeconds} сек)");
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    private static string WithConnectionTimeout(string connectionString)
    {
        if (connectionString.Contains(ConnectionTimeoutParameter, StringComparison.OrdinalIgnoreCase))
            return connectionString;

        return $"{connectionString}{ConnectionTimeoutParameter}={ConnectionTimeoutSeconds};";
    }
}
