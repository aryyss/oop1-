using SpreadsheetApp.Core.Model;
using Xunit;

namespace SpreadsheetApp.Tests;

public class DependencyGraphTests
{
    private static CellAddress A(string text) => CellAddress.Parse(text);

    private static string Names(IEnumerable<CellAddress> addresses) =>
        string.Join(",", addresses.Select(a => a.ToString()));

    [Fact]
    public void GetReferences_ReturnsWhatWasSet()
    {
        var graph = new DependencyGraph();
        graph.Update(A("C1"), new[] { A("A1"), A("B1") });
        Assert.Equal("A1,B1", Names(graph.GetReferences(A("C1")).OrderBy(a => a.Column)));
    }

    [Fact]
    public void Update_ReplacesOldReferences()
    {
        var graph = new DependencyGraph();
        graph.Update(A("C1"), new[] { A("A1") });
        graph.Update(A("C1"), new[] { A("B1") });

        Assert.Equal("B1", Names(graph.GetReferences(A("C1"))));
        Assert.Equal("A1", Names(graph.GetRecalcOrder(new[] { A("A1") })));
        Assert.Equal("B1,C1", Names(graph.GetRecalcOrder(new[] { A("B1") })));
    }

    [Fact]
    public void Update_EmptyReferences_RemovesCell()
    {
        var graph = new DependencyGraph();
        graph.Update(A("B1"), new[] { A("A1") });
        graph.Update(A("B1"), Array.Empty<CellAddress>());

        Assert.Empty(graph.GetReferences(A("B1")));
        Assert.Equal("A1", Names(graph.GetRecalcOrder(new[] { A("A1") })));
    }

    [Fact]
    public void HasCycle_SelfReference()
    {
        var graph = new DependencyGraph();
        graph.Update(A("A1"), new[] { A("A1") });
        Assert.True(graph.HasCycle(A("A1")));
    }

    [Fact]
    public void HasCycle_LongCycle()
    {
        var graph = new DependencyGraph();
        graph.Update(A("A1"), new[] { A("B1") });
        graph.Update(A("B1"), new[] { A("C1") });
        graph.Update(A("C1"), new[] { A("A1") });
        Assert.True(graph.HasCycle(A("A1")));
        Assert.True(graph.HasCycle(A("B1")));
    }

    [Fact]
    public void HasCycle_DiamondIsNotCycle()
    {
        var graph = new DependencyGraph();
        graph.Update(A("B1"), new[] { A("A1") });
        graph.Update(A("C1"), new[] { A("A1") });
        graph.Update(A("D1"), new[] { A("B1"), A("C1") });
        Assert.False(graph.HasCycle(A("D1")));
        Assert.False(graph.HasCycle(A("A1")));
    }

    [Fact]
    public void GetRecalcOrder_DependenciesComeFirst()
    {
        var graph = new DependencyGraph();
        graph.Update(A("B1"), new[] { A("A1") });
        graph.Update(A("C1"), new[] { A("B1") });
        graph.Update(A("D1"), new[] { A("B1"), A("C1") });

        var order = graph.GetRecalcOrder(new[] { A("A1") }).ToList();

        Assert.Equal(4, order.Count);
        Assert.Equal(A("A1"), order[0]);
        Assert.Equal(A("D1"), order[3]);
        Assert.True(order.IndexOf(A("B1")) < order.IndexOf(A("C1")));
    }

    [Fact]
    public void GetRecalcOrder_MultipleRoots_NoDuplicates()
    {
        var graph = new DependencyGraph();
        graph.Update(A("C1"), new[] { A("A1"), A("B1") });

        var order = graph.GetRecalcOrder(new[] { A("A1"), A("B1") }).ToList();

        Assert.Equal(3, order.Count);
        Assert.Equal(A("C1"), order[2]);
    }

    [Fact]
    public void GetRecalcOrder_LongChain_DoesNotOverflowStack()
    {
        var graph = new DependencyGraph();
        for (var row = 1; row < 50000; row++)
        {
            graph.Update(new CellAddress(0, row), new[] { new CellAddress(0, row - 1) });
        }

        var order = graph.GetRecalcOrder(new[] { new CellAddress(0, 0) });

        Assert.Equal(50000, order.Count);
        Assert.Equal(new CellAddress(0, 0), order[0]);
        Assert.Equal(new CellAddress(0, 49999), order[^1]);
    }
}
