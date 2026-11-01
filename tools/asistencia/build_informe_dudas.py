exec(open('audit.py', encoding='utf-8').read())
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment

from openpyxl import load_workbook
QX = {}
for r_ in load_workbook('Datos Qcards.xlsx', data_only=True)['Listado Final'].iter_rows(min_row=2, values_only=True):
    if r_[0]:
        QX[int(re.sub(r'[^0-9]', '', str(r_[0])))] = str(r_[0])
def ql(i):
    cs = sorted(id2cards.get(i, []))
    return ' / '.join(QX.get(k, str(k)) for k in cs) if cs else 'sin tarjeta'
P = [i for i in records if i not in com]
rec_flags = collections.defaultdict(list)
for f in flags:
    rec_flags[f['id']].append(f)
risky = {i: {f['sid'] for f in fl if f['sid'] and f['level'] in ('Alta', 'Media')} for i, fl in rec_flags.items()}
LV = {'Alta': 0, 'Media': 1, 'Baja': 2}

VIS = [(cd, sid) for sid, (t, cd) in SINFO.items() if t == 'Visita técnica']
vis_rows = []
for cd, sid in sorted(VIS):
    en = enrolled_by_code[(1, cd)]
    pres = {i for i in en if sid in records.get(i, set())}
    title = c.execute("select Title from SelectableActivities where Code=? and BlockId=1", (cd,)).fetchone()[0]
    ausentes = sorted((R[i]['lastname'] + ' ' + R[i]['name']) for i in en - pres)
    vis_rows.append([cd, title, len(en), len(pres), len(en) - len(pres), '; '.join(ausentes)])

def is_sofia(i):
    return R[i]['lastname'] == 'Cabrera' and R[i]['name'].startswith('Leonardo')

rows = []
for i in P:
    recs = records[i]
    exp, ok, e_all = eligible(i, recs)
    _, ok_s, e_safe = eligible(i, recs - risky.get(i, set()))
    n = len(recs)
    eff = min(ok + 1, exp)
    just = (not e_all) and exp > 0 and eff * 100 >= exp * 75
    if n == 0:
        cat = 'Sin ninguna asistencia'
    elif e_all and e_safe:
        cat = 'Cumplen (registros seguros)'
    elif e_all:
        cat = 'Cumplen solo con registros dudosos'
    elif just:
        cat = 'Cumplen con 1 falta justificada'
    else:
        cat = 'No cumplen'
    fl = rec_flags.get(i, [])
    nd = len([f for f in fl if f['level'] in ('Alta', 'Media')])
    rows.append(dict(id=i, cat=cat, exp=exp, ok=ok, pct=(ok / exp if exp else 0), pct_safe=(ok_s / exp if exp else 0), nd=nd,
                     note='; '.join(sorted({f['reason'].split(' — ')[0].split(':')[0] for f in fl if f['level'] in ('Alta', 'Media')})) + (' | CASO REPORTADO POR SOFÍA' if is_sofia(i) else '')))
cnt = collections.Counter(r['cat'] for r in rows)
print(cnt)

wb = Workbook()
def hdr(ws, fill):
    for c_ in ws[1]:
        c_.font = Font(bold=True, color='FFFFFF')
        c_.fill = PatternFill('solid', start_color=fill)
        c_.alignment = Alignment(wrap_text=True, vertical='center')
def widths(ws, mx=45):
    for col in ws.columns:
        ws.column_dimensions[col[0].column_letter].width = min(mx, max(10, max(len(str(x.value or '')) for x in col[:80]) + 2))

ws = wb.active
ws.title = 'Resumen'
na = sum(1 for f in flags if f['level'] == 'Alta'); nm = sum(1 for f in flags if f['level'] == 'Media'); nb = sum(1 for f in flags if f['level'] == 'Baja')
lines = [
    ("Revisión de asistencias: qué es seguro y qué es dudoso",), (),
    ("Personas (sin Comité Organizador)", len(P)),
    ("Cumplen 75% con registros seguros", cnt['Cumplen (registros seguros)']),
    ("Cumplen 75% pero DEPENDEN de registros dudosos (revisar antes de emitir)", cnt['Cumplen solo con registros dudosos']),
    ("Cumplen con 1 falta justificada (regla del error de toma)", cnt['Cumplen con 1 falta justificada']),
    ("No cumplen", cnt['No cumplen']),
    ("Sin ninguna asistencia", cnt['Sin ninguna asistencia']), (),
    ("Registros marcados Alta (muy probable error)", na),
    ("Registros marcados Media (a confirmar)", nm),
    ("Registros marcados Baja (informativos: inferidos / cargados por pedido / dos tarjetas)", nb), (),
    ("Cómo leerlo",),
    ("- 'Seguras' = el cumplimiento se mantiene aunque se descarten los registros marcados Alta o Media.",),
    ("- Alta: actividad a la que no estaba inscripto con un número parecido que encaja (posible lectura errónea), o dos tomas del mismo tipo (no puede estar en dos a la vez).",),
    ("- Media: presente en una actividad del mismo tipo distinta a la inscripta (cambio de último momento posible) o número leído con dudas en la foto.",),
    ("- Baja: acreditación inferida, cargas por pedido expreso, personas con dos tarjetas.",),
    ("- Las visitas técnicas se cargaron por descarte (ausentes informados por la coordinación): ver hoja 'Visitas (por descarte)'. No hay lista nominal para confirmarlas una por una.",),
    ("- Ningún registro fue modificado: este archivo solo marca. 'Asistencias dudosas' trae la posible corrección cuando hay un número parecido que encaja.",),
]
for l in lines:
    ws.append(list(l))
