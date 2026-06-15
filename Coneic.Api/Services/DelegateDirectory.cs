namespace Coneic.Api.Services;

/// <summary>
/// Directorio estático de delegados y vocales del CONEIC 2026.
/// Fuente: planilla "Deles para CoNEIC (2).xlsx" — actualizar junto con la planilla.
///
/// Las claves primarias coinciden EXACTAMENTE con los nombres de facultad que usa
/// el formulario de inscripción (filiales.js). Las claves secundarias son alias
/// comunes para búsquedas manuales o provenientes del panel de delegados.
/// </summary>
public sealed record ContactPerson(string Name, string Phone);
public sealed record DelegationInfo(string DelegationName, IReadOnlyList<ContactPerson> Contacts);

internal static class DelegateDirectory
{
    /// <summary>
    /// Devuelve la info de delegación para una facultad dada.
    /// La búsqueda es case-insensitive y tolera espacios extra.
    /// Retorna null si la facultad no tiene entrada conocida.
    /// </summary>
    public static DelegationInfo? Lookup(string? faculty)
    {
        if (string.IsNullOrWhiteSpace(faculty)) return null;
        var key = faculty.Trim().ToLowerInvariant();
        return Entries.TryGetValue(key, out var info) ? info : null;
    }

    // ── Helpers de construcción ──────────────────────────────────────────────
    private static DelegationInfo D(string name, params (string Name, string Phone)[] contacts) =>
        new(name, contacts.Select(c => new ContactPerson(c.Name, c.Phone)).ToList());

    // ═══════════════════════════════════════════════════════════════════════════
    // DIRECTORIO COMPLETO
    // Claves primarias = nombre exacto de filiales.js (lo que envía el formulario)
    // ═══════════════════════════════════════════════════════════════════════════
    private static readonly Dictionary<string, DelegationInfo> Entries =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // ── REGIÓN CENTRO ────────────────────────────────────────────────────

        ["utn - facultad regional venado tuerto"] = D("UTN - Facultad Regional Venado Tuerto",
            ("Tomas Daniel Stipanovich", "3462638309"),
            ("Stefania Giumbini",        "3462518537")),
        ["utn venado tuerto"] = D("UTN - Facultad Regional Venado Tuerto",
            ("Tomas Daniel Stipanovich", "3462638309"),
            ("Stefania Giumbini",        "3462518537")),

        ["utn - facultad regional rafaela"] = D("UTN - Facultad Regional Rafaela",
            ("Tamara Ghirardotti",       "3492610096"),
            ("Martina Fiorella Paredes", "3492332171")),
        ["utn rafaela"] = D("UTN - Facultad Regional Rafaela",
            ("Tamara Ghirardotti",       "3492610096"),
            ("Martina Fiorella Paredes", "3492332171")),

        ["utn - facultad regional rosario"] = D("UTN - Facultad Regional Rosario",
            ("Francisco Coppari", "3413208022"),
            ("Luana Plese",       "3406421101")),
        ["utn rosario"] = D("UTN - Facultad Regional Rosario",
            ("Francisco Coppari", "3413208022"),
            ("Luana Plese",       "3406421101")),

        ["universidad nacional de rosario"] = D("Universidad Nacional de Rosario",
            ("Valentin Cordoba", "3382672049")),
        ["un rosario"] = D("Universidad Nacional de Rosario",
            ("Valentin Cordoba", "3382672049")),

        ["utn - facultad regional paraná"] = D("UTN - Facultad Regional Paraná",
            ("Ramiro Nicolás Acosta", "3435339453"),
            ("Sofía Sattler",         "3435444008")),
        ["utn - facultad regional parana"] = D("UTN - Facultad Regional Paraná",
            ("Ramiro Nicolás Acosta", "3435339453"),
            ("Sofía Sattler",         "3435444008")),
        ["utn paraná"] = D("UTN - Facultad Regional Paraná",
            ("Ramiro Nicolás Acosta", "3435339453"),
            ("Sofía Sattler",         "3435444008")),
        ["utn parana"] = D("UTN - Facultad Regional Paraná",
            ("Ramiro Nicolás Acosta", "3435339453"),
            ("Sofía Sattler",         "3435444008")),

