using Microsoft.Xna.Framework;
using NUnit.Framework;
using PCWarmer.Core.Ui;

namespace PCWarmer.Tests.Ui;

[TestFixture]
public class UiScaleTests
{
    [TestCase(800, 480, ExpectedResult = 1f)]
    [TestCase(1600, 960, ExpectedResult = 2f)]
    [TestCase(1920, 480, ExpectedResult = 1f)]
    [TestCase(200, 100, ExpectedResult = 0.5f)]
    public float For_FitsTheReferenceCanvas(int width, int height) => UiScale.For(new Point(width, height));
}
