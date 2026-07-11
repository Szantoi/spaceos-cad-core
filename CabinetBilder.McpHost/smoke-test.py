#!/usr/bin/env python3
"""MCP stdio smoke-teszt a CabinetBilder.McpHost-hoz (TASK-007).
Elinditja a hostot, lejatssza a JSON-RPC kezfogast + tool-hivasokat,
kiirja a valaszokat. Fix skeletonId-t hasznal, hogy a lanc kovetheto legyen.
"""
import json, subprocess, sys, threading, time, os

DLL = os.path.join(os.path.dirname(__file__), "bin", "Debug", "net10.0", "cabinetbilder-mcphost.dll")
SID = "11111111-1111-1111-1111-111111111111"
SID2 = "22222222-2222-2222-2222-222222222222"
EXPORT_DIR = os.path.join(os.path.dirname(__file__), "bin", "smoke-export")

msgs = [
    {"jsonrpc":"2.0","id":1,"method":"initialize","params":{
        "protocolVersion":"2024-11-05","capabilities":{},
        "clientInfo":{"name":"smoke","version":"1.0"}}},
    {"jsonrpc":"2.0","method":"notifications/initialized"},
    {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}},
    {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"skeleton_create",
        "arguments":{"name":"Smoke Cabinet","skeletonId":SID,"intent":"Smoke-teszt elem"}}},
    {"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"skeleton_apply_parameter",
        "arguments":{"skeletonId":SID,"key":"Width","value":800,"intent":"Szelesites teszt"}}},
    {"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"skeleton_compute_bom",
        "arguments":{"skeletonId":SID}}},
    {"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"get_connection_status","arguments":{}}},
    {"jsonrpc":"2.0","id":7,"method":"tools/call","params":{"name":"get_store_stats","arguments":{}}},
    {"jsonrpc":"2.0","id":8,"method":"tools/call","params":{"name":"list_materials","arguments":{}}},
    {"jsonrpc":"2.0","id":9,"method":"tools/call","params":{"name":"skeleton_set_material",
        "arguments":{"skeletonId":SID,"target":"carcass","materialCode":"LAM18_SONOMA"}}},
    {"jsonrpc":"2.0","id":10,"method":"tools/call","params":{"name":"skeleton_compute_bom",
        "arguments":{"skeletonId":SID}}},
    {"jsonrpc":"2.0","id":11,"method":"tools/call","params":{"name":"skeleton_material_summary",
        "arguments":{"skeletonId":SID}}},
    {"jsonrpc":"2.0","id":12,"method":"tools/call","params":{"name":"skeleton_cutting_plan",
        "arguments":{"skeletonId":SID,"allowanceMm":10}}},
    {"jsonrpc":"2.0","id":13,"method":"tools/call","params":{"name":"skeleton_cutting_sheet",
        "arguments":{"skeletonId":SID,"allowanceMm":10}}},
    {"jsonrpc":"2.0","id":14,"method":"tools/call","params":{"name":"skeleton_set_material",
        "arguments":{"skeletonId":SID,"target":"edging","materialCode":"ABS2_SONOMA"}}},
    {"jsonrpc":"2.0","id":15,"method":"tools/call","params":{"name":"skeleton_material_summary",
        "arguments":{"skeletonId":SID}}},
    {"jsonrpc":"2.0","id":16,"method":"tools/call","params":{"name":"skeleton_cost_calculation",
        "arguments":{"skeletonId":SID,"laborHours":16,"hourlyRate":5000}}},
    {"jsonrpc":"2.0","id":17,"method":"tools/call","params":{"name":"skeleton_submit_cutting_sheet",
        "arguments":{"skeletonId":SID,"allowanceMm":10}}},
    {"jsonrpc":"2.0","id":18,"method":"tools/call","params":{"name":"record_design_intent",
        "arguments":{"skeletonId":SID,"intent":"Sonoma korpusz a nappali szinvilagahoz","parameterKey":"CarcassMaterialId"}}},
    {"jsonrpc":"2.0","id":19,"method":"tools/call","params":{"name":"skeleton_technical_description",
        "arguments":{"skeletonId":SID}}},
    {"jsonrpc":"2.0","id":20,"method":"tools/call","params":{"name":"skeleton_export_project",
        "arguments":{"skeletonId":SID,"outputDir":EXPORT_DIR,"dsmr":"26144","allowanceMm":10,"laborHours":16}}},
    {"jsonrpc":"2.0","id":21,"method":"tools/call","params":{"name":"skeleton_labor_estimate",
        "arguments":{"skeletonId":SID}}},
    {"jsonrpc":"2.0","id":22,"method":"tools/call","params":{"name":"skeleton_cost_calculation",
        "arguments":{"skeletonId":SID,"laborHours":-1,"hourlyRate":5000}}},
    {"jsonrpc":"2.0","id":23,"method":"tools/call","params":{"name":"skeleton_production_schedule",
        "arguments":{"skeletonId":SID}}},
    {"jsonrpc":"2.0","id":24,"method":"tools/call","params":{"name":"skeleton_create",
        "arguments":{"name":"Smoke Cabinet 2","skeletonId":SID2}}},
    {"jsonrpc":"2.0","id":25,"method":"tools/call","params":{"name":"skeleton_schedule_projects",
        "arguments":{"skeletonIds":[SID,SID2],"startDate":"2026-07-17T08:00:00","asztalosCount":1,"cncCount":1,"osszeszereloCount":1}}},
]

