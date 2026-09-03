namespace Coneic.Api.Models;

/// <summary>One selectable option within an ActivityBlock (a workshop, talk, or visit).</summary>
public class SelectableActivity
{
    public int Id { get; set; }
    public int BlockId { get; set; }

    /// <summary>Reference code from the "Guía de Elección de Actividades" (e.g. "4.01").</summary>
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Speaker { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Placeholder until organizers confirm real capacity per activity.</summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Denormalized running count of active selections, updated via a single
    /// atomic UPDATE ... WHERE TakenCount &lt; Capacity (see
    /// ActivitySelectionController). Avoids a separate COUNT query and — more
    /// importantly — avoids the check-then-insert race that a COUNT-based
    /// check would have under concurrent requests.
    /// </summary>
    public int TakenCount { get; set; } = 0;
}
