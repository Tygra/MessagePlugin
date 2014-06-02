using System;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using TerrariaApi;
using TShockAPI.Hooks;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;

namespace MessagePlugin
{
    public class MPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }

        public MPlayer(int index)
        {
            Index = index;
        }

        //PLAYER TOOLS
        // Return player by ID
        public static MPlayer GetPlayerById(int index)
        {
            foreach (MPlayer player in MessagePlugin.Players)
            {
                if (player != null && player.Index == index)
                    return player;
            }
            return null;
        }

        // Return player by Name
        public static MPlayer GetPlayerByName(string name)
        {
            var player = TShock.Utils.FindPlayer(name)[0];
            if (player != null)
            {
                foreach (MPlayer ply in MessagePlugin.Players)
                {
                    if (ply.TSPlayer == player)
                    {
                        return ply;
                    }
                }
            }
            return null;
        }

        // Return player from TShock db
        public static int GetPlayerInDb(string name)
        {
            int ply = 0;

            if (name != null)
            {
				if (TShock.Users.GetUserByName (name) != null)
					return 1;
            }

            return ply;
        }

    }
}