ws['A1'].font = Font(bold=True, size=14)
ws.column_dimensions['A'].width = 120
ws.column_dimensions['B'].width = 12

wd = wb.create_sheet('Asistencias dudosas')
wd.append(['Nivel', 'Qcard', 'Apellido', 'Nombre', 'Delegación', 'Toma', 'Motivo', 'Tarjeta leída', 'Posible corrección (persona / tarjeta)', '¿Su cumplimiento depende de este registro?', 'Nota'])
flagged = sorted(flags, key=lambda f: (LV[f['level']], R[f['id']]['lastname'].lower()))
for f in flagged:
    i = f['id']
    dep = ''
    if f['sid'] is not None:
        _, _, e1 = eligible(i, records[i])
        _, _, e2 = eligible(i, records[i] - {f['sid']})
        dep = 'Sí' if e1 and not e2 else 'No'
    wd.append([f['level'], ql(i), R[i]['lastname'], R[i]['name'], R[i]['faculty'], SN[f['sid']] if f['sid'] else '(persona)', f['reason'],
               f['card'] or '', f['cand'], dep, 'CASO REPORTADO POR SOFÍA' if is_sofia(i) else ''])
hdr(wd, 'B42318'); wd.freeze_panes = 'C2'; wd.auto_filter.ref = wd.dimensions; widths(wd, 60)

def personsheet(title, cat, fill):
    w = wb.create_sheet(title)
    w.append(['Qcard', 'Apellido', 'Nombre', 'DNI', 'Delegación', 'Email', 'Tomas esperadas', 'Tomas asistidas', '% asistencia', '% solo registros seguros', 'Registros dudosos', 'Detalle de dudas'])
    for r in sorted([r for r in rows if r['cat'] == cat], key=lambda r: (R[r['id']]['lastname'].lower(), R[r['id']]['name'].lower())):
        i = r['id']
        w.append([ql(i), R[i]['lastname'], R[i]['name'], R[i]['dni'], R[i]['faculty'], R[i]['email'], r['exp'], r['ok'], r['pct'], r['pct_safe'], r['nd'], r['note']])
    hdr(w, fill)
    for rr in w.iter_rows(min_row=2, min_col=9, max_col=10):
        for x in rr:
            x.number_format = '0.0%'
    w.freeze_panes = 'C2'; w.auto_filter.ref = w.dimensions; widths(w)

personsheet('Cumplen seguras - %d' % cnt['Cumplen (registros seguros)'], 'Cumplen (registros seguros)', '1B7F4B')
personsheet('Cumplen con dudosas - %d' % cnt['Cumplen solo con registros dudosos'], 'Cumplen solo con registros dudosos', 'B7791F')
personsheet('Cumplen con 1 falta - %d' % cnt['Cumplen con 1 falta justificada'], 'Cumplen con 1 falta justificada', 'D97706')
personsheet('No cumplen - %d' % cnt['No cumplen'], 'No cumplen', 'B42318')
personsheet('Sin ninguna asistencia - %d' % cnt['Sin ninguna asistencia'], 'Sin ninguna asistencia', '667085')

wv = wb.create_sheet('Visitas (por descarte)')
wv.append(['Visita', 'Nombre', 'Inscriptos confirmados', 'Presentes cargados', 'Ausentes', 'Ausentes (informados)'])
for r in vis_rows:
    wv.append(r)
hdr(wv, '0F4C81'); wv.freeze_panes = 'A2'; widths(wv, 70)

out = r'Asistencia_final_con_dudas.xlsx'
wb.save(out)
print(len(flagged), 'flags; Alta', na, 'Media', nm, 'Baja', nb)
print('personas con >=1 registro dudoso:', sum(1 for r in rows if r['nd'] > 0))
