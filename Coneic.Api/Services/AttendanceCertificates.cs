using ClosedXML.Excel;
using Coneic.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Coneic.Api.Services;

public sealed record CertificatePerson(
    int Id, string Lastname, string Name, string Dni, string Faculty, string Email,
    int Expected, int Attended, double Percent, bool Eligible,
    Dictionary<string, string> Cells);

public sealed record CertificateSession(string Name, int Records, bool Loaded);

public sealed record CertificateReport(
    bool OnlyLoaded, List<string> Columns, List<CertificateSession> Sessions, List<CertificatePerson> People);

/// <summary>
/// Cruza la asistencia con lo que cada persona debía asistir y marca quién llega al 75 %.
///   Martes 13: Acreditación, Salida a almuerzo, Vuelta de almuerzo, Fin de jornada.
///   Miércoles 14: una toma por cada actividad elegida (Taller, Simultánea, Solidaria).
///   Jueves 15: Visita técnica (solo si tiene una visita confirmada).
///   Viernes 16: Inicio de mañana, Salida a almuerzo, Vuelta de almuerzo.
/// Certificado = asistió a, al menos, el 75 % de las tomas que le correspondían.
/// </summary>
public static class AttendanceCertificates
{
    public const int ThresholdPercent = 75;

    public static readonly string[] FixedSessions =
    {
        "Martes 13 · Acreditación", "Martes 13 · Salida a almuerzo", "Martes 13 · Vuelta de almuerzo", "Martes 13 · Fin de jornada",
        "Jueves 15 · Visita técnica",
        "Viernes 16 · Inicio de mañana", "Viernes 16 · Salida a almuerzo", "Viernes 16 · Vuelta de almuerzo",
    };

    private const string VisitSession = "Jueves 15 · Visita técnica";
    private static readonly string[] WednesdayTypes = { "Taller", "Simultánea", "Solidaria" };
    private static readonly Regex ActivitySession = new(@"^(Taller|Simultánea|Solidaria) (\d\.\d{2}) ", RegexOptions.Compiled);

    public static async Task<CertificateReport> BuildAsync(ApplicationDbContext db, bool onlyLoaded)
    {
        var sessions = await db.AttendanceSessions.AsNoTracking().Select(s => new { s.Id, s.Name }).ToListAsync();
        var records = await db.AttendanceRecords.AsNoTracking().Select(r => new { r.RegistrationId, r.SessionId }).ToListAsync();
        var attended = records.GroupBy(r => r.RegistrationId).ToDictionary(g => g.Key, g => g.Select(x => x.SessionId).ToHashSet());
        var recordCount = records.GroupBy(r => r.SessionId).ToDictionary(g => g.Key, g => g.Count());

        var fixedIds = FixedSessions.ToDictionary(n => n, n => sessions.FirstOrDefault(s => s.Name == n)?.Id);
        var activityIds = new Dictionary<string, List<int>>();   // código -> ids de la toma (por si hubiera repetidas)
        foreach (var s in sessions)
        {
            var m = ActivitySession.Match(s.Name);
            if (m.Success) { if (!activityIds.TryGetValue(m.Groups[2].Value, out var l)) activityIds[m.Groups[2].Value] = l = new(); l.Add(s.Id); }
        }

        var codeById = await db.SelectableActivities.AsNoTracking()
            .Where(a => a.BlockId >= 2 && a.BlockId <= 4).ToDictionaryAsync(a => a.Id, a => (a.Code, a.BlockId));
        var picks = (await db.ActivitySelections.AsNoTracking().Where(s => s.BlockId >= 2 && s.BlockId <= 4)
                .Select(s => new { s.UserEmail, s.ActivityId }).ToListAsync())
            .Where(p => codeById.ContainsKey(p.ActivityId))
            .GroupBy(p => p.UserEmail.ToLower())
            .ToDictionary(g => g.Key, g => g.Select(p => codeById[p.ActivityId]).ToList());
        var withVisit = (await db.ActivitySelections.AsNoTracking().Where(s => s.BlockId == 1 && s.IsConfirmed)
            .Select(s => s.UserEmail).ToListAsync()).Select(e => e.ToLower()).ToHashSet();

        bool Counts(int? id) => id != null && (!onlyLoaded || recordCount.ContainsKey(id.Value));

        var people = new List<CertificatePerson>();
        var registrations = await db.Registrations.AsNoTracking().Where(r => r.Status == "Paid")
            .Select(r => new { r.Id, r.Lastname, r.Name, r.Dni, r.Faculty, r.Email })
            .OrderBy(r => r.Lastname).ThenBy(r => r.Name).ToListAsync();

        foreach (var r in registrations)
        {
            var mail = r.Email.ToLower();
            var mine = attended.GetValueOrDefault(r.Id) ?? new HashSet<int>();
            var cells = new Dictionary<string, string>();
            int expected = 0, present = 0;

            foreach (var name in FixedSessions)
            {
                var id = fixedIds[name];
                if (name == VisitSession && !withVisit.Contains(mail)) { cells[name] = "—"; continue; }
                if (!Counts(id)) { cells[name] = id == null ? "sin toma" : "sin cargar"; continue; }
                expected++;
                var ok = mine.Contains(id!.Value);
                if (ok) present++;
                cells[name] = ok ? "✓" : "✗";
            }

            var chosen = picks.GetValueOrDefault(mail) ?? new();
            foreach (var type in WednesdayTypes)
            {
                var block = type == "Taller" ? 2 : type == "Simultánea" ? 3 : 4;
                var pick = chosen.FirstOrDefault(c => c.BlockId == block);
                if (pick.Code == null) { cells[type] = "—"; continue; }
                var ids = activityIds.GetValueOrDefault(pick.Code) ?? new List<int>();
                var anyLoaded = ids.Any(i => recordCount.ContainsKey(i));
                if (ids.Count == 0 || (onlyLoaded && !anyLoaded)) { cells[type] = $"{pick.Code} sin cargar"; continue; }
                expected++;
                var ok = ids.Any(mine.Contains);
                if (ok) present++;
                cells[type] = $"{pick.Code} {(ok ? "✓" : "✗")}";
            }

            var percent = expected == 0 ? 0 : Math.Round(100.0 * present / expected, 1);
            people.Add(new CertificatePerson(r.Id, r.Lastname, r.Name, r.Dni, r.Faculty, r.Email,
                expected, present, percent, expected > 0 && present * 100 >= expected * ThresholdPercent, cells));
        }

        var columns = FixedSessions.Concat(WednesdayTypes).ToList();
        var summary = sessions.Where(s => FixedSessions.Contains(s.Name) || ActivitySession.IsMatch(s.Name))
            .Select(s => new CertificateSession(s.Name, recordCount.GetValueOrDefault(s.Id), recordCount.ContainsKey(s.Id)))
            .OrderBy(s => s.Name).ToList();
        foreach (var missing in FixedSessions.Where(n => sessions.All(s => s.Name != n)))
            summary.Add(new CertificateSession(missing + " (no creada)", 0, false));
        return new CertificateReport(onlyLoaded, columns, summary, people);
    }

