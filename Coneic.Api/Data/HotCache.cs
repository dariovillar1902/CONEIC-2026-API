using System.Collections.Concurrent;
using Coneic.Api.Models;

namespace Coneic.Api.Data;

/// <summary>
/// Índices en memoria (la app corre en una sola instancia). Se refrescan en
/// segundo plano desde ActivitySelectionController; los pedidos más frecuentes
/// de la elección (login, by-email, blocks, status) se resuelven acá y no
/// compiten con las escrituras por el lock del archivo SQLite (que vive en un
/// disco compartido CIFS y bajo carga encolaba todo por decenas de segundos).
/// </summary>
public static class HotCache
{
    /// <summary>Una escritura (o un refresco de lectura) a la vez contra SQLite; la espera es asíncrona.</summary>
    public static readonly SemaphoreSlim WriteGate = new(1, 1);

    public static volatile ConcurrentDictionary<string, User> Users = new();
    public static volatile ConcurrentDictionary<string, Registration> Registrations = new();
    public static volatile bool RegistrationsLoaded;
}
