namespace Coneic.Api.Models;

/// <summary>
/// A group of mutually-exclusive activity options (e.g. "Visita Técnica",
/// or a time slot of simultaneous talks/workshops). A person picks at most
/// <see cref="MaxSelections"/> option(s) from within the block.
/// </summary>
public class ActivityBlock
{
    public int Id { get; set; }

    /// <summary>"VisitaTecnica" | "TallerCharla" — matches SelectableActivity.Category.</summary>
    public string Category { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Free-text disclaimer shown to the user (e.g. "agrupación provisoria").</summary>
    public string? Note { get; set; }

    public int MaxSelections { get; set; } = 1;

    /// <summary>
    /// Inactive blocks are hidden from GetBlocks and excluded from the
    /// "you must pick one per block" check in Confirm — lets a block's
    /// content stay seeded in the DB without being live yet.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
