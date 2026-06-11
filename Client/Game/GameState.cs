using System.Collections.Concurrent;

namespace Client.Game;

public class GameState
{
    private readonly ConcurrentDictionary<string, Fleet> _ships = new();

    public ICollection<Fleet> AllShips => _ships.Values;

    public Fleet? LocalShip { get; private set; }

    public string? LocalShipName { get; set; }
    public string? LocalCaptainName { get; set; }

    public event Action? StateChanged;

    public void ProcessServerMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        if (message.StartsWith("Online,"))
        {
            ProcessOnline(message);
        }
        else if (message.StartsWith("Data,"))
        {
            ProcessData(message);
        }
    }

    private void ProcessOnline(string message)
    {
        // Use regular Split (not RemoveEmptyEntries) to preserve empty fields.
        // Old server may have trailing comma (TrimEnd bug) or empty crewNames.
        var parts = message.Split(',');
        // Online,shipID,shipName,CaptainName,crewNames,...
        // Groups of 4 fields per ship
        var onlineIds = new HashSet<string>();

        for (int i = 1; i + 3 < parts.Length; i += 4)
        {
            string id = parts[i];
            // Skip empty groups (trailing comma artifact) or invalid IDs
            if (string.IsNullOrEmpty(id)) continue;

            string name = parts[i + 1];
            string captain = parts[i + 2];
            string crew = parts[i + 3];

            onlineIds.Add(id);

            if (_ships.TryGetValue(id, out var existing))
            {
                existing.ShipName = name;
                existing.CaptainName = captain;
                existing.CrewNames = crew;
            }
            else
            {
                _ships[id] = new Fleet
                {
                    ShipID = id,
                    ShipName = name,
                    CaptainName = captain,
                    CrewNames = crew
                };
            }
        }

        // Remove ships no longer online
        foreach (var key in _ships.Keys)
        {
            if (!onlineIds.Contains(key))
            {
                _ships.TryRemove(key, out _);
            }
        }

        // Auto-identify local ship
        if (LocalShip == null && LocalShipName != null)
        {
            LocalShip = _ships.Values.FirstOrDefault(s =>
                s.ShipName == LocalShipName && s.CaptainName == LocalCaptainName);
        }

        StateChanged?.Invoke();
    }

    private void ProcessData(string message)
    {
        var parts = message.Split(',');
        // Data,shipID,px,py,fx,fy,HP,score[,fireCooldownMs],...
        // Old server: 7 fields per ship (no FireCooldownMs), may have trailing comma
        // New server: 8 fields per ship (includes FireCooldownMs)

        // Trim trailing empty elements (TrimEnd bug in old server)
        int lastIndex = parts.Length - 1;
        while (lastIndex >= 0 && string.IsNullOrEmpty(parts[lastIndex]))
            lastIndex--;

        int dataCount = lastIndex; // exclude "Data" header at index 0
        if (dataCount <= 0) return;

        // Auto-detect fields per ship: check divisibility
        int fieldsPerShip;
        if (dataCount % 7 == 0)
            fieldsPerShip = 7; // old server protocol
        else if (dataCount % 8 == 0)
            fieldsPerShip = 8; // new server protocol
        else
            return; // unknown format, skip

        for (int i = 1; i + fieldsPerShip - 1 <= lastIndex; i += fieldsPerShip)
        {
            string id = parts[i];
            if (string.IsNullOrEmpty(id)) continue;

            if (!_ships.TryGetValue(id, out var ship))
            {
                ship = new Fleet { ShipID = id };
                _ships[id] = ship;
            }

            if (int.TryParse(parts[i + 1], out int px)) ship.Px = px;
            if (int.TryParse(parts[i + 2], out int py)) ship.Py = py;
            if (int.TryParse(parts[i + 3], out int fx)) ship.Fx = fx;
            if (int.TryParse(parts[i + 4], out int fy)) ship.Fy = fy;
            if (int.TryParse(parts[i + 5], out int hp)) ship.HP = hp;
            if (int.TryParse(parts[i + 6], out int score)) ship.Score = score;

            if (fieldsPerShip >= 8 && int.TryParse(parts[i + 7], out int cd))
                ship.FireCooldownMs = cd;
            else
                ship.FireCooldownMs = 0;
        }

        StateChanged?.Invoke();
    }
}
