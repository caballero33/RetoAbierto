using System.Text.Json.Serialization;

namespace RetoAbierto.Application.Common.Models;

public class PagedResult<T>
{
    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; set; } = new List<T>();

    [JsonPropertyName("pagina")]
    public int Pagina { get; set; } = 1;

    [JsonPropertyName("tamano")]
    public int Tamano { get; set; } = 20;

    [JsonPropertyName("total")]
    public int Total { get; set; } = 0;

    public PagedResult() { }

    public PagedResult(IEnumerable<T> items, int total, int pagina, int tamano)
    {
        Items = items;
        Total = total;
        Pagina = pagina;
        Tamano = tamano;
    }
}

