using ECommerce.AdminPanel.Models;

namespace ECommerce.AdminPanel.Tests.Models;

public class ImageOrderTests
{
    [Theory]
    [InlineData(null, new[] { 1, 2, 3 })]
    [InlineData("1:-1", new[] { 1, 2, 3 })]
    [InlineData("3:1", new[] { 1, 2, 3 })]
    [InlineData("2:1", new[] { 1, 3, 2 })]
    [InlineData("2:-1", new[] { 2, 1, 3 })]
    [InlineData("9:-1", new[] { 1, 2, 3 })]
    [InlineData("abc", new[] { 1, 2, 3 })]
    public void Move_swaps_with_neighbour_or_keeps_order(string? move, int[] expected)
    {
        ImageOrder.Apply([1, 2, 3], move).ShouldBe(expected);
    }
}
