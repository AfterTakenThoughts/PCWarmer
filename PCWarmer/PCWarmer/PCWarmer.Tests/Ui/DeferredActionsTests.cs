using System.Collections.Generic;
using NUnit.Framework;
using PCWarmer.Core.Ui;

namespace PCWarmer.Tests.Ui;

[TestFixture]
public class DeferredActionsTests
{
    [Test]
    public void Post_DoesNotRunUntilRunPending()
    {
        var ran = new List<string>();
        var actions = new DeferredActions();

        actions.Post(() => ran.Add("click"));

        Assert.That(ran, Is.Empty, "a click during Draw must not change the screen list straight away");
        actions.RunPending();
        Assert.That(ran, Is.EqualTo(new[] { "click" }));
    }

    [Test]
    public void RunPending_RunsInOrderAndOnlyOnce()
    {
        var ran = new List<int>();
        var actions = new DeferredActions();
        actions.Post(() => ran.Add(1));
        actions.Post(() => ran.Add(2));

        actions.RunPending();
        actions.RunPending();

        Assert.That(ran, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void ActionPostedWhileRunning_WaitsForTheNextRun()
    {
        var ran = new List<string>();
        var actions = new DeferredActions();
        actions.Post(() => { ran.Add("first"); actions.Post(() => ran.Add("second")); });

        actions.RunPending();
        Assert.That(ran, Is.EqualTo(new[] { "first" }));

        actions.RunPending();
        Assert.That(ran, Is.EqualTo(new[] { "first", "second" }));
    }
}
