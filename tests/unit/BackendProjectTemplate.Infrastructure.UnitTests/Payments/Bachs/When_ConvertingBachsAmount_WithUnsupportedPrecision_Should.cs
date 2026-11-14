using BackendProjectTemplate.Infrastructure.Payments.Bachs;
using Shouldly;

namespace BackendProjectTemplate.Infrastructure.UnitTests.Payments.Bachs;

public sealed class When_ConvertingBachsAmount_WithUnsupportedPrecision_Should
{
    [Fact]
    public void RejectFractionalMinorUnits()
    {
        var action = () => BachsAmountConverter.ToMajorUnitString(2500.5m, "NGN");

        action.ShouldThrow<InvalidOperationException>();
    }
}
