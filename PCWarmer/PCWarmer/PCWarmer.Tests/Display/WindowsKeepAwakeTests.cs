using System.Collections.Generic;
using NUnit.Framework;
using PCWarmer.Core.Display;

namespace PCWarmer.Tests.Display;

[TestFixture]
public class WindowsKeepAwakeTests
{
    [Test]
    public void Begin_AsksForDisplayAndSystemOnce()
    {
        var calls = new List<uint>();
        var keepAwake = new WindowsKeepAwake(flags => { calls.Add(flags); return 1; });

        keepAwake.Begin();
        keepAwake.Begin();

        Assert.That(calls, Is.EqualTo(new[] { 0x80000003u }));
    }

    [Test]
    public void End_ReleasesOnceAndOnlyAfterBegin()
    {
        var calls = new List<uint>();
        var keepAwake = new WindowsKeepAwake(flags => { calls.Add(flags); return 1; });

        keepAwake.End();
        keepAwake.Begin();
        keepAwake.End();
        keepAwake.End();

        Assert.That(calls, Is.EqualTo(new[] { 0x80000003u, 0x80000000u }));
    }

    [Test]
    public void Flags_MatchTheWindowsConstants()
    {
        Assert.That(WindowsKeepAwake.AwakeFlags, Is.EqualTo(0x80000003u));
        Assert.That(WindowsKeepAwake.ReleaseFlags, Is.EqualTo(0x80000000u));
    }
}
