using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using fCraft.Events;

namespace fCraft
{
    public class Zombie
    {
        public static Random rand = new Random();
        public static bool RoundStarted = false;
        public static TimeSpan TimeElapsedSinceLastRound = new TimeSpan();
        public static TimeSpan TimeTillRoundStarts = new TimeSpan();
        public static TimeSpan RoundTime = new TimeSpan();


        public static void Init()
        {
            Server.Initialized += InitializedHandler;
            Server.Started += StartedHandler;
            Player.Moved += MovedHandler;
            Player.Ready += ConnectedHandler;
            Chat.Sending += ChatHandler;
            Player.Disconnected += ExtPlayerRemove;
            Player.Ready += ExtPlayerSend;
            LoadAddons();
        }

        static void InitializedHandler(object sender, EventArgs e)
        {
            Commands.Init();
        }

        private static void ExtPlayerSend(object sender, PlayerEventArgs e)
        {
            List<int> TakenNameID = new List<int>();
            foreach (var ply in Server.Players)
            {
                TakenNameID.Add(ply.NameID);
            }
            int final = 0;
            for (int a = 255; a >= 0; a--)
            {
                if (!TakenNameID.Contains(a))
                {
                    final = a;
                }
            }
            e.Player.NameID = final;

            int permission = 100;

            // Set permissions later {To Snowl}

            foreach (var ply in Server.Players)
            {
                ply.Send(Packet.ExtAddPlayerName((short)final, e.Player.Name, e.Player.ClassyName, e.Player.Info.Rank.ClassyName, (byte)permission));
            }
        }

        private static void ExtPlayerRemove(object sender, PlayerDisconnectedEventArgs e)
        {
            foreach (var ply in Server.Players)
            {
                if (ply != e.Player)
                {
                    ply.Send(Packet.ExtRemovePlayerName((short)e.Player.NameID));
                }
            }
        }

