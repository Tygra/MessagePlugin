using System;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using TerrariaApi.Server;
using Terraria;
using TShockAPI.Hooks;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;

namespace MessagePlugin {
  [ApiVersion(1, 19)]
  public class MessagePlugin : TerrariaPlugin {
    public static List<Message> Messages = new List<Message>();

    public override string Name {
      get { return "MessagePlugin2"; }
    }

    public override string Author {
      get { return "Created by Lmanik - Renovated by Colin."; }
    }

    public override string Description {
      get { return ""; }
    }

    public override Version Version {
      get { return new Version(1, 1); }
    }

    public override void Initialize() {
      ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
      ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
      PlayerHooks.PlayerPostLogin += OnPostLogin;
    }

    protected override void Dispose(bool disposing) {
      if (disposing) {
        ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
        ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
        PlayerHooks.PlayerPostLogin -= OnPostLogin;
      }
      base.Dispose(disposing);
    }

    public MessagePlugin(Main game)
      : base(game) {
    }

    public void OnInitialize(EventArgs e) {
      //init Message Plugin db
      MDb.InitMessageDB();

      //set group
      bool msg = false;

      foreach (Group group in TShock.Groups.groups) {
        if (group.HasPermission("msg.use"))
          msg = true;
      }

      List<string> permlist = new List<string>();
      if (!msg)
        permlist.Add("msg.use");

      TShock.Groups.AddPermissions("trustedadmin", permlist);

      Commands.ChatCommands.Add(new Command("msg.use", Msg, "msg"));
    }

    public void OnGreetPlayer(GreetPlayerEventArgs e) {
      if (TShock.Players[e.Who].Group.HasPermission("msg.use")) {
        string name = TShock.Players[e.Who].Name;
        int count = GetUnreadEmailsByName(name);
        TShock.Players[e.Who].SendInfoMessage("You have " + count + " unread messages.");
      }
    }

    public void OnPostLogin(PlayerPostLoginEventArgs e) {
      if (TShock.Players[e.Player.Index].Group.HasPermission("msg.use")) {
        string name = TShock.Players[e.Player.Index].Name;
        int count = GetUnreadEmailsByName(name);
        TShock.Players[e.Player.Index].SendMessage("You have " + count + " unread messages.", Color.Fuchsia);
      }
    }

    // Return unread emails
    public static int GetUnreadEmailsByName(string name) {
      if (name != null) {
        List<Message> m = MDb.GetMessagesType(name, false);
        return m.Count;
      }

      return 0;
    }

    // Save message to db
    public static void SendMessage(string to, string from, string text) {
      MDb.AddMessage(from, to, text);
      List<TSPlayer> players = TShock.Utils.FindPlayer(to);
      if (players.Count == 1) {
        if (players[0].Group.HasPermission("msg.use"))
          players[0].SendInfoMessage("You have a new message from " + from);
      }
    }

    //help message
    public static void Help(CommandArgs args) {
      args.Player.SendMessage("To send a message use: /msg <player> <message>", Color.Aqua);
      args.Player.SendMessage("To list unread messages use: /msg inbox <page number>", Color.Aqua);
      args.Player.SendMessage("To list all messages use: /msg list <page number>", Color.Aqua);
      args.Player.SendMessage("To read a specific message use: /msg read <id>", Color.Aqua);
      args.Player.SendMessage("To delete a message use: /msg del <id>", Color.Aqua);
    }

    // Run Message command
    public static void Msg(CommandArgs args) {
      string cmd = "help";

      if (args.Parameters.Count > 0) {
        cmd = args.Parameters[0].ToLower();
      }

      switch (cmd) {
        case "help": {
            //return help
            Help(args);

            break;
          }

        //list of all unread messages
        case "inbox": {
            // Fetch all unread messages
            List<Message> messages = MDb.GetMessagesType(args.Player.Name, false);
            //How many messages per page
            const int pagelimit = 5;
            //How many messages per line
            const int perline = 1;
            //Pages start at 0 but are displayed and parsed at 1
            int page = 0;


            if (args.Parameters.Count > 1) {
              if (!int.TryParse(args.Parameters[1], out page) || page < 1) {
                args.Player.SendErrorMessage(string.Format("Invalid page number ({0})", page));
                return;
              }
              page--; //Substract 1 as pages are parsed starting at 1 and not 0
            }
            if (messages.Count == 0) {
              args.Player.SendErrorMessage("You don't have any messages.");
              return;
            }

            //Check if they are trying to access a page that doesn't exist.
            int pagecount = messages.Count / pagelimit;
            if (page > pagecount) {
              args.Player.SendErrorMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1));
              return;
            }

            //Display the current page and the number of pages.
            args.Player.SendSuccessMessage(string.Format("Inbox ({0}/{1}):", page + 1, pagecount + 1));

            //Add up to pagelimit names to a list
            var messageslist = new List<string>();
            for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < messages.Count; i++) {
              messageslist.Add("[" + messages[i].ID + "]" + " " + messages[i].MailFrom + " (" + messages[i].Date + ") [" + (messages[i].Seen ? "Seen" : "Unseen") + "]");
            }

            //convert the list to an array for joining
            var lines = messageslist.ToArray();
            for (int i = 0; i < lines.Length; i += perline) {
              args.Player.SendInfoMessage(string.Join(", ", lines, i, Math.Min(lines.Length - i, perline)));
            }

