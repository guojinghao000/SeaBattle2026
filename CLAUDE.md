# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SeaBattle2026 is a multiplayer naval battle game — C# WinForms client + server, targeting `net8.0-windows`. The solution (`SeaBattle2026.sln`) contains two projects: **Client** and **Server2026**. Communication is TCP (port 18000) for game commands, with an optional UDP listener (port 18001) for broadcast discovery.

## Build & Run

```bash
# Build both projects
dotnet build SeaBattle2026.sln

# Run server first (listens on detected LAN IP, port 18000)
dotnet run --project Server2026\Server2026.csproj

# Then run client (connects to IP in the login panel, default 127.0.0.1)
dotnet run --project Client\Client.csproj
```

No test project exists in this solution.

## Architecture

### Client — 3 layers

| Layer | File | Responsibility |
|-------|------|----------------|
| **Net** | `Client/Net/NetworkService.cs` | TCP connect/send/receive + UDP listen. `ConnectAsync()` starts background read tasks that enqueue server messages into `ReceivedMessages` (ConcurrentQueue). `SendCommandAsync()` writes to TCP stream. |
| **Game** | `Client/Game/GameState.cs` | Parses `Online,` and `Data,` messages, maintains `ConcurrentDictionary<string, Fleet>` of all ships, auto-identifies `LocalShip` by matching name/captain. Fires `StateChanged` event on update. |
| **Game** | `Client/Game/Fleet.cs` | Plain data model: ShipID, ShipName, CaptainName, CrewNames, Px, Py, Fx, Fy, HP, Score. |
| **UI** | `Client/UI/Main.cs` | Login panel, battlefield rendering (100×100 grid with ships as colored dots, HP bars, range circles), keyboard input (WASD/arrows move, Space/F auto-fire), leaderboard, 3-timer game loop. |

### Client game loop (2 timers)

- **1000ms** (`_moveTimer`): sends `Move` if direction keys held
- **2000ms** (`_fireTimer`): re-enables fire cooldown

Auto-fire (`TryFireAtNearestTarget`) finds the nearest enemy within radius 5 (Euclidean), clamps the offset to the circle boundary, and sends `Fire,dx,dy`.

### Server

| File | Responsibility |
|------|----------------|
| `Server2026/Form1.cs` | Main window: TCP listener, fleet info tree, system log, naval map rendering. Manages `List<Ship> shipList` with `lock (_shipLock)`. Spawns/refills bots. |
| `Server2026/Ship.cs` | Player/bot entity: position, HP, score, TCP client ref, move/fire cooldown timers. `ReSet()` randomizes position and resets HP to 3. `IsBot` flag for auto-generated targets. |

**Server threading model:** `AcceptTcpClient` loop runs on a dedicated thread. Each connected client gets its own `ReceiveData` thread that reads lines from the TCP stream and dispatches commands (login/move/fire/logout). A `System.Timers.Timer` fires every 500ms to process hits, broadcast `Data` messages, and refill bots.

**Bots:** 5 auto-spawned target ships (`靶船-A` through `靶船-E`). Bots don't move or fire (their timers are stopped). When killed, they respawn via `ReSet()`; missing bots are refilled each 500ms cycle. Killing a bot gives the attacker +1 score and full HP.

## Wire Protocol

All messages are comma-delimited UTF-8 lines over TCP.

**Client → Server:**
- `Login,shipName,CaptainName,crewNames`
- `Move,x,y` — x,y ∈ [-1,1], once per second
- `Fire,dx,dy` — dx,dy ∈ [-5,5] with dx²+dy² ≤ 25, relative to ship position, 2s cooldown
- `Logout`

**Server → Client:**
- `Online,shipID,shipName,CaptainName,crewNames,...` — 4-field groups, sent when online list changes
- `Data,shipID,px,py,fx,fy,HP,score,...` — 7-field groups, sent every 500ms

## Key game rules

- Map: 100×100 grid, coordinates 0–100
- HP: 3 per ship; each hit reduces by 1; at 0 the ship respawns at a random position
- Fire range: radius 5 (Euclidean), 2-second cooldown
- Movement: 1 unit per second in 4 directions
- Score: increments by 1 for each kill; HP restored to 3 on kill

## Server bug fixes (from commit history)

The README documents several critical server fixes applied during development:
- `TcpListener` binds `IPAddress.Any` (not a specific IP) so `127.0.0.1` connections work
- `Fire` handler computes `fx = px + dx` (was storing relative offset, breaking hit detection)
- `shipList` uses `lock(_shipLock)` for all read/write to prevent concurrent modification exceptions
- `ReceiveData` cleans up timers, reader/writer, and client on disconnect
- `FormClosing` calls `Environment.Exit(0)` to prevent lingering processes
- Chinese text encoding was corrupted and has been repaired

## 新增功能
- 由于舰船之间的射程一样，目前需要设计自主开火功能，否则几乎都是同归于尽的下场，其次为了更加有竞技性，需要使敌我双方看到开火cd，其余要求请转至`要求.md`

## 要求（非常重要）
- Server2026_ori文件夹是服务端源文件请勿修改，对于Server2026文件夹中的代码修改要严格参考服务端源文件和`TCP&UDP海战大联机-开发文档 .docx`逻辑功能且需要做尽可能少的修改完成任务
- 任何无法完成的要求需要说明

