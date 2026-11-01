# Asistencia y certificados — CONEIC 2026

Este documento resume cómo funciona el módulo de asistencia, para poder reutilizarlo o auditarlo después de dar de baja la infraestructura.
El código vive en este repo (backend) y en `CONEIC-2026-FE` (panel de administración, pestaña "Asistencia").

## Modelo de datos
- `AttendanceSessions(Id, Name, CreatedAt)`: una toma de asistencia.
- `AttendanceRecords(Id, RegistrationId, SessionId, CheckedInAt, CheckedInBy)`, único por (`RegistrationId`, `SessionId`).
- Convención de nombres de tomas:
  - Fijas: `Martes 13 · Acreditación | Salida a almuerzo | Vuelta de almuerzo | Fin de jornada` y `Viernes 16 · Inicio de mañana | Salida a almuerzo | Vuelta de almuerzo`.
  - Por actividad: `{Taller|Simultánea|Solidaria|Visita técnica} {código} · {título[:70]}` (ej. `Taller 1.07 · …`).

## Endpoints (`Controllers/AttendanceController.cs`, solo staff: rol admin/tesoreria)
| Método | Ruta | Uso |
|---|---|---|
| GET/POST | `/api/attendance/sessions` | listar / crear tomas |
| DELETE | `/api/attendance/sessions/{id}` | borrar una toma (solo si está vacía) |
| POST | `/api/attendance/bulk-checkin` | `{sessionId, codes[], checkedInBy}`: cargar presentes por ID de inscripción (idempotente; usar tandas de ≤250 y de a una) |
| DELETE | `/api/attendance/checkin` | quitar un presente (`registrationId`, `sessionId`) |
| GET | `/api/attendance/matrix` | personas pagas con sus tomas e inscripciones |
| GET | `/api/attendance/certificates` y `/certificates/export` | cálculo de certificados (JSON / Excel) |
| GET | `/api/attendance/mine` | asistencia propia (solo staff) |

## Criterio de certificado (`Services/AttendanceCertificates.cs`)
- Solo inscriptos con pago confirmado (`Paid`).
- Tomas esperadas por persona: las 7 fijas + una por cada tipo en el que esté inscripto (Taller, Simultánea, Solidaria; Visita técnica solo si está confirmada).
- En cada tipo cuenta estar presente en la toma de su actividad **o en cualquier otra del mismo tipo**.
- Cumple con `presentes * 100 >= esperadas * 75`.

## Números de Qcard (ojo)
Las planillas a mano usan el **número de Qcard** (con prefijo A/D opcional), que **no es** el ID de inscripción de la web.
La equivalencia vino del Excel "Datos Qcards.xlsx" (hoja "Listado Final"), cruzado por apellido y nombre con `Registrations`. Las tarjetas 768–771 son presentadores sin inscripción.
Ese archivo y el mapeo contienen datos personales: **no están en el repo**.

## Cómo se cargó (flujo usado)
1. Fotos/planillas → transcribir los números → mapear Qcard → ID de inscripción.
2. Validar contra inscripciones: la planilla de una actividad debería coincidir casi toda con sus inscriptos (así se detectan planillas rotuladas mal).
3. `bulk-checkin` por toma, en forma **secuencial**.
4. Visitas técnicas: se cargaron en general por descarte (todos los confirmados menos los ausentes informados por la coordinación).

## Revisión de dudas (`tools/asistencia/`)
- `audit.py`: marca registros dudosos (actividad a la que no estaba inscripto, dos tomas del mismo tipo, números parecidos que encajan, números leídos con dudas) y replica la regla de certificados.
- `build_informe_dudas.py`: genera el Excel final (cumplen con registros seguros / dependen de dudosos / con una falta justificada / no cumplen / sin asistencia).
- Entradas (no incluidas por tener datos personales): copia de la base `coneic.db`, `matrix_now.json` (respuesta de `/api/attendance/matrix`) y `qcard_map.json` (Qcard → ID).

## Operación en Azure (histórico)
- SQLite sobre un share de Azure Files montado en `/home/coneic-data` (CIFS): **no hacer escrituras en paralelo** (se corrompió un índice una vez).
- App Service con plan B1 requería `DOTNET_TieredPGO=0` y `DOTNET_ReadyToRun=0`.
- Despliegue manual por Kudu `zipdeploy`; el front se publicaba con `git push origin dev:main`.
