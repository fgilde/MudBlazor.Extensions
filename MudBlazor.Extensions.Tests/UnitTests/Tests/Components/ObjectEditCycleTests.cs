using MudBlazor.Extensions.Components.ObjectEdit.Options;

namespace MudBlazor.Extensions.Tests.UnitTests.Tests.Components;

/// <summary>
/// A type that contains itself must not send the meta reader into an endless descent.
/// </summary>
/// <remarks>
/// This is what killed the whole wasm runtime with a StackOverflowException when the demo opened the
/// instance editor for MudExFileManager: its nodes carry a Parent of their own type, and the reader walked
/// into every sub object without noticing it had seen that type on the way down already.
/// </remarks>
public class ObjectEditCycleTests
{
    private class Node
    {
        public string Name { get; set; }
        public Node Parent { get; set; }
        public Node Child { get; set; }
    }

    private class Holder
    {
        public Node Root { get; set; }
        public string Title { get; set; }
    }

    [Fact]
    public void ATypeThatContainsItselfIsRead()
    {
        var root = new Node { Name = "root" };
        root.Child = new Node { Name = "child", Parent = root };

        var meta = new ObjectEditMeta<Node>(root);

        Assert.Contains(meta.AllProperties, p => p.PropertyName == nameof(Node.Name));
    }

    [Fact]
    public void TheCycleIsCutButEverythingBeforeItStays()
    {
        var holder = new Holder { Title = "holder", Root = new Node { Name = "root" } };

        var meta = new ObjectEditMeta<Holder>(holder);
        var names = meta.AllProperties.Select(p => p.PropertyName).ToList();

        Assert.Contains(nameof(Holder.Title), names);
        // One level into the node is still read...
        Assert.Contains(names, n => n.EndsWith(nameof(Node.Name), StringComparison.Ordinal));
        // ...and the way back into the same type is not followed again.
        Assert.DoesNotContain(names, n => n.Contains("Parent.Parent", StringComparison.Ordinal));
    }
}
