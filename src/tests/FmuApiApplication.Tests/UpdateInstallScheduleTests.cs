using FmuApiDomain.Configuration.Options;
using Xunit;

namespace FmuApiApplication.Tests;

public class UpdateInstallScheduleTests
{
    [Fact]
    public void Пустое_расписание_разрешает_установку_в_любое_время()
    {
        Assert.True(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(3, 0), []));
    }

    [Fact]
    public void Интервал_внутри_суток_разрешает_время_внутри_и_запрещает_время_вне()
    {
        ScheduleTime[] intervals = [new() { BeginTime = new TimeOnly(2, 0), EndTime = new TimeOnly(4, 0) }];

        Assert.True(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(3, 0), intervals));
        Assert.False(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(5, 0), intervals));
        Assert.False(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(1, 59, 59), intervals));
        Assert.False(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(4, 0, 1), intervals));
    }

    [Fact]
    public void Границы_интервала_входят_в_окно()
    {
        ScheduleTime[] intervals = [new() { BeginTime = new TimeOnly(2, 0), EndTime = new TimeOnly(4, 0) }];

        Assert.True(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(2, 0), intervals));
        Assert.True(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(4, 0), intervals));
    }

    [Fact]
    public void Интервал_через_полночь_разрешает_вечер_и_ночь_и_запрещает_день()
    {
        ScheduleTime[] intervals = [new() { BeginTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) }];

        Assert.True(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(23, 0), intervals));
        Assert.True(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(1, 0), intervals));
        Assert.False(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(12, 0), intervals));
    }

    [Fact]
    public void Границы_интервала_через_полночь_входят_в_окно()
    {
        ScheduleTime[] intervals = [new() { BeginTime = new TimeOnly(22, 0), EndTime = new TimeOnly(2, 0) }];

        Assert.True(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(22, 0), intervals));
        Assert.True(UpdateInstallSchedule.IsWithinSchedule(new TimeOnly(2, 0), intervals));
    }
}
