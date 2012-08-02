using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.IO;

namespace iPhoneSmsExtractor
{
    public class DatabaseAccess : IDisposable
    {
        private const string connectionStringFormat = "Data Source={0}";
        private SQLiteConnection connection;

        public string ConnectionString
        {
            get;
            private set;
        }

        public bool IsOpen
        {
            get { return connection != null && connection.State == System.Data.ConnectionState.Open; }
        }

        public DatabaseAccess(string pathToFile)
        {
            if (!File.Exists(pathToFile))
                throw new FileNotFoundException(pathToFile);

            ConnectionString = string.Format(connectionStringFormat, pathToFile);
        }

        public void Open()
        {
            connection = new SQLiteConnection(ConnectionString);
            connection.Open();
        }

        public void Dispose()
        {
            if (connection != null)
                connection.Close();

            connection = null;
        }

        public List<string> GetContacts()
        {
            if (!IsOpen)
                throw new InvalidOperationException("Database connection must be opened.");

            var contactDiscoverySql = @"SELECT DISTINCT COALESCE(madrid_handle, '') || COALESCE(address, '') AS contact FROM message";

            var retval = new List<string>();

            using (var command = new SQLiteCommand(contactDiscoverySql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    retval.Add(reader["contact"] as string);
                }
            }

            return retval;
        }

        public List<Message> GetMessages(string contact, int utcOffset)
        {
            var sql = string.Format(@"SELECT * FROM message WHERE madrid_handle LIKE '%{0}%' OR address LIKE '%{0}%'", contact);

            var retval = new List<Message>();

            using (var command = new SQLiteCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var address = reader["address"].ToString();
                    DateTime messageTimestamp;
                    if (!string.IsNullOrEmpty(address))
                    {
                        var baseDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        messageTimestamp = baseDate.AddSeconds(reader.GetInt64(2)).AddHours(utcOffset);
                    }
                    else
                    {
                        var baseDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        messageTimestamp = baseDate.AddSeconds(reader.GetInt64(2)).AddHours(utcOffset);
                    }
                    
                    retval.Add(new Message
                    {
                        iMessageFlags = reader["madrid_flags"].ToString(),
                        smsFlags = reader["flags"].ToString(),
                        Text = reader["text"].ToString(),
                        Timestamp = messageTimestamp
                    });
                }
            }

            return retval;
        }
    }
}
