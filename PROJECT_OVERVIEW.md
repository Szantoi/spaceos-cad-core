# CabinetBilder — Projekt Áttekintés (Project Overview)

Ez a dokumentum összefoglalja a **CabinetBilder** projekt céljait, architektúráját és a felhasznált komponenseket/tárakat.

---

## 1. A Projekt Célja (Project Goal)

A **CabinetBilder** egy modern, parametrikus bútortervező és gyártás-előkészítő .NET 10 beépülő modul (plugin) és parancssoros eszköz (CLI) AutoCAD környezethez. 

A rendszer fő céljai:
*   **Parametrikus bútor-tervezés (Skeleton):** Lehetővé tenni a tervezők számára, hogy parametrikus bútorokat (panelek, élek, furatok, hornyok) tervezzenek közvetlenül AutoCAD-ben, ahol a DWG fájl az egyedüli igazságforrás (Single Source of Truth).
*   **Okos Objektumok (SmartObjects):** Metaadatok hozzárendelése és validálása az AutoCAD entitásokhoz a DWG fájl `XData` formátumában.
*   **Offline-First működés:** Egy lokális SQLite adatbázis (`client.db`) segítségével pufferelni a vágási listák (BOM) és megmunkálási adatok szerverre való feltöltését (Outbox Queue), valamint gyorsítótárazni a sablonokat és anyagárakat (DPAPI titkosítással).
*   **Multi-CAD-instance támogatás:** Lehetővé tenni, hogy a felhasználó több AutoCAD ablakot futtasson párhuzamosan adatvesztés és zárolási ütközések nélkül (WAL mód + Named Mutex alapú OutboxLeader választás).
*   **Automatizált gyártás-előkészítés:** Egy kattintással előállítani a gyártáshoz szükséges vágási listákat (BOM), furatképeket és marási (pl. hátlap-horony) adatokat, majd ezeket szinkronizálni a SpaceOS felhőalapú ERP rendszerével.

---

## 2. A Projekt Felépítése és Repók (Project Structure & Repos)

A projekt a `C:/Users/szant/Documents/Development/Cabinet_bilder_scripts` workspace-ben található, és az alábbi modulokra tagolódik:

1.  **[CabinetBilder.Core](file:///C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/CabinetBilder.Core)** (Domain logikai réteg):
    *   AutoCAD-független C# osztálykönyvtár.
    *   Tartalmazza a bútorok parametrikus modelljét (`Skeleton` aggregate: panelek, furatok, hornyok), a SmartObject definíciókat és a MediatR use-case-eket.
2.  **[CabinetBilder.Adapter.AutoCAD](file:///C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/CabinetBilder.Adapter.AutoCAD)** (AutoCAD specifikus réteg):
    *   WPF és AutoCAD API függő adapter.
    *   Itt található az AutoCAD Palette UI (SmartObjectPalette), a helyi DWG XRecord perzisztencia, a context-menü és grip overrule-ok, valamint az AutoCAD-ből hívható parancsok (pl. `SyncDimToBlock`).
3.  **[CabinetBilder.SpaceOsBridge](file:///C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/CabinetBilder.SpaceOsBridge)** (Integrációs réteg):
    *   Megvalósítja a helyi SQLite tárolót (`client.db`), az offline outbox feldolgozót, a Keycloak bejelentkezést (Device Code Flow) és a token tárolást.
4.  **[CabinetBilder.Cli](file:///C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/CabinetBilder.Cli)** (Parancssoros diagnosztikai eszköz):
    *   Önálló parancssori segédprogram (`cabinetbilder.exe`) a bejelentkezéshez, template letöltésekhez és diagnosztikai JSON exportokhoz.
5.  **[CabinetBilder.Tests](file:///C:/Users/szant/Documents/Development/Cabinet_bilder_scripts/CabinetBilder.Tests)** (Teszt projekt):
    *   MSTest alapú unit és integrációs tesztek.

### Felhasznált / Külső Erőforrások
*   **[JoineryTech.AgentScripts](file:///C:/Users/szant/Documents/Development/JoineryTech.AgentScripts/database/knowledge)** repó:
    *   Ez a külső tárhely tartalmazza a SpaceOS működéséhez szükséges **tudásbázist** (Knowledge Base) `.md` fájlok formájában (Prompt engineering, Multi-workspace coordination, Design thinking, VS Code Copilot operating models).
    *   A Conductor háttérszerver indulásakor ezt a mappát indexeli be a szemantikus keresőjébe.
*   **[knowledge-service-0.0.01](file:///C:/Users/szant/Documents/Development/knowledge-service-0.0.01)** (SpaceOS Conductor / Knowledge Service):
    *   Helyi Node.js szerver, ami kiszolgálja az ágenseket (RAG keresés, Task Message Box, eseménybusz és Telegram koordináció).
*   **[tartalom_mentes](file:///C:/Users/szant/Documents/Development/tartalom_mentes)** mappa:
    *   Ebben a mappában találhatók a korábbi csevegések, kód-auditok és korábbi tudásbázis-verziók mentései és exportjai.
    *   Ezek a forrásadatok szolgáltak alapul a végleges, strukturált SpaceOS tudástár (.md fájlok) felépítéséhez és beindexeléséhez.

---

## 3. Aktuális Feladatunk: Fázis 12 (Milestone 12)

Jelenleg a **0021**-es mérföldkőnél járunk, melynek címe: **Machining Features (Grooving/Backpanel slot)**.
*   **Célja:** A hátlap horonymarási adatok paramétereinek implementálása a `CabinetBilder.Core` domainben, a 3D vizualizáció (3D test kivonás) megvalósítása a `CabinetBilder.Adapter.AutoCAD`-ben, és a horonymarási adatok beépítése a BOM export logikába.

---
*Dokumentum generálva: 2026-07-06*