        ["utn - facultad regional santa fe"] = D("UTN - Facultad Regional Santa Fe",
            ("Ana Breit", "3462611376")),
        ["utn santa fe"] = D("UTN - Facultad Regional Santa Fe",
            ("Ana Breit", "3462611376")),

        // ── REGIÓN ESTE ──────────────────────────────────────────────────────

        ["universidad nacional de la plata"] = D("Universidad Nacional de La Plata",
            ("Facundo Félix Salomán", "2344410495"),
            ("Leonel Loera Peroni",   "2215249407")),
        ["un la plata"] = D("Universidad Nacional de La Plata",
            ("Facundo Félix Salomán", "2344410495"),
            ("Leonel Loera Peroni",   "2215249407")),
        ["unlp"] = D("Universidad Nacional de La Plata",
            ("Facundo Félix Salomán", "2344410495"),
            ("Leonel Loera Peroni",   "2215249407")),

        ["utn - facultad regional general pacheco"] = D("UTN - Facultad Regional General Pacheco",
            ("Santiago Sanchez Diaz", "1168576041"),
            ("Nicolas Bohl",          "33397819")),
        ["utn general pacheco"] = D("UTN - Facultad Regional General Pacheco",
            ("Santiago Sanchez Diaz", "1168576041"),
            ("Nicolas Bohl",          "33397819")),

        ["utn - facultad regional buenos aires"] = D("UTN - Facultad Regional Buenos Aires",
            ("Leandro David Diaz",        "1134987525"),
            ("Haylin Nadeyva Silva Rodas", "1126834939")),
        ["utn buenos aires"] = D("UTN - Facultad Regional Buenos Aires",
            ("Leandro David Diaz",        "1134987525"),
            ("Haylin Nadeyva Silva Rodas", "1126834939")),

        ["universidad nacional de la matanza"] = D("Universidad Nacional de La Matanza",
            ("Marisol Rojas Cabañas", "1125763197")),
        ["universidad nacional de la matanza"] = D("Universidad Nacional de La Matanza",
            ("Marisol Rojas Cabañas", "1125763197")),
        ["un la matanza"] = D("Universidad Nacional de La Matanza",
            ("Marisol Rojas Cabañas", "1125763197")),

