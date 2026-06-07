using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server2026
{
    public class Ship
    {
        public static int count = 0;
        public string shipID = "";
        public string shipName = "";
        public string captainName = "";
        public string crewNames = "";
        public int px;
        public int py;
        public int fx;
        public int fy;
        public int hp;
        public int score;
        public bool allowMove = false;
        public bool allowFire = false;
        public bool IsBot { get; set; }
        public DateTime LastFireTime { get; set; } = DateTime.MinValue;
        public int FireCooldownMs
        {
            get
            {
                if (LastFireTime == DateTime.MinValue) return 0;
                int elapsed = (int)(DateTime.Now - LastFireTime).TotalMilliseconds;
                return Math.Max(0, 2000 - elapsed);
            }
        }

        public TcpClient? Client { get; private set; }
        public StreamReader? SReader { get; private set; }
        public StreamWriter? SWriter { get; private set; }
        public System.Timers.Timer moveTimer { get; set; } = new(1000);
        public System.Timers.Timer fireTimer { get; set; } = new(2000);
        Random rand = new();

        public Ship(TcpClient? client)//(string shipName, string captainName, string crewNames, TcpClient? client)
        {
            shipID = string.Format("{0,5:D3}", Interlocked.Increment(ref count) - 1);
            //this.shipName = shipName;
            //this.captainName = captainName;
            //this.crewNames = crewNames;

            ReSet();
            fx = -1;
            fy = -1;            
            score = 0;
            moveTimer.Elapsed += (s, e) => allowMove = true;
            fireTimer.Elapsed += (s, e) => allowFire = true;
            moveTimer.Start();
            fireTimer.Start();

            Client = client;
            if (client != null)
            {
                NetworkStream netStream = client.GetStream();
                SReader = new StreamReader(netStream, Encoding.UTF8);
                SWriter = new StreamWriter(netStream, Encoding.UTF8);
            }
        }

        public void ReSet()
        {
            px = rand.Next(0, 101);
            py = rand.Next(0, 101);
            hp = 3;
        }
    }
}
