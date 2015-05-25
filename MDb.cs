using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using MessagePlugin;

namespace MessagePlugin {
  class MDb {
    public static string dbpath = Path.Combine(TShock.SavePath, "messages.sqlite");
    public static IDbConnection DB;
    public static SqlTableEditor SQLEditor;
    public static SqlTableCreator SQLWriter;

    public static void InitMessageDB() {
      if (TShock.Config.StorageType.ToLower() == "sqlite") {
        DB = new SqliteConnection(string.Format("uri=file://{0},Version=3", dbpath));
        if (!File.Exists(dbpath)) {
          SqliteConnection.CreateFile(dbpath);
        }
      }
      else if (TShock.Config.StorageType.ToLower() == "mysql") {
        try {
          var host = TShock.Config.MySqlHost.Split(':');
          DB = new MySqlConnection {
            ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
              host[0],
              host.Length == 1 ? "3306" : host[1],
              TShock.Config.MySqlDbName,
              TShock.Config.MySqlUsername,
              TShock.Config.MySqlPassword
              )
          };
        }
        catch (MySqlException x) {
          TShock.Log.Error(x.ToString());
          throw new Exception("MySQL not setup correctly.");
        }
      }
      else
        throw new Exception("Invalid storage type.");

      SQLWriter = new SqlTableCreator(DB, DB.GetSqlType() == SqlType.Sqlite ?
        (IQueryBuilder)new SqliteQueryCreator()
        : new MysqlQueryCreator());

      SQLEditor = new SqlTableEditor(DB, DB.GetSqlType() == SqlType.Sqlite ?
        (IQueryBuilder)new SqliteQueryCreator()
        : new MysqlQueryCreator());

      var table = new SqlTable("MessagePlugin",
          new SqlColumn("Id", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
          new SqlColumn("mailFrom", MySqlDbType.Text),
          new SqlColumn("mailTo", MySqlDbType.Text),
          new SqlColumn("mailText", MySqlDbType.Text),
          new SqlColumn("Date", MySqlDbType.Text),
          new SqlColumn("Seen", MySqlDbType.Int32)
          );

      SQLWriter.EnsureExists(table);
    }
    public static List<Message> GetMessages(string name) {
      try {
        List<Message> messages = new List<Message>();
        using (var reader = DB.QueryReader("SELECT * FROM MessagePlugin WHERE mailTo = @0;", name)) {
          while (reader.Read()) {
            messages.Add(new Message(reader.Get<int>("Id"), reader.Get<string>("mailFrom"), reader.Get<string>("mailTo"), reader.Get<string>("mailText"), reader.Get<string>("date"), reader.Get<bool>("seen")));
          }
          return messages;
        }
      }
      catch (Exception ex) {
        TShock.Log.Error(ex.ToString());
      }
      return null;
    }
    public static List<Message> GetMessagesType(string name, bool seen) {
      try {
        List<Message> messages = new List<Message>();
        using (QueryResult reader = DB.QueryReader("SELECT * FROM MessagePlugin WHERE mailTo = @0 AND Seen = @1;", name, seen ? 1 : 0)) {
          while (reader.Read()) {
            messages.Add(new Message(reader.Get<int>("Id"), reader.Get<string>("mailFrom"), reader.Get<string>("mailTo"), reader.Get<string>("mailText"), reader.Get<string>("date"), reader.Get<bool>("Seen")));
          }
          return messages;
        }
      }
      catch (Exception ex) {
        TShock.Log.Error(ex.ToString());
      }
      return null;
    }
    public static bool AddMessage(string from, string to, string text) {
      try {
        return DB.Query("INSERT INTO MessagePlugin (mailFrom, mailTo, mailText, date, seen) VALUES (@0, @1, @2, @3, @4);", from, to, text, DateTime.Now.ToString("g"), 0) != 0;
      }
      catch (Exception ex) {
        TShock.Log.Error(ex.ToString());
      }
      return false;
    }
    public static bool DelMessage(int id, string name) {
      try {
        return DB.Query("DELETE FROM MessagePlugin WHERE Id = @0 AND mailTo = @1;", id, name) != 0;
      }
      catch (Exception ex) {
        TShock.Log.Error(ex.ToString());
      }
      return false;
    }
    public static bool DelAllMessages(string name) {
      try {
        return DB.Query("DELETE FROM MessagePlugin WHERE mailTo = @0;", name) != 0;
      }
      catch (Exception ex) {
        TShock.Log.Error(ex.ToString());
      }
      return false;
    }
    public static bool DelMessagesType(string name, bool seen) {
      try {
        return DB.Query("DELETE FROM MessagePlugin WHERE mailTo = @0 AND seen = @1;", name, seen ? 1 : 0) != 0;
      }
      catch (Exception ex) {
        TShock.Log.Error(ex.ToString());
      }
      return false;
    }
    public static Message GetMessage(int id, string name) {
      try {
        using (QueryResult reader = DB.QueryReader("SELECT * FROM MessagePlugin WHERE Id = @0 AND mailTo = @1;", id, name)) {
          if (reader.Read()) {
            return new Message(reader.Get<int>("Id"), reader.Get<string>("mailFrom"), reader.Get<string>("mailTo"), reader.Get<string>("mailText"), reader.Get<string>("date"), reader.Get<bool>("seen"));
          }
        }
      }
      catch (Exception ex) {
        TShock.Log.Error(ex.ToString());
      }
      return null;
    }
    public static bool MessageRead(int id) {
      try {
        return DB.Query("UPDATE MessagePlugin SET seen = '1' WHERE Id = @0;", id) != 0;
      }
      catch (Exception ex) {
        TShock.Log.Error(ex.ToString());
      }
      return false;
    }
  }
}