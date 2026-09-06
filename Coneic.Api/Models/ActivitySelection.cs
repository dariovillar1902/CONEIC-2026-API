namespace Coneic.Api.Models;

/// <summary>
/// One confirmed pick: a person's chosen option within one ActivityBlock.
/// Unique per (UserEmail, BlockId) — choosing a new option in the same
/// block replaces the previous one.
/// </summary>
public class ActivitySelection
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int BlockId { get; set; }
    public int ActivityId { get; set; }
    public DateTime SelectedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Draft (false) selections can still be changed or deleted. Once the
    /// user hits "Guardar Selección Definitiva", ALL of their selections
    /// (across every block) flip to true in one go and become read-only.
    /// </summary>
    public bool IsConfirmed { get; set; } = false;
    public DateTime? ConfirmedAt { get; set; }
}
