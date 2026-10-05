using GitHubReleaseUpdate.Services;
using GitHubReleaseUpdate.Workers;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;

namespace GitHubReleaseUpdate;

public static class GitHubReleaseRegistrationExtension
{
    public static void AddService(IServiceCollection services)
    {
        services.AddSingleton<GitHubReleaseClient>();

        services.AddHostedService<GitHubReleaseUpdateWorker>();

        // Метаданные релиза: таймаут 30 секунд.
        services.AddHttpClient(GitHubReleaseClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(GitHubReleaseClient.UserAgent);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        });

        // Скачивание архива: таймаут 10 минут.
        services.AddHttpClient(GitHubReleaseClient.DownloadHttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromMinutes(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(GitHubReleaseClient.UserAgent);
        });
    }
}
