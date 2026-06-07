namespace Client.Game;

public class Fleet
{
    public string ShipID { get; set; } = "";
    public string ShipName { get; set; } = "";
    public string CaptainName { get; set; } = "";
    public string CrewNames { get; set; } = "";
    public int Px { get; set; }
    public int Py { get; set; }
    public int Fx { get; set; }
    public int Fy { get; set; }
    public int HP { get; set; }
    public int Score { get; set; }
    public int FireCooldownMs { get; set; }
}