proc = subprocess.Popen(["dotnet", DLL], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                        stderr=subprocess.DEVNULL, text=True, encoding="utf-8", bufsize=1)

responses = {}
def reader():
    for line in proc.stdout:
        line = line.strip()
        if not line:
            continue
        try:
            obj = json.loads(line)
        except Exception:
            continue
        if "id" in obj:
            responses[obj["id"]] = obj

t = threading.Thread(target=reader, daemon=True)
t.start()

for m in msgs:
    proc.stdin.write(json.dumps(m) + "\n")
    proc.stdin.flush()
    time.sleep(0.4)

time.sleep(2.0)
proc.stdin.close()
try:
    proc.wait(timeout=5)
except subprocess.TimeoutExpired:
    proc.kill()

def content_json(rid):
    r = responses.get(rid)
    if not r or "result" not in r:
        return None
    c = r["result"].get("content", [])
    if c and c[0].get("type") == "text":
        try:
            return json.loads(c[0]["text"])
        except Exception:
            return c[0]["text"]
    return r["result"]

ok = True
# 1) initialize
init = responses.get(1)
print("initialize:", "OK" if init and "result" in init else "FAIL")
ok &= bool(init and "result" in init)

# 2) tools/list
tl = responses.get(2)
tools = [t["name"] for t in tl["result"]["tools"]] if tl and "result" in tl else []
print("tools/list:", sorted(tools))
expected = {"ping","skeleton_create","skeleton_apply_parameter","skeleton_compute_bom",
            "record_design_intent","get_store_stats","get_connection_status",
            "list_materials","list_templates","skeleton_set_material","skeleton_material_summary",
            "skeleton_cutting_plan","skeleton_cutting_sheet",
            "skeleton_cost_calculation","skeleton_submit_cutting_sheet",
            "skeleton_technical_description","skeleton_export_project",
            "skeleton_labor_estimate","skeleton_production_schedule","skeleton_schedule_projects"}
missing = expected - set(tools)
print("  hianyzo toolok:", missing if missing else "nincs")
ok &= not missing

# 3) create
cr = content_json(3)
print("skeleton_create isSuccess:", cr.get("isSuccess") if isinstance(cr,dict) else cr)
ok &= isinstance(cr,dict) and cr.get("isSuccess") is True

# 4) apply_parameter -> Width=800
ap = content_json(4)
width = None
if isinstance(ap,dict) and ap.get("value"):
    for p in ap["value"].get("parameters",[]):
        if p.get("key")=="Width": width = p.get("value")
print("apply_parameter Width ->", width, "(elvart: 800)")
ok &= (width == 800 or width == 800.0)

