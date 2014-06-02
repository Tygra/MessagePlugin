using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using MessagePlugin;

namespace MessagePlugin
{
    class MDb
    {
		public static string dbpath = Path.Combine(TShock.SavePath, "messages.sqlite");
        public static IDbConnection DB;
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;

        public static void InitMessageDB()
        {
            if (!File.Exists(dbpath))
            {
                SqliteConnection.CreateFile(dbpath);
            }

			DB = new SqliteConnection (string.Format ("uri=file://{0},Version=3", dbpath));
            SQLWriter = new SqlTableCreator(DB, new SqliteQueryCreator());
            SQLEditor = new SqlTableEditor(DB, new SqliteQueryCreator());

            var table = new SqlTable("MessagePlugin",
                new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("mailFrom", MySqlDbType.Text),
                new SqlColumn("mailTo", MySqlDbType.Text),
                new SqlColumn("mailText", MySqlDbType.Text),
                new SqlColumn("Date", MySqlDbType.Text),
                new SqlColumn("Read", MySqlDbType.Int32)
                );

            SQLWriter.EnsureExists(table);
        }
		public static List<Message> GetMessages(string name)
		{
			try
			{
				List<Message> messages = new List<Message>();
				using (var reader = DB.QueryReader("SELECT * FROM 'MessagePlugin' WHERE mailTo = @0;", name))
				{
					while (reader.Read())
					{
						messages.Add(new Message(reader.Get<int>("Id"), reader.Get<string>("mailFrom"), reader.Get<string>("mailTo"), reader.Get<string>("mailText"), reader.Get<string>("date"), reader.Get<bool>("read")));
					}
					return messages;
				}
			}
			catch (Exception ex) {
				Log.Error (ex.ToString ());
			}
			return null;
		}
		public static List<Message> GetMessagesType(string name, bool read)
		{
			try
			{
				List<Message> messages = new List<Message>();
				using (QueryResult reader = DB.QueryReader("SELECT * FROM 'MessagePlugin' WHERE mailTo = @0 AND Read = @1;", name, read?1:0))
				{
					while (reader.Read())
					{
						messages.Add(new Message(reader.Get<int>("Id"), reader.Get<string>("mailFrom"), reader.Get<string>("mailTo"), reader.Get<string>("mailText"), reader.Get<string>("date"), reader.Get<bool>("read")));
					}
					return messages;
				}
			}
			catch (Exception ex) {
				Log.Error (ex.ToString ());
			}
			return null;
		}
		public static bool AddMessage(string from, string to, string text)
		{
			try {
				return DB.Query ("INSERT INTO 'MessagePlugin' (mailFrom, mailTo, mailText, date, read) VALUES (@0, @1, @2, @3, @4);", from, to, text, DateTime.Now.ToString ("g"), 0) != 0;
			} catch (Exception ex) {
				Log.Error (ex.ToString ());
			}
			return false;
		}
		public static bool DelMessage(int id, string name)
		{
			try
			{
				return DB.Query ("DELETE FROM 'MessagePlugin' WHERE Id = @0 AND mailTo = @1;", id, name) != 0;
			}
			catch (Exception ex) {
				Log.Error (ex.ToString ());
			}
			return false;
		}
		public static bool DelAllMessages(string name)
		{
			try {
				return DB.Query ("DELETE FROM 'MessagePlugin' WHERE mailTo = @0;", name) != 0;
			} catch (Exception ex) {
				Log.Error (ex.ToString ());
			}
			return false;
		}
		public static bool DelMessagesType(string name, bool read)
		{
			try {
				return DB.Query ("DELETE FROM 'MessagePlugin' WHERE mailTo = @0 AND read = @1;", name, read?1:0) != 0;
			} catch (Exception ex) {
				Log.Error (ex.ToString ());
			}
			return false;
		}
		public static Message GetMessage(int id, string name)
		{
			try
			{
				using (QueryResult reader = DB.QueryReader("SELECT * FROM 'MessagePlugin' WHERE Id = @0 AND mailTo = @1;", id, name))
				{
					if (reader.Read())
					{
						return new Message(reader.Get<int>("Id"), reader.Get<string>("mailFrom"), reader.Get<string>("mailTo"), reader.Get<string>("mailText"), reader.Get<string>("date"), reader.Get<bool>("read"));
					}
				}
			}
			catch (Exception ex) {
				Log.Error (ex.ToString ());
			}
			return null;
		}
		public static bool MessageRead(int id)
		{
			try
			{
				return DB.Query ("UPDATE 'MessagePlugin' SET read = '1' WHERE Id = @0;", id) != 0;
			}
			catch (Exception ex) {
				Log.Error (ex.ToString ());
			}
			return false;
		}
    }
}