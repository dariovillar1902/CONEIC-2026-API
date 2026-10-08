using Coneic.Api.Data;
using Coneic.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Coneic.Api.Services;

public record AutoAssignResult(
    bool DryRun, int People, int ConfirmedDrafts,
    int Talleres, int Simultaneas, int Solidarias, int MixedFamily, List<string> Failures);

/// <summary>
/// Asigna al azar lo que quedó sin elegir de Taller / Simultánea / Solidaria
/// (Guía de Elección: "si no elegís, el sistema te asigna una de manera
/// automática y aleatoria"). Reglas:
///   - un borrador sin confirmar se confirma tal cual (cuenta como elección);
///   - la Simultánea se sortea entre las de la MISMA Familia que el Taller de
///     la persona; solo si esa familia no tiene más lugar se admite otra
///     familia (queda contado en MixedFamily);
///   - quien está en el Desafío de Barreras no recibe Taller ni Simultánea;
///   - el Comité Organizador no participa del sorteo;
///   - no toca Visitas Técnicas (bloque 1).
/// Llamar con la cola de escrituras (HotCache.WriteGate) tomada.
/// </summary>
public static class AutoAssignRunner
{
    private const int TallerBlockId = 2;
    private const int SimultaneaBlockId = 3;
    private const int SolidariaBlockId = 4;

    public static async Task<AutoAssignResult> RunAsync(ApplicationDbContext db, bool dryRun)
    {
        var blockIds = new[] { TallerBlockId, SimultaneaBlockId, SolidariaBlockId };

        var activities = await db.SelectableActivities.Where(a => blockIds.Contains(a.BlockId)).ToListAsync();
        var allSelections = await db.ActivitySelections.Where(s => blockIds.Contains(s.BlockId)).ToListAsync();
        var taken = activities.ToDictionary(a => a.Id, a => allSelections.Count(s => s.ActivityId == a.Id));
        var byId = activities.ToDictionary(a => a.Id);

        var regs = await db.Registrations
            .Where(r => r.Status == "Paid")
            .Select(r => new { r.Email, r.InterestedInMaccaferri, r.Faculty })
            .ToListAsync();
        var people = regs
            .Where(r => !(r.Faculty ?? "").StartsWith("Comit", StringComparison.OrdinalIgnoreCase))
            .GroupBy(r => r.Email.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();
        var emails = people.Select(p => p.Email.ToLowerInvariant()).ToHashSet();

        var byUser = allSelections
            .Where(s => emails.Contains(s.UserEmail.ToLowerInvariant()))
            .GroupBy(s => s.UserEmail.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        var rng = Random.Shared;
        var now = DateTime.Now;
        var confirmedDrafts = 0;
        var mixed = 0;
        var assigned = new Dictionary<int, int> { [TallerBlockId] = 0, [SimultaneaBlockId] = 0, [SolidariaBlockId] = 0 };
        var failures = new List<string>();
        var toAdd = new List<ActivitySelection>();

        bool HasRoom(SelectableActivity a) => taken[a.Id] < a.Capacity;
        SelectableActivity? Pick(IEnumerable<SelectableActivity> pool)
        {
            var open = pool.Where(HasRoom).ToList();
            return open.Count == 0 ? null : open[rng.Next(open.Count)];
        }
        void Add(string email, int blockId, SelectableActivity a, List<ActivitySelection> mine)
        {
            taken[a.Id]++;
            var sel = new ActivitySelection { UserEmail = email, BlockId = blockId, ActivityId = a.Id, IsConfirmed = true, ConfirmedAt = now };
            toAdd.Add(sel);
            mine.Add(sel);
            assigned[blockId]++;
        }

        foreach (var person in people.OrderBy(_ => rng.Next()))
        {
            var email = person.Email;
            var mine = byUser.TryGetValue(email.ToLowerInvariant(), out var list) ? list : new List<ActivitySelection>();
            ActivitySelection? Existing(int blockId) => mine.FirstOrDefault(s => s.BlockId == blockId);

            foreach (var draft in mine.Where(s => !s.IsConfirmed))
            {
                draft.IsConfirmed = true;
                draft.ConfirmedAt = now;
                confirmedDrafts++;
            }

            if (!person.InterestedInMaccaferri)
            {
                var tallerSel = Existing(TallerBlockId);
                var simSel = Existing(SimultaneaBlockId);
                var taller = tallerSel == null ? null : byId[tallerSel.ActivityId];

                if (taller == null)
                {
                    // Un Taller con lugar cuya Familia todavía tenga alguna Simultánea con lugar.
                    var viable = activities
                        .Where(a => a.BlockId == TallerBlockId && HasRoom(a)
                            && activities.Any(s => s.BlockId == SimultaneaBlockId && s.Family == a.Family && HasRoom(s)))
                        .ToList();
                    if (simSel != null)
                    {
                        var simFamily = byId[simSel.ActivityId].Family;
                        var sameFamily = viable.Where(a => a.Family == simFamily).ToList();
                        if (sameFamily.Count > 0) viable = sameFamily;
                    }
                    taller = Pick(viable) ?? Pick(activities.Where(a => a.BlockId == TallerBlockId));
                    if (taller == null) { failures.Add($"{email}: sin Taller con cupo"); continue; }
                    Add(email, TallerBlockId, taller, mine);
                }

                if (simSel == null)
                {
                    var sim = Pick(activities.Where(a => a.BlockId == SimultaneaBlockId && a.Family == taller.Family));
                    if (sim == null)
                    {
                        sim = Pick(activities.Where(a => a.BlockId == SimultaneaBlockId));
                        if (sim != null) mixed++;
                    }
                    if (sim == null) { failures.Add($"{email}: sin Simultánea con cupo"); continue; }
                    Add(email, SimultaneaBlockId, sim, mine);
                }
            }

            if (Existing(SolidariaBlockId) == null)
            {
                var sol = Pick(activities.Where(a => a.BlockId == SolidariaBlockId));
                if (sol == null) { failures.Add($"{email}: sin Solidaria con cupo"); continue; }
                Add(email, SolidariaBlockId, sol, mine);
            }
        }

        if (!dryRun)
        {
            using var tx = await db.Database.BeginTransactionAsync();
            db.ActivitySelections.AddRange(toAdd);
            foreach (var a in activities) a.TakenCount = taken[a.Id];
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        else
        {
            db.ChangeTracker.Clear();
        }

        return new AutoAssignResult(dryRun, people.Count, confirmedDrafts,
            assigned[TallerBlockId], assigned[SimultaneaBlockId], assigned[SolidariaBlockId], mixed, failures);
    }
}
