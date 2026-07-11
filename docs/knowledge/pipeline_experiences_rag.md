# Projekt Tapasztalatok: OCR & RAG Feldolgozási Pipeline Optimalizáció

Ez a dokumentum összefoglalja a **Faipari Műszaki Dokumentáció** és a **Faipari Gyártásszervezés** tankönyvek tömeges feldolgozása során szerzett technikai tapasztalatokat, hibaelhárítási megoldásokat és optimalizációkat.

---

## 1. Google AI Studio Ingyenes Kvóta & Modell-Rotáció 🔄

### Kihívás
A Google AI Studio ingyenes hozzáférése szigorú korlátot alkalmaz: **kifejezetten 20 kérés / nap / modell** a Flash modellekre vonatkozóan. Egy 130 oldalas tankönyv több lépésből álló (OCR + Beautify) feldolgozása során ez a limit percek alatt elfogyott.

### Megoldás (Modell-Rotációs Motor)
Kifejlesztettünk egy dinamikus rotációs mechanizmust az API hívó kliensben (`api_vision.py`).
* Létrehoztunk egy 11 modellből álló medencét (pool):
  * `gemini-2.5-flash`, `gemini-3.5-flash`, `gemini-flash-latest`, `gemini-2.0-flash`, `gemini-2.0-flash-lite`, `gemini-2.5-flash-lite`, `gemini-3.1-flash-lite`, `gemini-flash-lite-latest`, `gemini-pro-latest`, `gemini-2.5-pro`, `gemini-3.1-pro-preview`
* Amikor az API **`429 Rate Limit`** vagy **`RESOURCE_EXHAUSTED`** hibát ad vissza, a kliens azonnal átvált a pool következő modelljére, és várakozás nélkül újrapróbálja a kérést.
* Ezzel a módszerrel a napi kapacitást **220 kérésre** növeltük, ami lehetővé tette a teljes feldolgozást nulla plusz költséggel.

---

## 2. Ingyenes Gemini 503-as Szerver Túlterhelések Kezelése ⚡

### Kihívás
Az ingyenes Gemini végpontok a magas leterheltség miatt gyakran adtak vissza **`503 Service Unavailable / High Demand`** (Szolgáltatás nem elérhető) hibákat. A korábbi hibakezelő ilyenkor percekig altatta (sleep) a szálat, ami blokkolta a futást.

### Megoldás
Kiterjesztettük a modell-rotációs feltételt. 
* Ha az API **`503`**, **`unavailable`** vagy **`temporarily`** kulcsszavakat tartalmazó hibát ad vissza, a rendszer ezt is híváskorlátnak tekinti.
* **Azonnal átvált egy másik Gemini modellre** a listából, és 2 másodpercen belül sikeresen befejezi az oldalt. Ezzel a várakozási idő szinte nullára csökkent.

---

## 3. Windows Fájl-Zárolási (PermissionError) Hibaelhárítás 🖥️

### Kihívás
Windows környezetben az atomi mentések (`os.replace`) során **`PermissionError: [WinError 32]`** lépett fel. Ennek oka, hogy a háttérben futó Google Drive Sync vagy más fájlfigyelő szolgáltatások zárolták a `cache_v*.json` fájlt éppen abban a pillanatban, amikor a Python felül akarta írni.

### Megoldás
Hibatűrő fájlcsere mechanizmust vezettünk be a `storage_json.py` mentési logikájában:
* A fájl felülírását egy maximum 10 próbálkozásból álló ciklusba zártuk.
* Ha zárolási hiba lép fel, a kód vár 0.5 másodpercet, majd újrapróbálja. Ez teljesen kiküszöbölte a Windowsos környezetben jellemző tranziens fájlrendszeri ütközéseket.

---

## 4. Üres Oldalak Kezelése a Szépítő (Beautify) Fázisban 📄

### Kihívás
Néhány oldal (pl. üres lapok vagy szöveg nélküli ábrák) feldolgozásakor az EasyOCR 0 szövegblokkot észlelt. A `beautifier.py` ilyenkor naplózta a figyelmeztetést és kilépett, de **nem állította át a lap állapotát `beautified = True`-ra**. Így a pipeline minden újraindításakor ezeket az üres oldalakat újra és újra elküldte az AI-nak.

### Megoldás
Javítottuk a logikát: ha egy oldalon nincsenek blokkok, a rendszer naplózza a figyelmeztetést, de **beállítja a `beautified = True` flaget**, majd úgy lép tovább. Így a szépítési fázis az újraindítások során azonnal átugorja a már ellenőrzött üres oldalakat.

---

## 5. Menetszám-Optimalizálás (Pass Optimization) ⚙️

### Kihívás
Alapesetben a kód 2-szeres beolvasási kört végzett (két független Vision hívás oldalanként), ami megduplázta az API kvótafelhasználást és felezte a sebességet.

### Megoldás
Mivel a lokális EasyOCR már előre behatárolja a szövegdobozok pontos helyét, a Gemini-nek elegendő 1 pass segítségével elolvasnia és korrigálnia azokat.
* A `self.n_passes = 2` értéket átállítottuk `1`-re az `extractor.py`-ban.
* **Eredmény**: 33%-os sebességnövekedés és kvótamegtakarítás, miközben a szövegminőség azonos maradt.
