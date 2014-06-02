using System;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using TerrariaApi;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.Hooks;
using TShockAPI.DB;
using System.ComponentModel;

namespace MessagePlugin
{
    public class Message
    {
        public int ID { get; set; }
        public string MailFrom { get; set; }
        public string MailTo { get; set; }
        public string MailText { get; set; }
        public string Date { get; set; }
        public bool Read { get; set; }

        public Message(int id, string mailFrom, string mailTo, string mailText, string date, bool read)
        {
            ID = id;
            MailFrom = mailFrom;   
            MailTo = mailTo;
            MailText = mailText;
            Date = date;
            Read = read;
        }
    }
}