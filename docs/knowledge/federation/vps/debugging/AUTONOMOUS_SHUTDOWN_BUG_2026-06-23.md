# KRITIKUS BUG: Autonóm rendszer leállítása fél napba telt

> **Dátum:** 2026-06-23
> **Súlyosság:** CRITICAL
> **Státusz:** MEGOLDVA, de `nexus kill` frissítése SZÜKSÉGES

## Probléma összefoglalása

Az autonóm agent infrastruktúra annyira "jól" működött, hogy **lehetetlen volt leállítani**. A felhasználó tokent akart spórolni és kontrolláltan fejleszteni, de a rendszer folyamatosan újraindult.

**Időveszteség:** ~fél nap debugging

## Azonosított perzisztencia rétegek (7 db!)

| # | Típus | Lokáció | Leállítás módja |
|---|---|---|---|
| 1 | System systemd | `/etc/systemd/system/spaceos-knowledge.service` | `sudo systemctl stop && disable` |
| 2 | System systemd | `/etc/systemd/system/claude-code.service` | `sudo systemctl stop && disable` |
| 3 | System systemd | `/etc/systemd/system/marveen.service` | `sudo systemctl stop && disable` |
| 4 | **User systemd** | `~/.config/systemd/user/spaceos-dashboard.service` | `systemctl --user stop && disable` |
| 5 | **User systemd** | `~/.config/systemd/user/spaceos-channels.service` | `systemctl --user stop && disable` |
| 6 | **User systemd** | `~/.config/systemd/user/spaceos-morning.timer` | `systemctl --user stop && disable` |
| 7 | **Régi process** | Háttérben futó `node dist/server.js` Jun22-től | `kill -9 <PID>` |

## Miért volt nehéz megtalálni?

1. **User systemd service-ek** (`~/.config/systemd/user/`) KÜLÖNBÖZNEK a system service-ektől
2. A `systemctl status` csak system service-eket mutat, a `systemctl --user` kell a user service-ekhez
3. A **spaceos-dashboard.service** a Marveen node-ot futtatja, ami agent session-öket indít
4. Régi háttér process-ek (napokkal korábbiak) tovább futnak és a régi config-gal dolgoznak
5. Az .env ENABLE_* flag-ek NEM hatnak a már futó régi process-ekre

## A működési lánc (kaszkád újraindítás)

```
spaceos-dashboard.service (user systemd, Restart=on-failure)
  └── /opt/marveen/dist/index.js (dashboard)
       └── tmux session-öket indít (agent-spaceos, marveen-worker)
            └── claude --dangerously-skip-permissions
                 └── autonomousDev.ts → spaceos-conductor indítás
                      └── további terminálok indítása (backend, frontend, stb.)
```

Bármelyik réteg újraindulása újraépíti az egész láncot!

## Jelenlegi `nexus kill` - HIÁNYOS!

A `/opt/spaceos/scripts/nexus kill` parancs létezik, de NEM kezeli:
- User systemd service-eket
- Marveen dashboard/channels-t
- Régi háttér process-eket

## SZÜKSÉGES: `nexus kill` kibővítése

```bash
kill_all() {
    echo "🛑 EMERGENCY STOP - ALL layers..."

    # 1. System systemd services
    echo "1. System systemd services..."
    sudo systemctl stop spaceos-knowledge.service 2>/dev/null

    # 2. USER systemd services (KRITIKUS - ez hiányzott!)
    echo "2. User systemd services..."
    systemctl --user stop spaceos-dashboard.service 2>/dev/null
    systemctl --user stop spaceos-channels.service 2>/dev/null
    systemctl --user stop spaceos-morning.timer 2>/dev/null

    # 3. Marveen stop script
    echo "3. Marveen services..."
    /opt/marveen/scripts/stop.sh 2>/dev/null

    # 4. MINDEN node/ts-node process (régi is!)
    echo "4. Background processes..."
    pkill -9 -f "node.*dist/server.js" 2>/dev/null
    pkill -9 -f "node.*marveen" 2>/dev/null
    pkill -9 -f "ts-node.*server" 2>/dev/null
    pkill -9 -f "inotifywait" 2>/dev/null

    # 5. MINDEN agent tmux session (spaceos-root KIVÉTELÉVEL!)
    echo "5. Agent tmux sessions..."
    for s in spaceos-backend spaceos-frontend spaceos-conductor \
             spaceos-architect spaceos-librarian spaceos-explorer \
             spaceos-designer agent-spaceos marveen-worker; do
        tmux kill-session -t "$s" 2>/dev/null
    done

    # 6. Verify
    echo ""
    remaining=$(ps aux | grep -E "(node.*server|marveen|conductor)" | grep -v grep | wc -l)
    sessions=$(tmux list-sessions 2>/dev/null | grep -v spaceos-root | wc -l)

    if [ "$remaining" -eq 0 ] && [ "$sessions" -eq 0 ]; then
        echo "✅ ALL STOPPED - Only spaceos-root remains"
    else
        echo "⚠️ Still running: $remaining processes, $sessions sessions"
        ps aux | grep -E "(node.*server|marveen)" | grep -v grep
    fi
}
```

## Disable parancs is kell!

```bash
disable_all() {
    echo "🔒 DISABLING all auto-start..."

    # System
    sudo systemctl disable spaceos-knowledge.service 2>/dev/null
    sudo systemctl disable claude-code.service 2>/dev/null
    sudo systemctl disable marveen.service 2>/dev/null

    # User
    systemctl --user disable spaceos-dashboard.service 2>/dev/null
    systemctl --user disable spaceos-channels.service 2>/dev/null
    systemctl --user disable spaceos-morning.timer 2>/dev/null

    echo "✅ All auto-start disabled"
}
```

## Tanulságok

1. **Minden perzisztencia réteg dokumentálva legyen** - egy helyen, explicit listával
2. **Egy parancs** legyen ami TÉNYLEG mindent leállít (`nexus kill`)
3. **Egy parancs** legyen ami TÉNYLEG mindent disable-ol (`nexus disable`)
4. **User systemd** service-ek könnyen elfelejtődnek - más namespace!
5. **Régi process-ek** napokig futhatnak észrevétlenül a régi config-gal
6. **Token budget control** KELL mielőtt az autonóm rendszer "túl jól" működik

## Kapcsolódó dokumentumok

- `/opt/spaceos/docs/agent-infrastructure/DISPATCH_CONTROL_DESIGN.md` - Token budget terv
- `/opt/spaceos/scripts/nexus` - Központi vezérlő script
- `~/.config/systemd/user/` - User systemd service-ek helye

## Jövőbeli megelőzés

1. **Minden új szolgáltatás** regisztrálva legyen a `nexus` script-ben
2. **Startup/shutdown checklist** karbantartása
3. **Token monitoring** implementálása MIELŐTT autonóm módba kapcsolunk
