import json, re, sqlite3, collections, itertools
rx=re.compile(r'^(Taller|Simultánea|Solidaria|Visita técnica) (\d\.\d{2}) ')
BLK={'Taller':2,'Simultánea':3,'Solidaria':4,'Visita técnica':1}
TYPES=list(BLK)
m=json.load(open('matrix_now.json',encoding='utf-8'))
cm=json.load(open('qcard_map.json',encoding='utf-8'))
card2id={int(k):v for k,v in cm.items()}
id2cards=collections.defaultdict(list)
for k,v in card2id.items(): id2cards[v].append(k)
c=sqlite3.connect('file:../ro.db?mode=ro',uri=True)
R={r[0]:dict(id=r[0],lastname=r[1],name=r[2],faculty=r[3],email=r[4].lower(),dni=r[5],status=r[6]) for r in c.execute("select Id,Lastname,Name,Faculty,Email,Dni,Status from Registrations")}
codeOf={r[0]:(r[1],r[2]) for r in c.execute("select Id,Code,BlockId from SelectableActivities")}
sel=collections.defaultdict(lambda: collections.defaultdict(list))
for e,b,a,cf in c.execute("select UserEmail,BlockId,ActivityId,IsConfirmed from ActivitySelections"):
    if a in codeOf and 1<=b<=4 and (b!=1 or cf): sel[e.lower()][b].append(codeOf[a][0])
enrolled_by_code=collections.defaultdict(set)   # (block,code)->ids
for i,r in R.items():
    for b,codes in sel[r['email']].items():
        for cd in codes: enrolled_by_code[(b,cd)].add(i)
SN={s['id']:s['name'] for s in m['sessions']}
SINFO={}
for sid,n in SN.items():
    mm=rx.match(n); SINFO[sid]=(mm.group(1),mm.group(2)) if mm else ('Fija',n)
SID_BY_CODE={v[1]:k for k,v in SINFO.items() if v[0]!='Fija'}
FIXED=[sid for sid,v in SINFO.items() if v[0]=='Fija']
records={p['id']:set(p['sessions']) for p in m['people']}
people={p['id']:p for p in m['people']}
def eligible(i,recs):
    r=R[i]; exp=0; ok=0
    for sid in FIXED:
        exp+=1; ok+= sid in recs
    for t in TYPES:
        ch=sel[r['email']].get(BLK[t],[])
        if not ch: continue
        own=SID_BY_CODE.get(ch[0]); same=[s for s,v in SINFO.items() if v[0]==t]
        exp+=1; ok+= (own in recs) or any(s in recs for s in same)
    return exp,ok,(exp>0 and ok*100>=exp*75)

com={i for i,r in R.items() if 'comit' in (r['faculty'] or '').lower()}
# ---------- flags ----------
CONF={('1','7'),('7','1'),('2','7'),('7','2'),('3','8'),('8','3'),('5','6'),('6','5'),('5','8'),('8','5'),('6','0'),('0','6'),('6','8'),('8','6'),('0','8'),('8','0'),('4','9'),('9','4'),('9','7'),('7','9'),('2','5'),('5','2'),('3','5'),('5','3'),('0','9'),('9','0'),('1','4'),('4','1'),('1','2'),('2','1')}
def variants(card):
    s='%03d'%card
    for pos in range(3):
        for d in '0123456789':
            if d!=s[pos]:
                v=int(s[:pos]+d+s[pos+1:])
                if 1<=v<=767: yield v,((s[pos],d) in CONF)
    for a,b in ((0,1),(1,2)):
        t=list(s); t[a],t[b]=t[b],t[a]; v=int(''.join(t))
        if v!=card and 1<=v<=767: yield v,True
UNCERTAIN={}  # {código de toma: {tarjetas leídas con dudas}}
UNCERTAIN_FIXED={}
MANUAL_ADD=set()  # (código de toma, tarjeta) cargados por pedido expreso  # pedido expreso (ayudaron a UCA)
# Acreditación inferida (sin tilde pero presentes en otras tomas)
UNCH_CARDS=[]  # tarjetas sin tilde en la hoja de acreditación (dato personal: no incluido en el repo)
unch_ids={card2id[k] for k in UNCH_CARDS}
ACR=next(s for s,v in SINFO.items() if v[1]=='Martes 13 · Acreditación')
flags=[]   # dict per record
def add(i,sid,level,reason,card=None,cand=None):
    flags.append(dict(id=i,sid=sid,level=level,reason=reason,card=card,cand=cand or ''))
for i,recs in records.items():
    if i in com: continue
    r=R[i]
    byType=collections.defaultdict(list)
    for sid in recs:
        t,cd=SINFO[sid]
        if t!='Fija': byType[t].append(sid)
    for t,sids in byType.items():
        ch=sel[r['email']].get(BLK[t],[])
        for sid in sids:
            cd=SINFO[sid][1]
            cards=id2cards.get(i,[]); card=cards[0] if cards else None
            if (cd,i) in {(a,card2id.get(b)) for a,b in MANUAL_ADD}: 
                add(i,sid,'Baja','Cargada por pedido expreso (colaboró en UCA), no por planilla',card); continue
            enrolled_here=(i in enrolled_by_code[(BLK[t],cd)])
            if not enrolled_here:
                # candidatos de lectura errónea
                cands=[]
                for cc in cards:
                    for v,likely in variants(cc):
                        j=card2id.get(v)
                        if j and j!=i and j in enrolled_by_code[(BLK[t],cd)] and sid not in records.get(j,set()) and j not in com:
                            cands.append((likely,'%s %s (tarjeta %d)'%(R[j]['lastname'],R[j]['name'],v)))
                lk=[x[1] for x in cands if x[0]]; ot=[x[1] for x in cands if not x[0]]
                txt='; '.join((lk+ot)[:3])
                inscripto='inscripto en '+(', '.join(ch) or 'ninguna')+' ('+t+')'
                if t=='Visita técnica':
                    lvl='Alta'; why='Visita técnica a la que NO estaba inscripto/confirmado — figura '+inscripto
                elif len(sids)>1:
                    lvl='Alta'; why='No inscripto en esta actividad y además tiene otra toma del mismo tipo — '+inscripto
                else:
                    lvl='Media'; why='Presente en '+t.lower()+' distinto al inscripto — '+inscripto
                if lk: lvl='Alta'; why+='; posible lectura errónea del número'
                add(i,sid,lvl,why,card,txt)
            else:
                if len(sids)>1 and sid!=SID_BY_CODE.get(ch[0] if ch else None):
                    add(i,sid,'Media','Dos tomas del mismo tipo (además de la inscripta)',card)
            cards=id2cards.get(i,[])
            if any(cc in UNCERTAIN.get(cd,set()) for cc in cards):
                add(i,sid,'Media','Número leído con dudas al transcribir la foto (letra poco clara)',card)
    for sid in recs:
        t,n=SINFO[sid]
        if t=='Fija':
            cards=id2cards.get(i,[])
            if n=='Martes 13 · Acreditación' and i in unch_ids:
                add(i,sid,'Baja','Acreditación inferida: sin tilde en la hoja pero presente en otras tomas',cards[0] if cards else None)
            if any(cc in UNCERTAIN_FIXED.get(n,set()) for cc in cards):
                add(i,sid,'Media','Número leído con dudas al transcribir la foto',cards[0] if cards else None)
    if len(id2cards.get(i,[]))>1:
        add(i,None,'Baja','Persona con dos tarjetas (%s): revisar a cuál corresponde cada marca'%', '.join(map(str,id2cards[i])))