            if (page < pagecount) {
              args.Player.SendInfoMessage(string.Format("Type /msg inbox {0} for more unread messages.", (page + 2)));
            }

            //remove all messages
            int count = Messages.Count;
            Messages.RemoveRange(0, count);

            break;
          }

        //list of all messages
        case "list": {
            // Fetch all messages
            List<Message> messages = MDb.GetMessages(args.Player.Name);

            //How many messages per page
            const int pagelimit = 5;
            //How many messages per line
            const int perline = 1;
            //Pages start at 0 but are displayed and parsed at 1
            int page = 0;


            if (args.Parameters.Count > 1) {
              if (!int.TryParse(args.Parameters[1], out page) || page < 1) {
                args.Player.SendErrorMessage(string.Format("Invalid page number ({0})", page));
                return;
              }
              page--; //Substract 1 as pages are parsed starting at 1 and not 0
            }

            if (messages.Count == 0) {
              args.Player.SendErrorMessage("You don't have any messages.");
              return;
            }

            //Check if they are trying to access a page that doesn't exist.
            int pagecount = messages.Count / pagelimit;
            if (page > pagecount) {
              args.Player.SendErrorMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1));
              return;
            }

            //Display the current page and the number of pages.
            args.Player.SendSuccessMessage(string.Format("List messages ({0}/{1}):", page + 1, pagecount + 1));

            //Add up to pagelimit names to a list
            var messageslist = new List<string>();
            for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < messages.Count; i++) {
              messageslist.Add("[" + messages[i].ID + "]" + " " + messages[i].MailFrom + " (" + messages[i].Date + ") [" + (messages[i].Seen ? "Seen'" : "Unseen") + "]");
            }

            //convert the list to an array for joining
            var lines = messageslist.ToArray();
            for (int i = 0; i < lines.Length; i += perline) {
              args.Player.SendInfoMessage(string.Join(", ", lines, i, Math.Min(lines.Length - i, perline)));
            }

            if (page < pagecount) {
              args.Player.SendInfoMessage(string.Format("Type /msg list {0} for more messages.", (page + 2)));
            }

            //remove all messages
            int count = Messages.Count;
            Messages.RemoveRange(0, count);

            break;
          }

        //read a specific message
        case "read": {
            if (args.Parameters.Count > 1) {
              int id = int.Parse(args.Parameters[1]);
              Message msg = MDb.GetMessage(id, args.Player.Name);
              if (msg != null) {

                args.Player.SendMessage(msg.ID + ": On " + msg.Date + ", " + msg.MailFrom + " wrote:", Color.Aqua);
                args.Player.SendMessage(msg.MailText, Color.White);

                //set message to read
                MDb.MessageRead(msg.ID);
              }
              else {
                args.Player.SendErrorMessage("Message with the ID \"" + args.Parameters[1].ToString() + "\" does not exist.");
              }
            }
            else {
              args.Player.SendErrorMessage("You must specify an ID.");
            }

            break;
          }

        //delete specify messages
        case "del": {
            if (args.Parameters.Count > 1) {
              //switch args [id, unread, read, all]
              switch (args.Parameters[1].ToString()) {
                case "all": {
                    if (MDb.DelAllMessages(args.Player.Name)) {
                      args.Player.SendErrorMessage("All messages were deleted.");
                    }
                    else {
                      args.Player.SendErrorMessage("You don't have any messages.");
                    }


                    break;
                  }
                case "read": {
                    if (MDb.DelMessagesType(args.Player.Name, true)) {
                      args.Player.SendErrorMessage("All read messages were deleted.");
                    }
                    else {
                      args.Player.SendErrorMessage("You don't have any read messages.");
                    }

                    break;
                  }

                case "unread": {
                    if (MDb.DelMessagesType(args.Player.Name, false)) {
                      args.Player.SendErrorMessage("All unread messages were deleted.");
                    }
                    else {
                      args.Player.SendErrorMessage("You don't have any unread messages.");
                    }

                    break;
                  }

                default: {
                    int id = int.Parse(args.Parameters[1]);
                    if (MDb.DelMessage(id, args.Player.Name)) {
                      args.Player.SendErrorMessage("Message with ID \"" + args.Parameters[1].ToString() + "\" was deleted.");
                    }
                    else {
                      args.Player.SendErrorMessage("Message with ID \"" + args.Parameters[1].ToString() + "\" does not exist.");
                    }

                    break;
                  }
              }
            }
            else {
              args.Player.SendErrorMessage("You must set second parameter [id, all, unseen, seen]");
            }
            break;
          }

        //send message
        default: {
            if (args.Parameters.Count > 1) {
              User userPlr = TShock.Users.GetUserByName(args.Parameters[0]);

              if (userPlr == null) {
                args.Player.SendErrorMessage("Player " + args.Parameters[0] + " could not be found.");
              } else {
                string mailTo = args.Parameters[0];
                SendMessage(mailTo, args.Player.Name, String.Join(" ", args.Parameters.GetRange(1, args.Parameters.Count - 1)));

                args.Player.SendSuccessMessage("You sent a message to " + mailTo);
              }
            }
            else {
              //return help
              Help(args);
            }

            break;
          }
      }
    }
  }
}