# 5) compute_bom -> 5 komponens, valós materialId + materialName
bom = content_json(5)
lines = bom.get("value",[]) if isinstance(bom,dict) else []
print("compute_bom sorok szama:", len(lines))
names = [l.get("name") for l in lines]
print("  komponensek:", names)
ok &= len(lines) == 5
side = next((l for l in lines if l.get("name")=="Side Left"), {})
back = next((l for l in lines if l.get("name")=="Back"), {})
print("  Side Left anyag:", side.get("materialId"), "/", side.get("materialName"), "/ felület:", side.get("surface"))
print("  Back anyag:", back.get("materialId"), "/", back.get("materialName"), "/ felület:", back.get("surface"))
ok &= side.get("materialId") == "LAM18_W1000" and side.get("materialName") is not None
ok &= back.get("materialId") == "HDF3_WHITE"
ok &= side.get("surface") == "laminált" and back.get("surface") == "hdf hátlap"

# 6) connection status
cs = content_json(6)
print("get_connection_status:", cs.get("value",{}).get("status") if isinstance(cs,dict) else cs)
ok &= isinstance(cs,dict) and cs.get("isSuccess") is True

# 7) store stats
ss = content_json(7)
print("get_store_stats isSuccess:", ss.get("isSuccess") if isinstance(ss,dict) else ss)
ok &= isinstance(ss,dict) and ss.get("isSuccess") is True

# 8) list_materials -> interim katalógus (>=5)
lm = content_json(8)
mats = lm.get("value",[]) if isinstance(lm,dict) else []
codes = [m.get("materialCode") for m in mats]
print("list_materials:", codes)
ok &= "LAM18_W1000" in codes and "HDF3_WHITE" in codes and len(mats) >= 5

# 9) set_material carcass -> LAM18_SONOMA
sm = content_json(9)
print("set_material isSuccess:", sm.get("isSuccess") if isinstance(sm,dict) else sm)
ok &= isinstance(sm,dict) and sm.get("isSuccess") is True

# 10) compute_bom újra -> a Side sorok most SONOMA
bom2 = content_json(10)
lines2 = bom2.get("value",[]) if isinstance(bom2,dict) else []
side2 = next((l for l in lines2 if l.get("name")=="Side Left"), {})
back2 = next((l for l in lines2 if l.get("name")=="Back"), {})
print("  set_material után Side Left:", side2.get("materialId"), "/", side2.get("materialName"))
print("  Back (változatlan):", back2.get("materialId"))
ok &= side2.get("materialId") == "LAM18_SONOMA"
ok &= back2.get("materialId") == "HDF3_WHITE"

# 11) material_summary -> anyagonkénti összesítés, felület + terület
ms = content_json(11)
val = ms.get("value",{}) if isinstance(ms,dict) else {}
sum_lines = val.get("lines",[])
print("material_summary anyagok:", [(l.get("materialId"), l.get("surface"), round(l.get("totalAreaM2",0),3)) for l in sum_lines])
print("  összterület m²:", round(val.get("totalAreaM2",0),3), "| becsült költség:", val.get("totalEstimatedCost"))
ok &= len(sum_lines) == 2  # SONOMA (korpusz) + HDF (hátlap)
ok &= val.get("totalAreaM2",0) > 0
ok &= all(l.get("surface") for l in sum_lines)

# 12) cutting_plan allowance=10 -> cutLength = finished + 20, tábla-becslés, rostirány
cp = content_json(12)
cpv = cp.get("value",{}) if isinstance(cp,dict) else {}
pcs = cpv.get("pieces",[])
p0 = pcs[0] if pcs else {}
print("cutting_plan allowance:", cpv.get("allowanceMm"), "| első tétel cut vs finished:",
      p0.get("cutLengthMm"), "vs", p0.get("finishedLengthMm"), "| grain:", p0.get("grain"))
bm = cpv.get("byMaterial",[])
print("  tábla-becslés:", [(m.get("materialId"), m.get("estimatedBoards")) for m in bm])
ok &= cpv.get("allowanceMm") == 10
ok &= p0.get("cutLengthMm") == p0.get("finishedLengthMm") + 20
ok &= len(bm) == 2 and all(m.get("estimatedBoards",0) >= 1 for m in bm)

# 13) cutting_sheet -> VPS payload sha256 + items cut méret, submitted=false
cs = content_json(13)
csv = cs.get("value",{}) if isinstance(cs,dict) else {}
pl = csv.get("payload",{})
meta = pl.get("metadata",{})
print("cutting_sheet items:", len(pl.get("items",[])), "| sha256 hossz:", len(meta.get("sha256","")),
      "| submitted:", csv.get("submitted"))
