namespace Coneic.Api.Models;

/// <summary>
/// One occasion on which attendance is taken (e.g. "Acreditación día 1",
/// "Cena de bienvenida"). A registrant can be checked into several
/// sessions, but only once per session.
/// </summary>
public class AttendanceSession
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
