using FmuApiDomain.GisMt.Journal;
using Xunit;

namespace FmuApiApplication.Tests;

public class GisMtReceiptAmountTests
{
    [Theory]
    [InlineData(250, 0.5, 12500)]
    [InlineData(250, 1, 25000)]
    [InlineData(250.5, 1, 25050)]
    [InlineData(1.25, 0.004, 1)]
    public void Сумма_считается_из_цены_за_килограмм_и_количества(double pricePerKg, double quantityKg, int expected)
    {
        Assert.Equal(expected, GisMtReceiptAmount.Calculate((decimal)pricePerKg, (decimal)quantityKg));
    }

    [Fact]
    public void Без_цены_суммы_нет()
    {
        Assert.Null(GisMtReceiptAmount.Calculate(null, 0.5m));
    }

    [Fact]
    public void Без_количества_суммы_нет()
    {
        Assert.Null(GisMtReceiptAmount.Calculate(250m, null));
    }

    [Fact]
    public void Нулевое_количество_это_нет_количества()
    {
        Assert.Null(GisMtReceiptAmount.Calculate(250m, 0m));
    }
}
