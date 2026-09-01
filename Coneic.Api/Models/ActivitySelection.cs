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
}