    public static byte[] ToExcel(CertificateReport report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Certificados");
        var header = new List<string> { "ID inscripción", "Apellido", "Nombre", "DNI", "Delegación", "Email" };
        header.AddRange(report.Columns);
        header.AddRange(new[] { "Tomas esperadas", "Tomas asistidas", "% asistencia", $"Certificado (≥{ThresholdPercent}%)" });
        for (var i = 0; i < header.Count; i++) ws.Cell(1, i + 1).Value = header[i];

        var row = 2;
        foreach (var p in report.People)
        {
            var c = 1;
            ws.Cell(row, c++).Value = p.Id; ws.Cell(row, c++).Value = p.Lastname; ws.Cell(row, c++).Value = p.Name;
            ws.Cell(row, c++).Value = p.Dni; ws.Cell(row, c++).Value = p.Faculty; ws.Cell(row, c++).Value = p.Email;
            foreach (var col in report.Columns) ws.Cell(row, c++).Value = p.Cells.GetValueOrDefault(col, "");
            ws.Cell(row, c++).Value = p.Expected; ws.Cell(row, c++).Value = p.Attended;
            ws.Cell(row, c++).Value = p.Percent / 100.0; ws.Cell(row, c - 1).Style.NumberFormat.Format = "0.0%";
            ws.Cell(row, c).Value = p.Eligible ? "Sí" : "No";
            if (p.Eligible) ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#D1FAE5");
            row++;
        }
        var head = ws.Range(1, 1, 1, header.Count);
        head.Style.Font.Bold = true; head.Style.Font.FontColor = XLColor.White; head.Style.Fill.BackgroundColor = XLColor.FromHtml("#002F51");
        ws.SheetView.FreezeRows(1);
        ws.Range(1, 1, Math.Max(row - 1, 1), header.Count).SetAutoFilter();
        ws.Columns().AdjustToContents();

        var ts = wb.AddWorksheet("Tomas");
        ts.Cell(1, 1).Value = "Toma"; ts.Cell(1, 2).Value = "Asistentes cargados"; ts.Cell(1, 3).Value = "¿Cargada?";
        var tr = 2;
        foreach (var s in report.Sessions) { ts.Cell(tr, 1).Value = s.Name; ts.Cell(tr, 2).Value = s.Records; ts.Cell(tr, 3).Value = s.Loaded ? "Sí" : "No"; tr++; }
        ts.Row(1).Style.Font.Bold = true; ts.Columns().AdjustToContents();

        var rule = wb.AddWorksheet("Criterio");
        var notes = new[]
        {
            $"Certificado: asistió a, al menos, el {ThresholdPercent}% de las tomas de asistencia que le correspondían.",
            "Tomas por persona: Martes 13 (4), Miércoles 14 (una por su Taller, Simultánea y Solidaria), Jueves 15 (visita técnica, solo si tiene una confirmada) y Viernes 16 (3).",
            report.OnlyLoaded
                ? "Este archivo cuenta SOLO las tomas que ya tienen asistencia cargada; las demás figuran como 'sin cargar'."
                : "Este archivo cuenta TODAS las tomas esperadas; una toma sin asistencia cargada cuenta como ausencia para todos.",
            "Solo se incluyen inscriptos con pago confirmado.",
            "—: la persona no tenía esa actividad. ✓ presente, ✗ ausente.",
        };
        for (var i = 0; i < notes.Length; i++) rule.Cell(i + 1, 1).Value = notes[i];
        rule.Column(1).Width = 140;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
