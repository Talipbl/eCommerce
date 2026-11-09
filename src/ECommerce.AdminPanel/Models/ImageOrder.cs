namespace ECommerce.AdminPanel.Models;

public static class ImageOrder
{
    /// <summary>
    /// Ekrandaki sıraya isteğe bağlı bir taşıma uygular. <paramref name="move"/> "görselId:-1" (öne) veya "görselId:1" (arkaya) biçimindedir;
    /// geçersizse sıra değişmeden döner.
    /// </summary>
    public static List<int> Apply(IReadOnlyList<int> imageIds, string? move)
    {
        var order = imageIds.Distinct().ToList();
        var parts = move?.Split(':');
        if (parts is not [var idText, var directionText] || !int.TryParse(idText, out var id) || !int.TryParse(directionText, out var direction))
            return order;

        var index = order.IndexOf(id);
        var target = index + Math.Sign(direction);
        if (index < 0 || target < 0 || target >= order.Count)
            return order;

        (order[index], order[target]) = (order[target], order[index]);
        return order;
    }
}
