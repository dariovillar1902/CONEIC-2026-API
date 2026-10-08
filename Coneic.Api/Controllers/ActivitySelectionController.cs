using Coneic.Api.Data;
using Coneic.Api.Models;
using Coneic.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Coneic.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActivitySelectionController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<ActivitySelectionController> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    // Única cuenta habilitada para reasignar manualmente la visita de otra
    // persona (excepción del directorio, fuera del flujo normal). No se
    // deriva del rol 'admin' genérico a propósito: es un caso puntual, no un
    // permiso de todos los admins.
    private const string DirectorioOverrideEmail = "directorio@coneic2026.com.ar";

    // IDs fijos de los bloques de "Actividades Académicas parte 2" (ver Guía
    // de Elección v1): el Taller elegido determina la Familia, que a su vez
    // restringe qué Charla Simultánea se puede elegir. Solidaria es libre.
    private const int TallerBlockId = 2;
    private const int SimultaneaBlockId = 3;

    // Ventana de elección de Taller / Simultánea / Solidaria (Guía de Elección
    // de Actividades Académicas v1): abre el miércoles 07/10 a las 23:00 y
    // cierra el jueves 08/10 a las 23:00 (hora Argentina). Antes de abrir y
    // después de cerrar, Select/Unselect/Confirm quedan bloqueados (los admins
    // pasan igual). Lo que quede sin elegir se asigna con auto-assign.
    // Expresado directamente en UTC
    // (ART es UTC-3 fijo, sin horario de verano) para no depender de
    // TimeZoneInfo.FindSystemTimeZoneById, que puede fallar si el contenedor
    // no tiene tzdata instalada (ya pasó: tumbaba GetBlocks con un 500).
    private static readonly DateTime SelectionWindowOpensAt = new(2026, 10, 8, 2, 0, 0, DateTimeKind.Utc);        // mié 07/10 23:00 ART (Guía de Elección v1)
    private static readonly DateTime SelectionWindowClosesAt = new(2026, 10, 9, 2, 0, 0, DateTimeKind.Utc);       // jue 08/10 23:00 ART (Guía de Elección v1)

    // Hasta las 22:57 ART del 07/10 las cuentas admin pueden elegir a modo de
    // prueba (pedido del equipo, 2026-10-07); a partir de ahí solo eligen los
    // asistentes, dentro de la ventana. Expresado en UTC (01:57 UTC del 08/10).
    private static readonly DateTime AdminSelectionCutoff = new(2026, 10, 8, 1, 57, 0, DateTimeKind.Utc);

    private async Task<bool> CanSelectNowAsync(string email) =>
        await IsAdminEmailAsync(email)
            ? DateTime.UtcNow < AdminSelectionCutoff
            : IsSelectionWindowOpen();

    private static bool IsSelectionWindowOpen()
    {
        var nowUtc = DateTime.UtcNow;
        return nowUtc >= SelectionWindowOpensAt && nowUtc <= SelectionWindowClosesAt;
    }

    // Pedido del equipo (WhatsApp, 2026-09-27): habilitar la elección HOY
    // para perfiles admin (para poder probarla antes de la apertura real),
    // sin tocar la ventana real que rige para el resto. Por rol, no por
    // lista de emails — cualquier cuenta admin actual o futura queda
    // cubierta automáticamente.
    // ── Índice en memoria (una sola instancia de la app) ────────────────────
    //
    // Durante la elección cientos de personas piden /blocks y /status a la vez
    // mientras otras tantas escriben. Con SQLite sobre el disco compartido
    // (CIFS) cada lectura competía con las escrituras por el lock del archivo
    // y las respuestas se iban a decenas de segundos. Ahora esas lecturas salen
    // de memoria: el catálogo (cupos) se refresca en segundo plano cada 1,5 s y
    // las elecciones de cada persona (más admin / Barreras) cada 30 s, y tras
    // cada escritura propia se actualizan al instante. Nunca se bloquea un
    // pedido esperando a la base: si el dato está viejo se sirve igual y se
    // refresca aparte (dentro de la misma cola que las escrituras, para que la
    // lectura no se muera de hambre).
    private sealed record UserSnap(List<ActivitySelection> Selections, bool IsMaccaferri, bool IsAdmin)
    {
        public static readonly UserSnap Empty = new(new List<ActivitySelection>(), false, false);
    }

    private sealed record CatalogSnap(List<ActivityBlock> Blocks, List<SelectableActivity> Activities);

    private static ConcurrentDictionary<string, UserSnap> _users = new();
    private static volatile CatalogSnap? _catalog;
    private static volatile bool _loaded;
    private static long _lastCatalogTicks;
    private static long _lastUsersTicks;
    private static int _refreshingCatalog;
    private static int _refreshingUsers;
    private static readonly SemaphoreSlim FirstLoadLock = new(1, 1);

    private static void InvalidateAll()
    {
        Interlocked.Exchange(ref _lastCatalogTicks, 0);
        Interlocked.Exchange(ref _lastUsersTicks, 0);
    }

    private static async Task RefreshCatalogAsync(IServiceScopeFactory sf)
    {
        await HotCache.WriteGate.WaitAsync();
        try
        {
            using var scope = sf.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var blocks = await db.ActivityBlocks.AsNoTracking().OrderBy(b => b.Id).ToListAsync();
            var acts = await db.SelectableActivities.AsNoTracking().OrderBy(a => a.Code).ToListAsync();
            _catalog = new CatalogSnap(blocks, acts);
            Interlocked.Exchange(ref _lastCatalogTicks, Environment.TickCount64);
        }
        finally { HotCache.WriteGate.Release(); }
    }

    private static async Task RefreshUsersAsync(IServiceScopeFactory sf)
    {
        // Se hace dentro de la cola de escrituras: nadie escribe mientras se lee.
        await HotCache.WriteGate.WaitAsync();
        try
        {
            using var scope = sf.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var sels = await db.ActivitySelections.AsNoTracking().ToListAsync();
            var regs = await db.Registrations.AsNoTracking().ToListAsync();
            var users = await db.Users.AsNoTracking().ToListAsync();

            var selByEmail = sels.GroupBy(x => x.UserEmail.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.ToList());
            var macc = regs.Where(r => r.InterestedInMaccaferri).Select(r => r.Email.ToLowerInvariant()).ToHashSet();
            var admins = users.Where(u => u.Role == "admin").Select(u => u.Email.ToLowerInvariant()).ToHashSet();

            var dict = new ConcurrentDictionary<string, UserSnap>();
            foreach (var key in selByEmail.Keys.Union(macc).Union(admins))
                dict[key] = new UserSnap(selByEmail.TryGetValue(key, out var l) ? l : new List<ActivitySelection>(), macc.Contains(key), admins.Contains(key));
            _users = dict;

            HotCache.Users = new ConcurrentDictionary<string, User>(
                users.GroupBy(u => u.Email.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First()));
            HotCache.Registrations = new ConcurrentDictionary<string, Registration>(
                regs.GroupBy(r => r.Email.ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First()));
            HotCache.RegistrationsLoaded = true;
            Interlocked.Exchange(ref _lastUsersTicks, Environment.TickCount64);
        }
        finally { HotCache.WriteGate.Release(); }
    }

    private async Task EnsureCachesAsync()
    {
        var sf = _scopeFactory;
        if (!_loaded)
        {
            await FirstLoadLock.WaitAsync();
            try
            {
                if (!_loaded)
                {
                    await RefreshCatalogAsync(sf);
                    await RefreshUsersAsync(sf);
                    _loaded = true;
                }
            }
            finally { FirstLoadLock.Release(); }
            return;
        }

        var now = Environment.TickCount64;
        if (now - Interlocked.Read(ref _lastCatalogTicks) > 1500 && Interlocked.CompareExchange(ref _refreshingCatalog, 1, 0) == 0)
        {
            _ = Task.Run(async () =>
            {
                try { await RefreshCatalogAsync(sf); }
                catch { /* se sigue sirviendo el catálogo anterior */ }
                finally { Volatile.Write(ref _refreshingCatalog, 0); }
            });
        }
        if (now - Interlocked.Read(ref _lastUsersTicks) > 30000 && Interlocked.CompareExchange(ref _refreshingUsers, 1, 0) == 0)
        {
            _ = Task.Run(async () =>
            {
                try { await RefreshUsersAsync(sf); }
                catch { /* se sigue sirviendo el índice anterior */ }
                finally { Volatile.Write(ref _refreshingUsers, 0); }
            });
        }
    }

    private async Task<UserSnap> GetUserSnapAsync(string email)
    {
        await EnsureCachesAsync();
        return _users.TryGetValue(email.ToLowerInvariant(), out var s) ? s : UserSnap.Empty;
    }

    private async Task<CatalogSnap> GetCatalogAsync(bool includeInactive)
    {
        await EnsureCachesAsync();
        var snap = _catalog!;
        return includeInactive ? snap : new CatalogSnap(snap.Blocks.Where(b => b.IsActive).ToList(), snap.Activities);
    }

    // Tras una escritura propia se recargan las elecciones de esa persona.
    // Llamar SOLO con la cola de escrituras tomada.
    private async Task ReloadUserAsync(string email)
    {
        var lower = email.ToLowerInvariant();
        var sel = await _db.ActivitySelections.AsNoTracking()
            .Where(s => s.UserEmail.ToLower() == lower).ToListAsync();
        var old = _users.TryGetValue(lower, out var o) ? o : UserSnap.Empty;
        _users[lower] = new UserSnap(sel, old.IsMaccaferri, old.IsAdmin);
    }

    private async Task ReloadUserWithGateAsync(string email)
    {
        await HotCache.WriteGate.WaitAsync();
        try { await ReloadUserAsync(email); }
        finally { HotCache.WriteGate.Release(); }
    }

    private async Task<bool> IsAdminEmailAsync(string email) => (await GetUserSnapAsync(email)).IsAdmin;

    // Recorte temporal ("por ahora") mientras se prueba la feature con el
    // equipo: solo estas cuentas admin reciben el mail de confirmación, y a
    // una casilla personal real (son cuentas institucionales compartidas).
    // Sacar este mapeo cuando se habilite para todo el mundo.
    private static readonly Dictionary<string, (string Name, string Email)[]> PilotRecipients = new()
    {
        ["web@coneic2026.com.ar"] = new[] { ("Darío", "dario_villar2001@hotmail.com") },
        ["prensa@coneic2026.com.ar"] = new[] { ("Carol", "carollombardino97@gmail.com") },
        ["directorio@coneic2026.com.ar"] = new[]
        {
            ("Sofi", "spizzamus@frba.utn.edu.ar"),
            ("Cande", "candepoggi@frba.utn.edu.ar"),
        },
        ["comision-directiva@coneic2026.com.ar"] = new[] { ("Darío", "dvillar@frba.utn.edu.ar") },
    };

    // Actualizado 2026-09-09: el PDF viejo en blob storage seguía teniendo
    // AESA (visita que se sacó del listado). Ahora apunta directo al Drive
    // con la propuesta definitiva vigente.
    private const string EppPdfUrl =
        "https://drive.google.com/file/d/1YXycLwBieByabfsSTqBqJVfU3ifJ13-F/view?usp=drive_link";

    public ActivitySelectionController(
        ApplicationDbContext db, IEmailService email, ILogger<ActivitySelectionController> logger, IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _email = email;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    // ── Listado de bloques + opciones + cupos + tu elección actual (draft o confirmada) ──

    [HttpGet("blocks")]
    public async Task<IActionResult> GetBlocks([FromQuery] string email, [FromQuery] bool includeInactive = false)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Falta el email." });

        var user = await GetUserSnapAsync(email);
        var catalog = await GetCatalogAsync(includeInactive);
        var mySelections = user.Selections;

        // Desafío de Barreras (Maccaferri): no eligen Taller ni Simultánea,
        // esa actividad ya los cubre — el frontend usa esto para bloquear
        // esas dos pestañas con un mensaje claro en vez de dejar elegir y
        // fallar recién al confirmar.
        var isMaccaferri = user.IsMaccaferri;

        var result = catalog.Blocks.Select(b => new
        {
            b.Id,
            b.Category,
            b.Name,
            b.Note,
            b.MaxSelections,
            YourSelectionActivityId = mySelections.FirstOrDefault(s => s.BlockId == b.Id)?.ActivityId,
            Options = catalog.Activities.Where(a => a.BlockId == b.Id).Select(a => new
            {
                a.Id,
                a.Code,
                a.Title,
                a.Speaker,
                a.Description,
                a.ImageUrl,
                a.Capacity,
                a.Family,
                a.Venue,
                a.MeetingPoint,
                a.StartTime,
                a.EndTime,
                Taken = a.TakenCount,
            }),
        });

        return Ok(new
        {
            WindowOpensAt = SelectionWindowOpensAt,
            WindowClosesAt = SelectionWindowClosesAt,
            IsWindowOpen = IsSelectionWindowOpen(),
            CanSelect = user.IsAdmin ? DateTime.UtcNow < AdminSelectionCutoff : IsSelectionWindowOpen(),
            IsMaccaferri = isMaccaferri,
            Blocks = result,
        });
    }

    // ── Tu estado general: ¿ya confirmaste definitivamente? ──────────────────

    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Falta el email." });

        var user = await GetUserSnapAsync(email);
        var catalog = await GetCatalogAsync(true);
        var byId = catalog.Activities.ToDictionary(a => a.Id);

        var mine = user.Selections
            .Where(s => byId.ContainsKey(s.ActivityId))
            .Select(s => new
            {
                s.BlockId,
                s.IsConfirmed,
                s.ConfirmedAt,
                ActivityId = s.ActivityId,
                ActivityCode = byId[s.ActivityId].Code,
                ActivityTitle = byId[s.ActivityId].Title,
            }).ToList();

        return Ok(new
        {
            isConfirmed = mine.Any(m => m.IsConfirmed),
            confirmedAt = mine.Where(m => m.ConfirmedAt.HasValue).Select(m => m.ConfirmedAt).FirstOrDefault(),
            selections = mine,
        });
    }

    // ── Elegir / cambiar de opción dentro de un bloque (draft, reversible) ──────

    public record SelectRequest(string Email, int ActivityId);

    [HttpPost("select")]
    public async Task<IActionResult> Select([FromBody] SelectRequest req)
    {
        await EnsureCachesAsync();   // antes de tomar la cola: la carga inicial también la usa
        await HotCache.WriteGate.WaitAsync();
        try { return await SelectCore(req); }
        finally { HotCache.WriteGate.Release(); }
    }

    private async Task<IActionResult> SelectCore(SelectRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Falta el email." });

        var activity = await _db.SelectableActivities.FindAsync(req.ActivityId);
        if (activity == null)
            return NotFound(new { message = "La actividad indicada no existe." });

        if (activity.BlockId != 1 && !await CanSelectNowAsync(req.Email))
            return BadRequest(new { message = "La elección de actividades académicas no está habilitada en este momento." });

        // Quien participa del Desafío de Barreras (Maccaferri) no elige Taller
        // ni Charla Simultánea — esas actividades quedan cubiertas por el
        // propio desafío (Guía de Elección v1). Sí les corresponde elegir su
        // Actividad de Compromiso Social y Medio Ambiente con normalidad.
        if (activity.BlockId == TallerBlockId || activity.BlockId == SimultaneaBlockId)
        {
            var isMaccaferri = await _db.Registrations
                .Where(r => r.Email.ToLower() == req.Email.ToLower())
                .Select(r => r.InterestedInMaccaferri)
                .FirstOrDefaultAsync();
            if (isMaccaferri)
                return BadRequest(new { message = "Estás anotado/a en el Desafío de Barreras (Maccaferri) — esa actividad ya cubre el Taller y la Charla Simultánea, así que no podés elegir acá. Sí tenés que elegir tu Actividad de Compromiso Social." });
        }

        // La confirmación es individual por bloque (no todo-o-nada): un
        // Taller ya confirmado no se puede tocar, pero eso no bloquea elegir
        // o cambiar Simultánea/Solidaria si esos bloques siguen en borrador.
        var blockAlreadyConfirmed = await _db.ActivitySelections
            .AnyAsync(s => s.UserEmail.ToLower() == req.Email.ToLower() && s.BlockId == activity.BlockId && s.IsConfirmed);
        if (blockAlreadyConfirmed)
            return BadRequest(new { message = "Ya confirmaste definitivamente esta categoría — no se puede modificar." });

        var existing = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == req.Email.ToLower() && s.BlockId == activity.BlockId);

        if (existing != null && existing.ActivityId == activity.Id)
            return Ok(new { message = "Ya tenías esta actividad seleccionada.", activityId = activity.Id, blockId = activity.BlockId });

        // Charla Simultánea: solo se puede elegir una que pertenezca a la
        // misma Familia que el Taller ya elegido (Guía de Elección v1).
        ActivitySelection? tallerSelection = null;
        if (activity.BlockId == SimultaneaBlockId)
        {
            tallerSelection = await _db.ActivitySelections
                .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == req.Email.ToLower() && s.BlockId == TallerBlockId);
            if (tallerSelection == null)
                return BadRequest(new { message = "Primero tenés que elegir un Taller." });

            var taller = await _db.SelectableActivities.FindAsync(tallerSelection.ActivityId);
            if (taller?.Family == null || taller.Family != activity.Family)
                return BadRequest(new { message = "Esta charla simultánea no pertenece a la familia de tu taller elegido." });
        }

        using var tx = await _db.Database.BeginTransactionAsync();

        // Reserva atómica: un único UPDATE que solo avanza el contador si
        // todavía hay cupo. Bajo carga concurrente, SQLite serializa estas
        // transacciones — nunca dos personas "ganan" el mismo último cupo.
        var reserved = await _db.SelectableActivities
            .Where(a => a.Id == activity.Id && a.TakenCount < a.Capacity)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount + 1));

        if (reserved == 0)
        {
            await tx.RollbackAsync();
            return Conflict(new { message = "Se acaba de completar el cupo de esta actividad. Elegí otra opción." });
        }

        if (existing != null)
        {
            await _db.SelectableActivities
                .Where(a => a.Id == existing.ActivityId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount - 1));
            _db.ActivitySelections.Remove(existing);
            await _db.SaveChangesAsync();
        }

        // Taller: si ya había una Charla Simultánea elegida de otra Familia,
        // queda inválida — se libera su cupo y se borra, para forzar a
        // re-elegir dentro de la nueva Familia. Si esa Simultánea ya estaba
        // confirmada definitivamente, no se toca — se corta la operación
        // antes (no debería llegar acá: Simultánea confirmada implica que
        // Confirm ya validó el acople de familias, así que el Taller nunca
        // debería poder cambiar a esta altura salvo un caso raro que
        // preferimos rechazar explícitamente antes que corromper un dato ya
        // confirmado).
        if (activity.BlockId == TallerBlockId)
        {
            var staleSimultanea = await (
                from s in _db.ActivitySelections
                join a in _db.SelectableActivities on s.ActivityId equals a.Id
                where s.UserEmail.ToLower() == req.Email.ToLower()
                    && s.BlockId == SimultaneaBlockId
                    && a.Family != activity.Family
                select s
            ).FirstOrDefaultAsync();

            if (staleSimultanea != null)
            {
                if (staleSimultanea.IsConfirmed)
                {
                    // El rollback deshace también la reserva de cupo que
                    // acabamos de hacer sobre `activity` más arriba, en la
                    // misma transacción — no hace falta revertirla a mano.
                    await tx.RollbackAsync();
                    return BadRequest(new { message = "Ya confirmaste tu charla simultánea — no se puede cambiar a un taller de otra familia." });
                }

                await _db.SelectableActivities
                    .Where(a => a.Id == staleSimultanea.ActivityId)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount - 1));
                _db.ActivitySelections.Remove(staleSimultanea);
                await _db.SaveChangesAsync();
            }
        }

        _db.ActivitySelections.Add(new ActivitySelection
        {
            UserEmail = req.Email,
            BlockId = activity.BlockId,
            ActivityId = activity.Id,
        });
        await _db.SaveChangesAsync();

        await tx.CommitAsync();
        await ReloadUserAsync(req.Email);

        return Ok(new { message = "Selección guardada.", activityId = activity.Id, blockId = activity.BlockId });
    }

    // ── Quitar tu elección (draft) en un bloque ──────────────────────────────

    [HttpDelete("select")]
    public async Task<IActionResult> Unselect([FromQuery] string email, [FromQuery] int blockId)
    {
        await EnsureCachesAsync();   // antes de tomar la cola: la carga inicial también la usa
        await HotCache.WriteGate.WaitAsync();
        try { return await UnselectCore(email, blockId); }
        finally { HotCache.WriteGate.Release(); }
    }

    private async Task<IActionResult> UnselectCore(string email, int blockId)
    {
        var existing = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == email.ToLower() && s.BlockId == blockId);

        if (existing == null) return NoContent();
        if (existing.IsConfirmed)
            return BadRequest(new { message = "Ya confirmaste tu selección definitiva — no se puede modificar." });
        if (blockId != 1 && !await CanSelectNowAsync(email))
            return BadRequest(new { message = "La elección de actividades académicas no está habilitada en este momento." });

        // Quitar el Taller invalida la Charla Simultánea elegida (dependía de
        // su Familia) — pero si esa Simultánea ya está confirmada
        // definitivamente, no la tocamos: se corta la operación en vez de
        // borrar un dato ya confirmado.
        if (blockId == TallerBlockId)
        {
            var dependentSimultanea = await _db.ActivitySelections
                .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == email.ToLower() && s.BlockId == SimultaneaBlockId);
            if (dependentSimultanea?.IsConfirmed == true)
                return BadRequest(new { message = "Ya confirmaste tu charla simultánea — no se puede quitar el taller." });
        }

        using var tx = await _db.Database.BeginTransactionAsync();
        await _db.SelectableActivities
            .Where(a => a.Id == existing.ActivityId)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount - 1));
        _db.ActivitySelections.Remove(existing);
        await _db.SaveChangesAsync();

        if (blockId == TallerBlockId)
        {
            var dependentSimultanea = await _db.ActivitySelections
                .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == email.ToLower() && s.BlockId == SimultaneaBlockId);
            if (dependentSimultanea != null)
            {
                await _db.SelectableActivities
                    .Where(a => a.Id == dependentSimultanea.ActivityId)
                    .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount - 1));
                _db.ActivitySelections.Remove(dependentSimultanea);
                await _db.SaveChangesAsync();
            }
        }

        await tx.CommitAsync();
        await ReloadUserAsync(email);

        return NoContent();
    }

    // ── Listado completo de selecciones (admin, sin restricción) ────────────
    //
    // Filtros opcionales: activityCode (código de la actividad, ej "4.01"),
    // faculty (facultad/delegación exacta), sortBy (date_desc | date_asc,
    // default date_desc = más reciente primero).
    [HttpGet("all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? activityCode,
        [FromQuery] string? faculty,
        [FromQuery] string? sortBy)
    {
        var query = BuildSelectionQuery(_db, activityCode, faculty);
        var results = await ApplySort(query, sortBy).ToListAsync();
        return Ok(results);
    }

    // ── Listado de selecciones restringido a las facultades del delegado ────
    //
    // Mismos filtros que /all, pero solo devuelve gente de las facultades que
    // el delegado gestiona (ManagedFaculties, o DelegationName si está vacío
    // — mismo patrón que RegistrationsController.GetByDelegate).
    [HttpGet("delegate")]
    public async Task<IActionResult> GetByDelegate(
        [FromQuery] string email,
        [FromQuery] string? activityCode,
        [FromQuery] string? faculty,
        [FromQuery] string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Falta el email." });

        var delegateUser = _db.Users.AsEnumerable()
            .FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (delegateUser == null)
            return NotFound(new { message = "Delegado no encontrado." });

        var managedFaculties = delegateUser.ManagedFaculties.Count > 0
            ? delegateUser.ManagedFaculties
            : (delegateUser.DelegationName != null ? new List<string> { delegateUser.DelegationName } : new List<string>());

        if (managedFaculties.Count == 0)
            return Ok(Array.Empty<object>());

        var query = BuildSelectionQuery(_db, activityCode, faculty)
            .Where(x => managedFaculties.Contains(x.Faculty ?? "", StringComparer.OrdinalIgnoreCase));

        var results = await ApplySort(query, sortBy).ToListAsync();
        return Ok(results);
    }

    private static IQueryable<SelectionListItem> BuildSelectionQuery(
        ApplicationDbContext db, string? activityCode, string? faculty)
    {
        var query =
            from s in db.ActivitySelections
            join a in db.SelectableActivities on s.ActivityId equals a.Id
            join r in db.Registrations on s.UserEmail.ToLower() equals r.Email.ToLower() into regJoin
            from r in regJoin.DefaultIfEmpty()
            select new SelectionListItem
            {
                RegistrationId = r != null ? r.Id : (int?)null,
                Name = r != null ? r.Name : null,
                Lastname = r != null ? r.Lastname : null,
                Email = s.UserEmail,
                Faculty = r != null ? r.Faculty : null,
                BlockId = s.BlockId,
                ActivityId = a.Id,
                ActivityCode = a.Code,
                ActivityTitle = a.Title,
                IsConfirmed = s.IsConfirmed,
                SelectedAt = s.SelectedAt,
                ConfirmedAt = s.ConfirmedAt,
            };

        if (!string.IsNullOrWhiteSpace(activityCode))
            query = query.Where(x => x.ActivityCode == activityCode);

        if (!string.IsNullOrWhiteSpace(faculty))
            query = query.Where(x => x.Faculty != null && x.Faculty.ToLower() == faculty.ToLower());

        return query;
    }

    private static IQueryable<SelectionListItem> ApplySort(IQueryable<SelectionListItem> query, string? sortBy) =>
        sortBy switch
        {
            "date_asc" => query.OrderBy(x => x.SelectedAt),
            _ => query.OrderByDescending(x => x.SelectedAt), // default: más reciente primero
        };

    private class SelectionListItem
    {
        public int? RegistrationId { get; set; }
        public string? Name { get; set; }
        public string? Lastname { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Faculty { get; set; }
        public int BlockId { get; set; }
        public int ActivityId { get; set; }
        public string ActivityCode { get; set; } = string.Empty;
        public string ActivityTitle { get; set; } = string.Empty;
        public bool IsConfirmed { get; set; }
        public DateTime SelectedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }

    // ── Confirmación definitiva (irreversible, por bloque) ───────────────────
    //
    // Cada categoría (Taller / Simultánea / Solidaria / Visita Técnica) se
    // confirma de forma independiente — no es "todo o nada". Pedido del
    // equipo (WhatsApp, 2026-09-27): "hacer que la confirmación sea en cada
    // una de las actividades, selección definitiva individual" — para que un
    // problema puntual en una categoría (cupo agotado, indecisión) no
    // bloquee confirmar las demás.

    public record ConfirmRequest(string Email, int BlockId);

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest req)
    {
        var confirmed = new List<ActivitySelection>();
        IActionResult result;
        await EnsureCachesAsync();   // antes de tomar la cola: la carga inicial también la usa
        await HotCache.WriteGate.WaitAsync();
        try { result = await ConfirmCore(req, confirmed); }
        finally { HotCache.WriteGate.Release(); }

        // Los mails salen fuera de la cola de escrituras (no retienen el lock).
        if (confirmed.Count > 0)
        {
            await SendPilotConfirmationEmailAsync(req.Email, confirmed[0]);
            await SendAcademicActivitiesConfirmationEmailIfCompleteAsync(req.Email);
        }
        return result;
    }

    private async Task<IActionResult> ConfirmCore(ConfirmRequest req, List<ActivitySelection> confirmedOut)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Falta el email." });

        var selection = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == req.Email.ToLower() && s.BlockId == req.BlockId);

        if (selection == null)
            return BadRequest(new { message = "Todavía no elegiste una opción en esta categoría." });
        if (selection.IsConfirmed)
            return BadRequest(new { message = "Ya habías confirmado esta categoría." });
        if (req.BlockId != 1 && !await CanSelectNowAsync(req.Email))
            return BadRequest(new { message = "La elección de actividades académicas no está habilitada en este momento." });

        // Defensa extra: el acople Taller↔Familia↔Simultánea ya se valida en
        // Select, pero se re-chequea acá por las dudas antes de volver la
        // selección irreversible.
        if (req.BlockId == TallerBlockId || req.BlockId == SimultaneaBlockId)
        {
            var otherBlockId = req.BlockId == TallerBlockId ? SimultaneaBlockId : TallerBlockId;
            var otherSelection = await _db.ActivitySelections
                .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == req.Email.ToLower() && s.BlockId == otherBlockId);
            if (otherSelection != null)
            {
                var families = await _db.SelectableActivities
                    .Where(a => a.Id == selection.ActivityId || a.Id == otherSelection.ActivityId)
                    .ToDictionaryAsync(a => a.Id, a => a.Family);
                if (families[selection.ActivityId] != families[otherSelection.ActivityId])
                    return BadRequest(new { message = "Tu charla simultánea no corresponde a la familia de tu taller — volvé a elegir." });
            }
        }

        var now = DateTime.Now;
        selection.IsConfirmed = true;
        selection.ConfirmedAt = now;
        await _db.SaveChangesAsync();
        await ReloadUserAsync(req.Email);
        confirmedOut.Add(selection);

        return Ok(new { message = "Categoría confirmada.", confirmedAt = now, blockId = req.BlockId });
    }

    // Envía el mail de "visita técnica elegida" solo si la cuenta que confirmó
    // está en la lista piloto (ver PilotRecipients) y lo que confirmó fue
    // justo el bloque de Visita Técnica. No falla la confirmación si el
    // envío tiene algún problema — la selección ya quedó guardada.
    private async Task SendPilotConfirmationEmailAsync(string userEmail, ActivitySelection confirmedSelection)
    {
        if (confirmedSelection.BlockId != 1) return;
        if (!PilotRecipients.TryGetValue(userEmail.ToLower(), out var recipients)) return;

        var visita = await _db.SelectableActivities
            .Where(a => a.Id == confirmedSelection.ActivityId)
            .Select(a => new { a.Code, a.Title })
            .FirstOrDefaultAsync();
        if (visita == null) return;

        foreach (var (name, email) in recipients)
        {
            try
            {
                await _email.SendActivitySelectionConfirmedAsync(email, name, visita.Code, visita.Title, EppPdfUrl);
            }
            catch
            {
                // no interrumpir la confirmación por un fallo de envío puntual
            }
        }
    }

    // Mail de confirmación de Taller + Charla Simultánea + Solidaria — a
    // diferencia de SendPilotConfirmationEmailAsync (Visita Técnica, todavía
    // restringido a PilotRecipients), este sale para cualquier asistente que
    // confirme, porque es la feature ya en producción (acción del 2026-09-24:
    // "Configurar Confirmación"). Como ahora cada bloque se confirma por
    // separado, este mail (que junta los 3) solo sale una vez que las 3
    // categorías quedaron confirmadas — no en cada confirmación individual.
    private async Task SendAcademicActivitiesConfirmationEmailIfCompleteAsync(string userEmail)
    {
        var chosen = await (
            from s in _db.ActivitySelections
            join a in _db.SelectableActivities on s.ActivityId equals a.Id
            where s.UserEmail.ToLower() == userEmail.ToLower()
                && (s.BlockId == TallerBlockId || s.BlockId == SimultaneaBlockId || s.BlockId == 4)
            select new { s.BlockId, s.IsConfirmed, a.Code, a.Title }
        ).ToListAsync();

        var taller = chosen.FirstOrDefault(c => c.BlockId == TallerBlockId);
        var simultanea = chosen.FirstOrDefault(c => c.BlockId == SimultaneaBlockId);
        var solidaria = chosen.FirstOrDefault(c => c.BlockId == 4);

        var registration = await _db.Registrations
            .FirstOrDefaultAsync(r => r.Email.ToLower() == userEmail.ToLower());

        // Quien está en el Desafío de Barreras no elige Taller ni Simultánea:
        // para ellos el mail sale cuando confirman la Solidaria.
        var isBarreras = registration?.InterestedInMaccaferri == true;

        // Todavía falta confirmar alguna categoría — no se manda nada todavía.
        if (solidaria is not { IsConfirmed: true })
            return;
        if (!isBarreras && (taller is not { IsConfirmed: true } || simultanea is not { IsConfirmed: true }))
            return;

        var name = registration != null ? $"{registration.Name} {registration.Lastname}" : userEmail;

        try
        {
            await _email.SendAcademicActivitiesConfirmedAsync(
                userEmail, name,
                taller?.Code, taller?.Title,
                simultanea?.Code, simultanea?.Title,
                solidaria.Code, solidaria.Title,
                isBarreras);
        }
        catch
        {
            // no interrumpir la confirmación por un fallo de envío puntual
        }
    }

    // ── Reasignación manual (excepción del directorio) ───────────────────────
    //
    // Mueve a `TargetEmail` a otra actividad del mismo bloque que la que ya
    // tenía elegida (o le crea una si no tenía). A diferencia de /select:
    //   - No le saca el cupo a nadie: si la actividad destino ya está llena,
    //     se le suma 1 a su Capacity en vez de rechazar el cambio.
    //   - No dispara el mail de confirmación — es una excepción administrativa,
    //     no una elección nueva del asistente.
    //   - Solo la cuenta de directorio puede usarlo (ver DirectorioOverrideEmail).
    public record AdminOverrideRequest(string AdminEmail, string TargetEmail, int ActivityId);

    [HttpPost("admin-override")]
    public async Task<IActionResult> AdminOverride([FromBody] AdminOverrideRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AdminEmail) ||
            !req.AdminEmail.Equals(DirectorioOverrideEmail, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { message = "Esta acción está restringida a la cuenta de Directorio." });
        }

        if (string.IsNullOrWhiteSpace(req.TargetEmail))
            return BadRequest(new { message = "Falta el email de la persona a reasignar." });

        var newActivity = await _db.SelectableActivities.FindAsync(req.ActivityId);
        if (newActivity == null)
            return NotFound(new { message = "La actividad indicada no existe." });

        using var tx = await _db.Database.BeginTransactionAsync();

        var existing = await _db.ActivitySelections
            .FirstOrDefaultAsync(s => s.UserEmail.ToLower() == req.TargetEmail.ToLower() && s.BlockId == newActivity.BlockId);

        if (existing != null && existing.ActivityId == newActivity.Id)
        {
            await tx.RollbackAsync();
            return Ok(new { message = "Ya estaba anotado/a en esta actividad.", activityId = newActivity.Id });
        }

        // Si hay cupo libre, se reserva como siempre. Si no, se agranda la
        // capacidad en 1 para hacerle lugar sin desplazar a nadie más.
        var hasRoom = await _db.SelectableActivities.AnyAsync(a => a.Id == newActivity.Id && a.TakenCount < a.Capacity);
        if (hasRoom)
        {
            await _db.SelectableActivities.Where(a => a.Id == newActivity.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount + 1));
        }
        else
        {
            await _db.SelectableActivities.Where(a => a.Id == newActivity.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Capacity, a => a.Capacity + 1)
                    .SetProperty(a => a.TakenCount, a => a.TakenCount + 1));
        }

        if (existing != null)
        {
            await _db.SelectableActivities.Where(a => a.Id == existing.ActivityId)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TakenCount, a => a.TakenCount - 1));

            existing.ActivityId = newActivity.Id;
            existing.SelectedAt = DateTime.Now;
            // Una reasignación de directorio es definitiva por definición —
            // si la selección original había quedado en borrador (la persona
            // nunca llegó a confirmar), no debe seguir en borrador después
            // de esto.
            existing.IsConfirmed = true;
            existing.ConfirmedAt ??= DateTime.Now;
            await _db.SaveChangesAsync();
        }
        else
        {
            _db.ActivitySelections.Add(new ActivitySelection
            {
                UserEmail = req.TargetEmail,
                BlockId = newActivity.BlockId,
                ActivityId = newActivity.Id,
                IsConfirmed = true,
                ConfirmedAt = DateTime.Now,
            });
            await _db.SaveChangesAsync();
        }

        await tx.CommitAsync();
        await ReloadUserWithGateAsync(req.TargetEmail);

        // Sin mail — acción administrativa excepcional, no una elección del
        // asistente. Queda igual registrada en los logs del servidor.
        _logger.LogWarning(
            "[ADMIN OVERRIDE] {Admin} reasignó a {Target} a {Code} - {Title} (bloque {BlockId})",
            req.AdminEmail, req.TargetEmail, newActivity.Code, newActivity.Title, newActivity.BlockId);

        return Ok(new { message = "Reasignación realizada.", activityId = newActivity.Id, activityCode = newActivity.Code });
    }

    // ── Exportar a Excel lo que se ve en la tab "Elección de actividades" ────
    //
    // El panel filtra y ordena en el navegador (búsqueda, categoría, actividad,
    // delegación, orden) y hasta arma filas "Sin elegir" a partir del padrón,
    // así que le manda a este endpoint exactamente las filas que están a la
    // vista: el Excel respeta cualquier filtro aplicado en pantalla.
    public record SelectionExportRow(
        string? Lastname, string? Name, string? Email, string? Faculty, int BlockId,
        string? ActivityCode, string? ActivityTitle, bool IsConfirmed,
        string? SelectedAt, string? ConfirmedAt);

    public record SelectionExportRequest(List<SelectionExportRow>? Rows, string? Filters);

    private static string ExcelSafe(string? value)
    {
        var v = value ?? "";
        // Evita que un valor que arranca con = + - @ se interprete como fórmula.
        return v.Length > 0 && "=+-@".Contains(v[0]) ? "'" + v : v;
    }

    private static string BlockLabel(int blockId) => blockId switch
    {
        1 => "Visita Técnica",
        TallerBlockId => "Taller",
        SimultaneaBlockId => "Charla Simultánea",
        4 => "Actividad Solidaria",
        _ => $"Bloque {blockId}",
    };

    [HttpPost("export")]
    public IActionResult ExportSelections([FromBody] SelectionExportRequest req)
    {
        var rows = req.Rows ?? new List<SelectionExportRow>();
        if (rows.Count > 20000)
            return BadRequest(new { message = "Demasiadas filas para exportar." });

        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.Worksheets.Add("Elección de actividades");

        var headers = new[]
        {
            "Fecha de elección", "Apellido", "Nombre", "Email", "Delegación",
            "Categoría", "Código", "Actividad", "Estado", "Fecha de confirmación",
        };
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
        }

        var r = 2;
        foreach (var row in rows)
        {
            var hasActivity = !string.IsNullOrEmpty(row.ActivityCode);
            ws.Cell(r, 1).Value = ExcelSafe(row.SelectedAt);
            ws.Cell(r, 2).Value = ExcelSafe(row.Lastname);
            ws.Cell(r, 3).Value = ExcelSafe(row.Name);
            ws.Cell(r, 4).Value = ExcelSafe(row.Email);
            ws.Cell(r, 5).Value = ExcelSafe(row.Faculty);
            ws.Cell(r, 6).Value = hasActivity ? BlockLabel(row.BlockId) : "";
            ws.Cell(r, 7).Value = ExcelSafe(row.ActivityCode);
            ws.Cell(r, 8).Value = hasActivity ? ExcelSafe(row.ActivityTitle) : "Sin elegir";
            ws.Cell(r, 9).Value = !hasActivity ? "Sin elegir" : row.IsConfirmed ? "Confirmada" : "Borrador";
            ws.Cell(r, 10).Value = ExcelSafe(row.ConfirmedAt);
            r++;
        }

        ws.SheetView.FreezeRows(1);
        if (rows.Count > 0) ws.Range(1, 1, r - 1, headers.Length).SetAutoFilter();
        ws.Columns().AdjustToContents();

        var info = wb.Worksheets.Add("Filtros aplicados");
        info.Cell(1, 1).Value = "Filtros aplicados al exportar";
        info.Cell(1, 1).Style.Font.Bold = true;
        info.Cell(2, 1).Value = string.IsNullOrWhiteSpace(req.Filters) ? "Sin filtros (todas las filas)." : ExcelSafe(req.Filters);
        info.Cell(3, 1).Value = $"Filas exportadas: {rows.Count}";
        info.Column(1).AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "eleccion_actividades.xlsx");
    }

    // ── Asignación automática de lo que quedó sin elegir ─────────────────────
    //
    // La Guía de Elección promete: "si no elegís Taller, Charla o Act. de
    // Compromiso Social, el sistema te asigna una de manera automática y
    // aleatoria; una vez asignado no se admiten cambios". Se corre una sola
    // vez, a mano, después del cierre de la ventana. Por cada asistente con
    // inscripción paga:
    //   - un borrador sin confirmar se confirma tal cual (cuenta como elección);
    //   - lo que falta se sortea entre las opciones con cupo, respetando la
    //     Familia (la Simultánea siempre de la misma Familia que el Taller);
    //   - quien está en el Desafío de Barreras no recibe Taller ni Simultánea.
    // No toca Visitas Técnicas (bloque 1) y no envía mails. DryRun (default
    // true) calcula y devuelve el resumen sin guardar nada.
    public record AutoAssignRequest(string AdminEmail, bool? DryRun);

    [HttpPost("auto-assign")]
    public async Task<IActionResult> AutoAssign([FromBody] AutoAssignRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AdminEmail) ||
            !req.AdminEmail.Equals(DirectorioOverrideEmail, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { message = "Esta acción está restringida a la cuenta de Directorio." });
        }

        var dryRun = req.DryRun ?? true;
        const int SolidariaBlockId = 4;
        var blockIds = new[] { TallerBlockId, SimultaneaBlockId, SolidariaBlockId };

        var activities = await _db.SelectableActivities
            .Where(a => blockIds.Contains(a.BlockId))
            .ToListAsync();
        var taken = activities.ToDictionary(a => a.Id, a => a.TakenCount);

        var people = await _db.Registrations
            .Where(r => r.Status == "Paid")
            .Select(r => new { r.Email, r.InterestedInMaccaferri })
            .ToListAsync();
        var emails = people.Select(p => p.Email.ToLower()).ToHashSet();

        var allSelections = await _db.ActivitySelections
            .Where(s => blockIds.Contains(s.BlockId))
            .ToListAsync();
        var byUser = allSelections
            .Where(s => emails.Contains(s.UserEmail.ToLower()))
            .GroupBy(s => s.UserEmail.ToLower())
            .ToDictionary(g => g.Key, g => g.ToList());

        var rng = Random.Shared;
        var now = DateTime.Now;
        var confirmedDrafts = 0;
        var assigned = new Dictionary<int, int> { [TallerBlockId] = 0, [SimultaneaBlockId] = 0, [SolidariaBlockId] = 0 };
        var failures = new List<string>();
        var toAdd = new List<ActivitySelection>();

        bool HasRoom(SelectableActivity a) => taken[a.Id] < a.Capacity;
        SelectableActivity? Pick(IEnumerable<SelectableActivity> pool)
        {
            var open = pool.Where(HasRoom).ToList();
            return open.Count == 0 ? null : open[rng.Next(open.Count)];
        }

        foreach (var person in people.OrderBy(_ => rng.Next()))
        {
            var email = person.Email;
            var mine = byUser.TryGetValue(email.ToLower(), out var list) ? list : new List<ActivitySelection>();
            ActivitySelection? Existing(int blockId) => mine.FirstOrDefault(s => s.BlockId == blockId);

            // Los borradores se confirman como están.
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
                SelectableActivity? taller = tallerSel == null ? null : activities.First(a => a.Id == tallerSel.ActivityId);

                if (taller == null)
                {
                    // Solo sirve un Taller cuya Familia todavía tenga alguna Simultánea con cupo.
                    var viable = activities
                        .Where(a => a.BlockId == TallerBlockId && HasRoom(a)
                            && activities.Any(s => s.BlockId == SimultaneaBlockId && s.Family == a.Family && HasRoom(s)))
                        .ToList();
                    if (simSel != null)
                    {
                        var simFamily = activities.First(a => a.Id == simSel.ActivityId).Family;
                        viable = viable.Where(a => a.Family == simFamily).ToList();
                    }
                    taller = Pick(viable);
                    if (taller == null) { failures.Add($"{email}: sin Taller con cupo"); continue; }
                    taken[taller.Id]++;
                    var sel = new ActivitySelection { UserEmail = email, BlockId = TallerBlockId, ActivityId = taller.Id, IsConfirmed = true, ConfirmedAt = now };
                    toAdd.Add(sel); mine.Add(sel); assigned[TallerBlockId]++;
                }

                if (simSel == null)
                {
                    var sim = Pick(activities.Where(a => a.BlockId == SimultaneaBlockId && a.Family == taller.Family));
                    if (sim == null) { failures.Add($"{email}: sin Simultánea con cupo en la familia {taller.Family}"); continue; }
                    taken[sim.Id]++;
                    var sel = new ActivitySelection { UserEmail = email, BlockId = SimultaneaBlockId, ActivityId = sim.Id, IsConfirmed = true, ConfirmedAt = now };
                    toAdd.Add(sel); mine.Add(sel); assigned[SimultaneaBlockId]++;
                }
            }

            if (Existing(SolidariaBlockId) == null)
            {
                var sol = Pick(activities.Where(a => a.BlockId == SolidariaBlockId));
                if (sol == null) { failures.Add($"{email}: sin Solidaria con cupo"); continue; }
                taken[sol.Id]++;
                var sel = new ActivitySelection { UserEmail = email, BlockId = SolidariaBlockId, ActivityId = sol.Id, IsConfirmed = true, ConfirmedAt = now };
                toAdd.Add(sel); mine.Add(sel); assigned[SolidariaBlockId]++;
            }
        }

        if (!dryRun)
        {
            using var tx = await _db.Database.BeginTransactionAsync();
            _db.ActivitySelections.AddRange(toAdd);
            foreach (var a in activities) a.TakenCount = taken[a.Id];
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            InvalidateAll();
            _logger.LogWarning("[AUTO-ASSIGN] {Admin} asignó {T} talleres, {S} simultáneas, {So} solidarias; {D} borradores confirmados; {F} sin lugar",
                req.AdminEmail, assigned[TallerBlockId], assigned[SimultaneaBlockId], assigned[SolidariaBlockId], confirmedDrafts, failures.Count);
        }
        else
        {
            _db.ChangeTracker.Clear();
        }

        return Ok(new
        {
            dryRun,
            people = people.Count,
            confirmedDrafts,
            assignedTalleres = assigned[TallerBlockId],
            assignedSimultaneas = assigned[SimultaneaBlockId],
            assignedSolidarias = assigned[SolidariaBlockId],
            failures,
        });
    }

    // ── Reenvío masivo del mail de confirmación (backfill puntual) ───────────
    //
    // El mail de "visita técnica confirmada" quedó restringido a
    // PilotRecipients desde que se armó la feature — ningún asistente real lo
    // recibió nunca. Este endpoint lo manda una vez a todo el mundo que ya
    // tiene su visita técnica (bloque 1) confirmada, sin tocar esa restricción
    // (que sigue rigiendo para confirmaciones futuras, hasta que se saque
    // explícitamente). Solo directorio puede dispararlo.
    public record ResendConfirmationsRequest(string AdminEmail, List<string>? ExcludeEmails);

    [HttpPost("resend-confirmations")]
    public async Task<IActionResult> ResendConfirmations([FromBody] ResendConfirmationsRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AdminEmail) ||
            !req.AdminEmail.Equals(DirectorioOverrideEmail, StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(403, new { message = "Esta acción está restringida a la cuenta de Directorio." });
        }

        var excludeSet = new HashSet<string>((req.ExcludeEmails ?? new()).Select(e => e.ToLower()));

        var pending = await (
            from s in _db.ActivitySelections
            join a in _db.SelectableActivities on s.ActivityId equals a.Id
            where s.BlockId == 1 && s.IsConfirmed
            select new { s.UserEmail, a.Code, a.Title }
        ).ToListAsync();
        var toSendCount = pending.Count(p => !excludeSet.Contains(p.UserEmail.ToLower()));

        // Corre desacoplado del request HTTP: 700+ mails tardan más que
        // cualquier timeout razonable de reverse proxy (ya nos pasó una vez —
        // el envío sincrónico se cortó a mitad de camino cuando el proxy
        // cerró la conexión). Usa su propio scope/DbContext porque el de
        // este controller se dispone en cuanto el request HTTP termina.
        _ = Task.Run(() => RunBulkResendAsync(req.AdminEmail, excludeSet));

        return Accepted(new { message = "Reenvío iniciado en segundo plano.", toSend = toSendCount, excluded = excludeSet.Count });
    }

    private async Task RunBulkResendAsync(string adminEmail, HashSet<string> excludeEmails)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var confirmed = await (
            from s in db.ActivitySelections
            join a in db.SelectableActivities on s.ActivityId equals a.Id
            where s.BlockId == 1 && s.IsConfirmed
            select new { s.UserEmail, a.Code, a.Title }
        ).ToListAsync();

        var toSend = confirmed.Where(c => !excludeEmails.Contains(c.UserEmail.ToLower())).ToList();

        var emails = toSend.Select(c => c.UserEmail.ToLower()).ToList();
        var regsByEmail = await db.Registrations
            .Where(r => emails.Contains(r.Email.ToLower()))
            .ToDictionaryAsync(r => r.Email.ToLower(), r => $"{r.Name} {r.Lastname}");

        _logger.LogWarning(
            "[BULK RESEND STARTED] {Admin} — {Count} a enviar ({Excluded} ya enviados antes, excluidos).",
            adminEmail, toSend.Count, excludeEmails.Count);

        foreach (var c in toSend)
        {
            var name = regsByEmail.TryGetValue(c.UserEmail.ToLower(), out var n) ? n : c.UserEmail;
            // SendActivitySelectionConfirmedAsync ya loguea éxito/error puntual
            // y no propaga excepciones.
            await _email.SendActivitySelectionConfirmedAsync(c.UserEmail, name, c.Code, c.Title, EppPdfUrl);
            await Task.Delay(150);
        }

        _logger.LogWarning("[BULK RESEND DONE] {Admin} — terminó de recorrer {Count} personas.", adminEmail, toSend.Count);
    }
}
