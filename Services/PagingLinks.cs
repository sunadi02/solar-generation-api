using SolarGenerationApi.Dtos;

namespace SolarGenerationApi.Services;

public static class PagingLinks
{
    public static PageLinks Build(HttpRequest request, int page, int pageSize, int total)
    {
        int last = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));

        string Link(int p)
        {
            var parts = request.Query
                .Where(kv => kv.Key != "page")
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value.ToString())}")
                .ToList();
            parts.Add($"page={p}");
            return $"{request.Path}?{string.Join("&", parts)}";
        }

        return new PageLinks(
            Link(page),
            Link(1),
            page > 1 ? Link(page - 1) : null,
            page < last ? Link(page + 1) : null,
            Link(last));
    }
}