        // Avellaneda, Belgrano, UBA, UCA, Defensa, CU, Concordia, FR La Plata, Morón
        // son cubiertos por los vocales del Este: Meneses y Ingratta
        ["utn - facultad regional avellaneda"] = D("UTN - Facultad Regional Avellaneda",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["utn avellaneda"] = D("UTN - Facultad Regional Avellaneda",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        ["universidad de belgrano"] = D("Universidad de Belgrano",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        ["universidad de buenos aires"] = D("Universidad de Buenos Aires",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["uba"] = D("Universidad de Buenos Aires",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        ["universidad católica argentina"] = D("Universidad Católica Argentina",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["universidad catolica argentina"] = D("Universidad Católica Argentina",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["uca"] = D("Universidad Católica Argentina",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        ["universidad de la defensa nacional"] = D("Universidad de la Defensa Nacional",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["undef"] = D("Universidad de la Defensa Nacional",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        ["utn - facultad regional concepción del uruguay"] = D("UTN - Facultad Regional Concepción del Uruguay",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["utn - facultad regional concepcion del uruguay"] = D("UTN - Facultad Regional Concepción del Uruguay",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        ["utn - facultad regional concordia"] = D("UTN - Facultad Regional Concordia",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["utn fr concordia"] = D("UTN - Facultad Regional Concordia",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        ["utn - facultad regional la plata"] = D("UTN - Facultad Regional La Plata",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["utn fr la plata"] = D("UTN - Facultad Regional La Plata",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        ["universidad nacional de morón"] = D("Universidad Nacional de Morón",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["universidad nacional de moron"] = D("Universidad Nacional de Morón",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["un morón"] = D("Universidad Nacional de Morón",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),
        ["un moron"] = D("Universidad Nacional de Morón",
            ("Santiago Meneses",  "3462611376"),
            ("Agostina Ingratta", "2974019956")),

        // ── REGIÓN NORTE ─────────────────────────────────────────────────────

        ["universidad nacional del nordeste"] = D("Universidad Nacional del Nordeste",
            ("Cristian Gabriel Ledesma", "3794707691"),
            ("Celeste Milena Sabaj",     "3704925065")),
        ["unne"] = D("Universidad Nacional del Nordeste",
            ("Cristian Gabriel Ledesma", "3794707691"),
            ("Celeste Milena Sabaj",     "3704925065")),

        ["universidad nacional de tucumán"] = D("Universidad Nacional de Tucumán",
            ("Alvaro Ramiro Brodersen", "3815294542"),
            ("Matias Aranda",           "3816633939")),
        ["universidad nacional de tucuman"] = D("Universidad Nacional de Tucumán",
            ("Alvaro Ramiro Brodersen", "3815294542"),
            ("Matias Aranda",           "3816633939")),
        ["un tucumán"] = D("Universidad Nacional de Tucumán",
            ("Alvaro Ramiro Brodersen", "3815294542"),
            ("Matias Aranda",           "3816633939")),
        ["un tucuman"] = D("Universidad Nacional de Tucumán",
            ("Alvaro Ramiro Brodersen", "3815294542"),
            ("Matias Aranda",           "3816633939")),

        ["utn - facultad regional tucumán"] = D("UTN - Facultad Regional Tucumán",
            ("Lara Josefina Chauque", "3813396300"),
            ("Maite Ines Morales",    "3813524349")),
        ["utn - facultad regional tucuman"] = D("UTN - Facultad Regional Tucumán",
            ("Lara Josefina Chauque", "3813396300"),
            ("Maite Ines Morales",    "3813524349")),
        ["utn tucumán"] = D("UTN - Facultad Regional Tucumán",
            ("Lara Josefina Chauque", "3813396300"),
            ("Maite Ines Morales",    "3813524349")),
        ["utn tucuman"] = D("UTN - Facultad Regional Tucumán",
            ("Lara Josefina Chauque", "3813396300"),
            ("Maite Ines Morales",    "3813524349")),

        ["universidad nacional de santiago del estero"] = D("Universidad Nacional de Santiago del Estero",
            ("Nicolas Agustin Sarubi",                  "2213104696"),
            ("Augusto Miguel Angel Robles Cameranesi",  "3854753078")),
        ["unse"] = D("Universidad Nacional de Santiago del Estero",
            ("Nicolas Agustin Sarubi",                  "2213104696"),
            ("Augusto Miguel Angel Robles Cameranesi",  "3854753078")),

        ["universidad nacional de misiones"] = D("Universidad Nacional de Misiones",
            ("Estefania Noemi Hundt",  "3751592942"),
            ("Nauhel Damian Posnik",   "3755542707")),
        ["un misiones"] = D("Universidad Nacional de Misiones",
            ("Estefania Noemi Hundt",  "3751592942"),
            ("Nauhel Damian Posnik",   "3755542707")),

        ["universidad nacional de salta"] = D("Universidad Nacional de Salta",
            ("Luis Fernando Alejandro Barrios", "3874464850"),
            ("Gimena Alvarez Matthews",          "3875142818")),
        ["un salta"] = D("Universidad Nacional de Salta",
            ("Luis Fernando Alejandro Barrios", "3874464850"),
            ("Gimena Alvarez Matthews",          "3875142818")),

        ["universidad nacional de formosa"] = D("Universidad Nacional de Formosa",
            ("Joaquín Antonio Rolon", "3718443595"),
            ("Adrian Ezequiel Paz",   "3704721155")),
        ["un formosa"] = D("Universidad Nacional de Formosa",
            ("Joaquín Antonio Rolon", "3718443595"),
            ("Adrian Ezequiel Paz",   "3704721155")),

        ["universidad católica de salta"] = D("Universidad Católica de Salta",
            ("Daniel Agustín Martínez", "3875518269"),
            ("Alejandra Mariana Sulca", "3874812466")),
        ["universidad catolica de salta"] = D("Universidad Católica de Salta",
            ("Daniel Agustín Martínez", "3875518269"),
            ("Alejandra Mariana Sulca", "3874812466")),
        ["ucasal"] = D("Universidad Católica de Salta",
            ("Daniel Agustín Martínez", "3875518269"),
            ("Alejandra Mariana Sulca", "3874812466")),

        // ── REGIÓN OESTE ─────────────────────────────────────────────────────

        ["utn - facultad regional córdoba"] = D("UTN - Facultad Regional Córdoba",
            ("Julieta Anahí Listello", "3573430566")),
        ["utn - facultad regional cordoba"] = D("UTN - Facultad Regional Córdoba",
            ("Julieta Anahí Listello", "3573430566")),
        ["utn córdoba"] = D("UTN - Facultad Regional Córdoba",
            ("Julieta Anahí Listello", "3573430566")),
        ["utn cordoba"] = D("UTN - Facultad Regional Córdoba",
            ("Julieta Anahí Listello", "3573430566")),

        ["universidad nacional de córdoba"] = D("Universidad Nacional de Córdoba",
            ("Sofia Abigail Bima Leon", "3516402001")),
        ["universidad nacional de cordoba"] = D("Universidad Nacional de Córdoba",
            ("Sofia Abigail Bima Leon", "3516402001")),
        ["un córdoba"] = D("Universidad Nacional de Córdoba",
            ("Sofia Abigail Bima Leon", "3516402001")),
        ["un cordoba"] = D("Universidad Nacional de Córdoba",
            ("Sofia Abigail Bima Leon", "3516402001")),
        ["unc"] = D("Universidad Nacional de Córdoba",
            ("Sofia Abigail Bima Leon", "3516402001")),

        ["universidad nacional de san juan"] = D("Universidad Nacional de San Juan",
            ("Julián Andrés Arévalo",          "2644697830"),
            ("Franco Damian Aguilera Aballay", "2645868465")),
        ["un san juan"] = D("Universidad Nacional de San Juan",
            ("Julián Andrés Arévalo",          "2644697830"),
            ("Franco Damian Aguilera Aballay", "2645868465")),
        ["unsj"] = D("Universidad Nacional de San Juan",
            ("Julián Andrés Arévalo",          "2644697830"),
            ("Franco Damian Aguilera Aballay", "2645868465")),

        ["universidad nacional de la rioja"] = D("Universidad Nacional de La Rioja",
            ("Franco Jose Avila", "3804618170")),
        ["un la rioja"] = D("Universidad Nacional de La Rioja",
            ("Franco Jose Avila", "3804618170")),
        ["unlar"] = D("Universidad Nacional de La Rioja",
            ("Franco Jose Avila", "3804618170")),

        ["utn - facultad regional la rioja"] = D("UTN - Facultad Regional La Rioja",
            ("Pablo Gaspar Carrizo",       "3837691881"),
            ("Pablo Samuel Carrizo Torres","3804862473")),
        ["utn la rioja"] = D("UTN - Facultad Regional La Rioja",
            ("Pablo Gaspar Carrizo",       "3837691881"),
            ("Pablo Samuel Carrizo Torres","3804862473")),

        ["universidad nacional de cuyo"] = D("Universidad Nacional de Cuyo",
            ("Martina Nerea Almirán Tittarelli", "2613622661"),
            ("Agustina Ailen Gallegos",           "2612574760")),
        ["un cuyo"] = D("Universidad Nacional de Cuyo",
            ("Martina Nerea Almirán Tittarelli", "2613622661"),
            ("Agustina Ailen Gallegos",           "2612574760")),
        ["uncuyo"] = D("Universidad Nacional de Cuyo",
            ("Martina Nerea Almirán Tittarelli", "2613622661"),
            ("Agustina Ailen Gallegos",           "2612574760")),

        ["utn - facultad regional san rafael"] = D("UTN - Facultad Regional San Rafael",
            ("Pablo Andres Perez",          "2604356041"),
            ("Antonella María Frana Bisang","2604600707")),
        ["utn san rafael"] = D("UTN - Facultad Regional San Rafael",
            ("Pablo Andres Perez",          "2604356041"),
            ("Antonella María Frana Bisang","2604600707")),

        // UTN Mendoza no tiene delegados propios → cubre Julieta Listello (isRegionalFallback de Oeste)
        ["utn - facultad regional mendoza"] = D("UTN - Facultad Regional Mendoza",
            ("Julieta Anahí Listello", "3573430566")),
        ["utn mendoza"] = D("UTN - Facultad Regional Mendoza",
            ("Julieta Anahí Listello", "3573430566")),

        ["universidad católica de córdoba"] = D("Universidad Católica de Córdoba",
            ("Fernando Cabrera", "2657637553")),
        ["universidad catolica de cordoba"] = D("Universidad Católica de Córdoba",
            ("Fernando Cabrera", "2657637553")),
        ["ucc"] = D("Universidad Católica de Córdoba",
            ("Fernando Cabrera", "2657637553")),

        // ── REGIÓN SUR ────────────────────────────────────────────────────────

        ["universidad nacional de la patagonia san juan bosco - sede comodoro rivadavia"] =
            D("UNPSJB - Sede Comodoro Rivadavia",
              ("Jeronimo Ferro Perea",      "2974293820"),
              ("Sergio Alejandro Gallardo", "2974358699")),
        ["unpsjb cr"] = D("UNPSJB - Sede Comodoro Rivadavia",
            ("Jeronimo Ferro Perea",      "2974293820"),
            ("Sergio Alejandro Gallardo", "2974358699")),
        ["unpsjb comodoro rivadavia"] = D("UNPSJB - Sede Comodoro Rivadavia",
            ("Jeronimo Ferro Perea",      "2974293820"),
            ("Sergio Alejandro Gallardo", "2974358699")),

        ["universidad nacional de la patagonia san juan bosco - sede trelew"] =
            D("UNPSJB - Sede Trelew",
              ("Cristian Santiago Schlund Cari", "2804005635"),
              ("Joaquín Soriano Rodriguez",       "2804569523")),
        ["unpsjb trelew"] = D("UNPSJB - Sede Trelew",
            ("Cristian Santiago Schlund Cari", "2804005635"),
            ("Joaquín Soriano Rodriguez",       "2804569523")),

        ["universidad nacional del comahue"] = D("Universidad Nacional del Comahue",
            ("Santiago Rodriguez Bilej", "3516456091")),
        ["un comahue"] = D("Universidad Nacional del Comahue",
            ("Santiago Rodriguez Bilej", "3516456091")),

        ["universidad nacional del centro de la provincia de buenos aires - sede olavarría"] =
            D("UNICEN - Sede Olavarría",
              ("Biran David Niz",     "2284516065"),
              ("Nahuel Nicolas Diaz", "2284514141")),
        ["universidad nacional del centro de la provincia de buenos aires - sede olavarria"] =
            D("UNICEN - Sede Olavarría",
              ("Biran David Niz",     "2284516065"),
              ("Nahuel Nicolas Diaz", "2284514141")),
        ["unicen"] = D("UNICEN - Sede Olavarría",
            ("Biran David Niz",     "2284516065"),
            ("Nahuel Nicolas Diaz", "2284514141")),

        ["universidad nacional del sur"] = D("Universidad Nacional del Sur",
            ("Emiliano David Herrera", "2932417721"),
            ("Florencia Hernandez",    "2984826474")),
        ["un sur"] = D("Universidad Nacional del Sur",
            ("Emiliano David Herrera", "2932417721"),
            ("Florencia Hernandez",    "2984826474")),
        ["uns"] = D("Universidad Nacional del Sur",
            ("Emiliano David Herrera", "2932417721"),
            ("Florencia Hernandez",    "2984826474")),

        ["utn - facultad regional bahía blanca"] = D("UTN - Facultad Regional Bahía Blanca",
            ("Angel Adrian De León",      "2914992004"),
            ("Agustina Ailén Manganelli", "2914727578")),
        ["utn - facultad regional bahia blanca"] = D("UTN - Facultad Regional Bahía Blanca",
            ("Angel Adrian De León",      "2914992004"),
            ("Agustina Ailén Manganelli", "2914727578")),
        ["utn bahía blanca"] = D("UTN - Facultad Regional Bahía Blanca",
            ("Angel Adrian De León",      "2914992004"),
            ("Agustina Ailén Manganelli", "2914727578")),
        ["utn bahia blanca"] = D("UTN - Facultad Regional Bahía Blanca",
            ("Angel Adrian De León",      "2914992004"),
            ("Agustina Ailén Manganelli", "2914727578")),
    };
}
