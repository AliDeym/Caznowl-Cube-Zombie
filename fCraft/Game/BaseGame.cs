using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using fCraft.Events;
using System.IO;

namespace fCraft.Game
{
    public class BaseGame
    {
        public const string ServerName = Color.Maroon + "[Caznowl] CPE Zombie Survival (No Rules!)";
        private static List<String> OmniBan = new List<string>();
        private static List<String> WoMExempt = new List<string>();
        internal static Dictionary<string, string> TempbanList = new Dictionary<string, string>();

        public static Random rand = new Random();
        public static bool RoundStarted = false;
        public static TimeSpan TimeElapsedSinceLastRound = new TimeSpan();
        public static TimeSpan TimeTillRoundStarts = new TimeSpan();
        public static TimeSpan RoundTime = new TimeSpan();

        public static void Init()
        {
            Server.SaveLevels = false;
            //Init events only. If you want to register something else, like custom commands use the Initialized function below
            Server.Initialized += InitializedHandler;
            //You can use this one to start timers, open up ports, manipulate world list, etc
            Server.Started += StartedHandler;
            Player.Moved += MovedHandler;
            //Player.Disconnected += DisconnectHandler;
            Player.Ready += ConnectedHandler;
            Chat.Sending += ChatHandler;
        }

        // This is where you can go ahead and register custom commands, brushes, etc
        // Server.Initialized is invoked after everything else in Server.InitServer() is done
        static void InitializedHandler(object sender, EventArgs e)
        {
            Game.Commands.Init();
        }

        // Server.Started is invoked just after server fully finished its startup routine
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
            TimeSpan zombieInterval = TimeSpan.FromSeconds( 1 );
            Scheduler.NewTask(ZombieTask).RunForever(zombieInterval);
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
                if (p != e.Player && (bool)e.Player.Metadata["caznowl.zombie", "infected"] && !(bool)p.Metadata["caznowl.zombie", "infected"])
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

        public static void AnnounceInfect(Player infectee, Player infected)
        {
            Server.Message(infectee.ClassyName + " was munched on");
        }

        static void ConnectedHandler(object sender, PlayerEventArgs e)
        {
            if (RoundStarted)
            {
                e.Player.Metadata["caznowl.zombie", "infected"] = true;
                e.Player.Mob = "zombie";
                e.Player.Message(Color.SysDefault + "You have been infected since you joined while a round was in progress.");
            }
            else
            {
                e.Player.Metadata["caznowl.zombie", "infected"] = false;
                e.Player.Mob = "steve";
            }
            AnnounceRoundInformation(e.Player);
        }

        public static void InfectPlayer(Player e)
        {
            e.Metadata["caznowl.zombie", "infected"] = true;
            e.Mob = "zombie";
        }

        static void ChatHandler(object sender, ChatSendingEventArgs e)
        {
            if (e.Player != null && e.Player.Metadata != null)
            {
                string n = "";
                if (!(bool)e.Player.Metadata["caznowl.zombie", "infected"])
                {
                    n = Color.Lime + "[Alive] " + Color.SysDefault;
                }
                else if ((bool)e.Player.Metadata["caznowl.zombie", "infected"])
                {
                    n = Color.Maroon + "[Infected] " + Color.SysDefault;
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

        static void StartRound()
        {
            int i = Server.Players.Count();
            RoundTime = RoundTime.Add(TimeSpan.FromMinutes(2));
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
                p.Metadata["caznowl.zombie", "infected"] = false;
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
                    if ((bool)p.Metadata["caznowl.zombie", "infected"] == false)
                    {
                        return; //Nope, someone is still alive!
                    }
                }
                EndRound();
            }
        }

        static void GenerateVariables() //Changes the game every round
        {
            RoundStarted = false;
            TimeElapsedSinceLastRound = new TimeSpan();
            TimeTillRoundStarts = TimeSpan.FromSeconds(125);
            RoundTime = TimeSpan.FromMinutes(rand.Next(5, 12)).Add(TimeSpan.FromSeconds(5)).Add(TimeSpan.FromMinutes(2));
        }

        //Show a custom join message to the player.
        public static void ShowJoinMessage(Player player)
        {
            player.Message(Color.SysDefault + "Welcome to " + ServerName + Color.SysDefault + "! Please have a good time. If you have any troubles, don't hesitate to ask our staff. " +
                "You can also join our community by visiting " + Color.Blue + "www.caznowl.net" + Color.SysDefault + ".");
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