using NUnit.Framework;
using PCWarmer.Core.Display;

namespace PCWarmer.Tests.Display;

[TestFixture]
public class FpsCapTextTests
{
    [TestCase(FpsCap.Fps60, ExpectedResult = "60")]
    [TestCase(FpsCap.Fps120, ExpectedResult = "120")]
    [TestCase(FpsCap.Uncapped, ExpectedResult = "Uncapped")]
    [TestCase((FpsCap)0, ExpectedResult = "60")]
    public string For_ReadsLikeTheMenu(FpsCap cap) => FpsCapText.For(cap);
}
