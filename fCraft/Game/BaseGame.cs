using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using fCraft.Events;
using System.IO;
using fCraft;

namespace fCraft.Game
{
    public class BaseGame
    {
        public const string ServerName = Color.Maroon + "[Caznowl] CPE Zombie Survival (No Rules!)";
        private static List<String> OmniBan = new List<string>();
        private static List<String> WoMExempt = new List<string>();
        internal static Dictionary<string, string> TempbanList = new Dictionary<string, string>();


        public static Action Game = () => Zombie.Init();

        public static void Init()
        {
            Server.SaveLevels = false;
            Game();
        }
        
        public static void ShowJoinMessage(Player player)
        {
            player.Message(Color.SysDefault + "Welcome to " + ServerName + Color.SysDefault + "! Please have a good time. If you have any troubles, don't hesitate to ask our staff. " +
                "You can also join our community by visiting " + Color.Blue + "www.caznowl.net" + Color.SysDefault + ".");
        }
    }
}