        static void LoadAddons()
        {
            if (!Directory.Exists("addons/"))
            {
                Directory.CreateDirectory("addons/");
            }
            Logger.Log(LogType.ConsoleOutput, "[DLL]: Loading plugins...");
            var dir = new DirectoryInfo("addons/").GetFiles().Where(x => x.Name.EndsWith(".dll")).ToArray();
            foreach (var file in dir)
            {
                try
                {
                    Assembly asm;
                    using (FileStream fs = File.Open("addons/" + file.Name, FileMode.Open))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            byte[] buffer = new byte[1024];
                            int read = 0;
                            while ((read = fs.Read(buffer, 0, 1024)) > 0)
                                ms.Write(buffer, 0, read);
                            asm = Assembly.Load(ms.ToArray());
                            ms.Close();
                            ms.Dispose();
                        }
                        fs.Close();
                        fs.Dispose();
                    }
                    var type = asm.GetType(file.Name.Replace(".dll", "") + ".Main");
                    var meth = type.GetMethod("Init");
                    var call = Activator.CreateInstance(type);
                    meth.Invoke(call, null);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogType.Error, "The plugin: " + file.Name.Replace(".dll", "") + " doesn't seems to work with current version of Mapping, Exception: " + ex.ToString());
                }
            }
            Logger.Log(LogType.ConsoleOutput, "[DLL]: Plugins loaded!");
        }

        static void StartRound()
        {
            int i = Server.Players.Count();
            RoundTime = RoundTime.Add(TimeSpan.FromSeconds(30));
            Server.Message(Color.Maroon + "The round has started!");
            RoundStarted = true;
            int amountOfPlayers = Server.Players.Length;
            Player e = Server.Players[rand.Next(amountOfPlayers - 1)];
            InfectPlayer(e);
            Server.Message(e.ClassyName + " has been chosen as the" + Color.Red + " first infected!");
            foreach (Player p in Server.Players.ToArray())
            {
                AnnounceRoundInformation(p);
            }
        }

        internal static void EndRound()
        {
            TimeElapsedSinceLastRound = RoundTime;
            RoundStarted = false;
            Server.Message(Color.Maroon + "The round has ended!");

            var files = Directory.GetFiles("maps", "*.*").Where(name => !name.EndsWith(".lvlqonly") && !name.Equals(WorldManager.MainWorld.Name)).ToArray();
            int i = rand.Next(files.Length - 1);
            string firstMainLevel = Path.GetFileName(files[i]);
            string firstMainLevelNoExt = Path.GetFileNameWithoutExtension(files[i]);
            Player.Console.ParseMessage(String.Format("/WLoad {0} {1}", firstMainLevel, Truncate(firstMainLevelNoExt, 16)), true);
            World LoadedWorld = WorldManager.FindWorldExact(Truncate(firstMainLevelNoExt, 16));
            WorldManager.MainWorld = LoadedWorld;
            WorldManager.MainWorld.BlockDB.Clear();
            UnloadAll();

            GenerateVariables();
            foreach (Player p in Server.Players.ToArray())
            {
                if (p.EnabledEnv)
                {
                    p.Send(Packet.EnvSetColor(1, 128, 128, 128));
                    p.Send(Packet.EnvSetColor(2, 128, 128, 128));
                    p.Send(Packet.EnvSetColor(3, 128, 128, 128));
                    p.Send(Packet.EnvSetColor(4, 128, 128, 128));
                }
                p.Infected = false;
                p.Mob = "steve";
                AnnounceRoundInformation(p);
            }
        }

        static void CheckIfEnded()
        {
            if (RoundStarted)
            {
                if (Server.Players.Count() < 2)
                {
                    EndRound();
                    return;
                }
                foreach (Player p in Server.Players.ToArray())
                {
                    if (!p.Infected)
                    {
                        return;
                    }
                }
                EndRound();
            }
        }

        static void StartedHandler(object sender, EventArgs e)
        {
            UnloadAll();
            var files = Directory.GetFiles("maps", "*.*").Where(name => !name.EndsWith(".lvlqonly") && !name.Equals(WorldManager.MainWorld.Name)).ToArray();
            int i = rand.Next(files.Length - 1);
            string firstMainLevel = Path.GetFileName(files[i]);
            string firstMainLevelNoExt = Path.GetFileNameWithoutExtension(files[i]);
            Player.Console.ParseMessage(String.Format("/WLoad {0} {1}", firstMainLevel, Truncate(firstMainLevelNoExt, 16)), true);
            World LoadedWorld = WorldManager.FindWorldExact(Truncate(firstMainLevelNoExt, 16));
            WorldManager.MainWorld = LoadedWorld;
            WorldManager.MainWorld.BlockDB.Clear();
            UnloadAll();

            GenerateVariables();
            TimeSpan zombieInterval = TimeSpan.FromSeconds(1);
            Scheduler.NewTask(ZombieTask).RunForever(zombieInterval);
        }

        static void ChatHandler(object sender, ChatSendingEventArgs e)
        {
            if (e.Player != null && e.Player.Metadata != null)
            {
                string n = "";
                if (e.Player.Referee)
                {
                    n = Color.Olive + "[Referee] ";
                }
                else if (!e.Player.Infected)
                {
                    n = Color.Lime + "[Alive] ";
                }
                else if (e.Player.Infected)
                {
                    n = Color.Maroon + "[Infected] ";
                }
                e.FormattedMessage = n + e.FormattedMessage;
            }
        }

        internal static void AnnounceRoundInformation(Player player)
        {
            if (!RoundStarted)
            {
                player.Message(Color.SysDefault + "[ZS]: Time remaining till round starts: " + Color.Maroon + TimeSpan.FromSeconds(-(TimeElapsedSinceLastRound.TotalSeconds - TimeTillRoundStarts.TotalSeconds)).ToString());
            }
            else
            {
                player.Message("Round Time: " + Color.Lime + RoundTime.Subtract(TimeSpan.FromSeconds(5)).Subtract(TimeSpan.FromMinutes(2)).ToString());
            }
        }

        public static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static void UnloadAll()
        {
            foreach (World world in WorldManager.Worlds)
            {
                if (WorldManager.MainWorld != world)
                {
                    WorldManager.RemoveWorld(world);
                }
            }
            Server.RequestGC();
        }

        static void MovedHandler(object sender, PlayerMovedEventArgs e)
        {
            foreach (Player p in Server.Players.ToArray())
            {
                if (p != e.Player && e.Player.Infected && !p.Infected)
                {
                    if (Math.Abs(e.Player.Position.X - p.Position.X) <= 32 &&
                        Math.Abs(e.Player.Position.Y - p.Position.Y) <= 64 &&
                        Math.Abs(e.Player.Position.Z - p.Position.Z) <= 32)
                    {
                        InfectPlayer(p);
                        AnnounceInfect(e.Player, p);
                    }
                }
            }
        }

        static void ConnectedHandler(object sender, PlayerEventArgs e)
        {
            if (RoundStarted)
            {
                InfectPlayer(e.Player);
                e.Player.Message(Color.SysDefault + "You have been infected since you joined while a round was in progress.");
            }
            else
            {
                e.Player.Infected = false;
                e.Player.Mob = "steve";
            }
            AnnounceRoundInformation(e.Player);
        }

        public static void AnnounceInfect(Player infectee, Player infected)
        {
            Server.Message(infected.ClassyName + " was munched on by " + infectee.ClassyName);
        }

        public static void InfectPlayer(Player e)
        {
            e.Infected = true;
            e.Mob = "zombie";
            if (!e.EnabledEnv) return;
            e.Send(Packet.EnvSetColor(1, 255, 0, 0));
            e.Send(Packet.EnvSetColor(2, 255, 0, 0));
            e.Send(Packet.EnvSetColor(3, 255, 0, 0));
            e.Send(Packet.EnvSetColor(4, 255, 0, 0));
        }

        static void GenerateVariables()
        {
            RoundStarted = false;
            TimeElapsedSinceLastRound = new TimeSpan();
            TimeTillRoundStarts = TimeSpan.FromSeconds(125);
            RoundTime = TimeSpan.FromMinutes(rand.Next(5, 12)).Add(TimeSpan.FromSeconds(5)).Add(TimeSpan.FromMinutes(2));
        }

        static void ZombieTask(SchedulerTask task)
        {
            TimeElapsedSinceLastRound = TimeElapsedSinceLastRound.Add(TimeSpan.FromSeconds(1));
            if (Server.Players.Count() < 2)
            {
                TimeElapsedSinceLastRound = TimeSpan.FromSeconds(1);
            }
            if (TimeElapsedSinceLastRound.TotalSeconds <= TimeTillRoundStarts.TotalSeconds && TimeElapsedSinceLastRound.TotalSeconds <= 125)
            {
                if (TimeElapsedSinceLastRound.Seconds == 0)
                {
                    foreach (Player p in Server.Players.ToArray())
                    {
                        AnnounceRoundInformation(p);
                    }
                }
                else if (TimeElapsedSinceLastRound.TotalSeconds >= 120 && TimeElapsedSinceLastRound.TotalSeconds <= 125)
                {
                    foreach (Player p in Server.Players.ToArray())
                    {
                        AnnounceRoundInformation(p);
                    }
                }
            }
            else if (TimeElapsedSinceLastRound.TotalSeconds == TimeTillRoundStarts.TotalSeconds + 1)
            {
                StartRound();
            }
            else if (TimeElapsedSinceLastRound.TotalSeconds == TimeSpan.FromMinutes(RoundTime.Minutes).Seconds && TimeElapsedSinceLastRound.Seconds == 0)
            {
                EndRound();
            }
            else if (TimeElapsedSinceLastRound.TotalSeconds % 3 == 0)
            {
                CheckIfEnded();
            }
        }
    }
}