ok &= len(pl.get("items",[])) == 5
ok &= len(meta.get("sha256","")) == 64 and meta.get("source") == "CabinetBilder"
ok &= csv.get("submitted") is False

# 14) set_material edging -> ABS2_SONOMA
se = content_json(14)
print("set_material(edging) isSuccess:", se.get("isSuccess") if isinstance(se,dict) else se)
ok &= isinstance(se,dict) and se.get("isSuccess") is True

# 15) material_summary -> élzáró blokk (ABS2_SONOMA, 4 korpusz-panel)
ms2 = content_json(15)
v2 = ms2.get("value",{}) if isinstance(ms2,dict) else {}
edg = v2.get("edging",[])
print("material_summary élzáró:", [(e.get("edgingId"), e.get("pieceCount"), round(e.get("totalLengthM",0),2), e.get("estimatedCost")) for e in edg])
ok &= len(edg) == 1 and edg[0].get("edgingId") == "ABS2_SONOMA" and edg[0].get("pieceCount") == 4
ok &= edg[0].get("totalLengthM",0) > 0 and edg[0].get("estimatedCost") is not None

# 16) cost_calculation -> 11 lépés, bruttó = nettó * 1.27
cc = content_json(16)
cv = cc.get("value",{}) if isinstance(cc,dict) else {}
steps = cv.get("steps",[])
net = cv.get("netSellingPriceHuf"); gross = cv.get("grossSellingPriceHuf")
print("cost_calculation lépések:", len(steps), "| nettó:", net, "| bruttó:", gross)
ok &= len(steps) == 11
ok &= net is not None and gross is not None and abs(gross - net*1.27) < 1
ok &= net % 1000 == 0  # 1000-re kerekítve

# 17) submit_cutting_sheet -> outbox enqueue
sub = content_json(17)
sv = sub.get("value",{}) if isinstance(sub,dict) else {}
print("submit_cutting_sheet outboxEntryId:", (sv.get("outboxEntryId") or "")[:8], "| outboxPending:", sv.get("outboxPending"))
ok &= isinstance(sub,dict) and sub.get("isSuccess") is True
ok &= bool(sv.get("outboxEntryId")) and (sv.get("outboxPending") or 0) >= 1
ok &= len(sv.get("payloadSha256","")) == 64

# 18) record_design_intent
ri = content_json(18)
ok &= isinstance(ri,dict) and ri.get("isSuccess") is True

# 19) technical_description -> tankönyvi szekciók + intents + markdown
td = content_json(19)
tv = td.get("value",{}) if isinstance(td,dict) else {}
print("technical_description név:", tv.get("name"), "| méret:", tv.get("overallSizeMm"))
print("  anyagok:", [(m.get("role"), m.get("materialId")) for m in tv.get("materials",[])])
print("  szándékok:", len(tv.get("designIntents",[])), "| markdown hossz:", len(tv.get("markdown","")))
ok &= "800 × 720 × 560" in (tv.get("overallSizeMm") or "")  # Width=800 az apply_parameter után
roles = [m.get("role") for m in tv.get("materials",[])]
ok &= "Korpusz" in roles and "Hátlap" in roles and "Élzáró" in roles
mats = {m.get("role"): m.get("materialId") for m in tv.get("materials",[])}
ok &= mats.get("Korpusz") == "LAM18_SONOMA" and mats.get("Élzáró") == "ABS2_SONOMA"
ok &= len(tv.get("designIntents",[])) >= 3  # create-intent + apply-intent + record_design_intent
ok &= "## Felhasznált anyagok" in (tv.get("markdown") or "")

# 20) export_project -> 5 fájl a lemezre, a Szabaszat.csv valós sémával
ep = content_json(20)
epv = ep.get("value",{}) if isinstance(ep,dict) else {}
print("export_project fileCount:", epv.get("fileCount"), "| dir:", os.path.basename(epv.get("outputDir","")))
ok &= epv.get("fileCount") == 5
sz_path = os.path.join(EXPORT_DIR, "Szabaszat.csv")
if os.path.exists(sz_path):
    with open(sz_path, encoding="utf-8-sig") as f:
        header = f.readline().strip()
    print("  Szabaszat.csv fejléc:", header[:60], "...")
    ok &= header == "DSMR;Sorszám;Hosszúság;Szélesség;Darab;Név;Megjegyzés;Tipus;Alkatrész Megnevezése;Anyag;Vastagság;Felület tipus;Szín;Minta"
    ok &= os.path.exists(os.path.join(EXPORT_DIR, "Kalkulacio.csv"))
    ok &= os.path.exists(os.path.join(EXPORT_DIR, "Muszaki-Leiras.md"))
else:
    print("  HIBA: Szabaszat.csv nem jött létre!")
    ok = False

# 21) labor_estimate -> műveletek + mancsóra
le = content_json(21)
lev = le.get("value",{}) if isinstance(le,dict) else {}
ops = lev.get("operations",[])
print("labor_estimate műveletek:", [(o.get("operationId"), o.get("appliedPieceCount"), o.get("manHours")) for o in ops])
print("  össz mancsóra:", lev.get("totalManHours"), "| szakmánként:", lev.get("manHoursByRole"))
ok &= len(ops) >= 4
ok &= (lev.get("totalManHours") or 0) > 0
szabas = next((o for o in ops if o.get("operationId")=="SZABAS"), {})
ok &= szabas.get("appliedPieceCount") == 4 and abs((szabas.get("manHours") or 0) - 0.30) < 0.001

# 22) cost_calculation auto-labor (laborHours=-1 -> folyamat-modell)
cc2 = content_json(22)
cv2 = cc2.get("value",{}) if isinstance(cc2,dict) else {}
print("auto-labor cost: laborSource:", cv2.get("laborSource"), "| laborHoursUsed:", cv2.get("laborHoursUsed"),
      "| nettó:", cv2.get("netSellingPriceHuf"))
ok &= cv2.get("laborSource") == "folyamat-modell (auto)"
ok &= abs((cv2.get("laborHoursUsed") or 0) - (lev.get("totalManHours") or -1)) < 0.01  # a becsléssel egyezik
ok &= (cv2.get("netSellingPriceHuf") or 0) > 0

# 23) production_schedule -> CPM: átfutási idő + kritikus út
ps = content_json(23)
psv = ps.get("value",{}) if isinstance(ps,dict) else {}
cp = psv.get("criticalPath",[])
print("production_schedule leadTime (h):", psv.get("leadTimeHours"), "| nap:", psv.get("leadTimeDays"))
print("  kritikus út:", cp)
ok &= (psv.get("leadTimeHours") or 0) > 0
ok &= cp == ["SZABAS","CNC_FURAT","ELZARAS","CSISZOLAS","OSSZEALLITAS"]
# a hátlap-szabás párhuzamos → nem kritikus, van tartaléka
hatlap_op = next((o for o in psv.get("operations",[]) if o.get("operationId")=="HATLAP_SZABAS"), {})
ok &= hatlap_op.get("critical") is False and (hatlap_op.get("slackHours") or 0) > 0

# 24) második skeleton
c2 = content_json(24)
ok &= isinstance(c2,dict) and c2.get("isSuccess") is True

# 25) schedule_projects -> 2 projekt közös kapacitásra, naptári dátumok
sp = content_json(25)
spv = sp.get("value",{}) if isinstance(sp,dict) else {}
print("schedule_projects projektek:", spv.get("projectCount"), "| makespan nap:", spv.get("makespanDays"))
print("  kezdés:", spv.get("startDate"), "→ befejezés:", spv.get("finishDate"))
print("  szakma-kihasználtság %:", spv.get("roleUtilization"))
ok &= spv.get("projectCount") == 2
ok &= (spv.get("makespanHours") or 0) > psv.get("leadTimeHours", 0)  # 2 projekt > 1 projekt átfutása
ok &= spv.get("startDate","").startswith("2026-07-17")  # péntek (munkanapra igazítva marad)
ok &= bool(spv.get("finishDate")) and len(spv.get("tasks",[])) == 12  # 6 művelet × 2 projekt
# a hétvége-átugrás: a péntek 08:00 + ~3 óra átfutás még pénteken végez VAGY hétfőn — ellenőrizzük hogy dátum-formátum ok
ok &= "T" in spv.get("finishDate","")

print("\nSMOKE-TESZT:", "PASS" if ok else "FAIL")
sys.exit(0 if ok else 1)
