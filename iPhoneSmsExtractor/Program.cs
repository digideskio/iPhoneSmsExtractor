using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SQLite;

namespace iPhoneSmsExtractor
{
    class Program
    {
        const string smsDbFileName = @"3d0d7e5fb2ce288813306e4d4636395e047a3d28";
        const string topLevelFolderFormat = @"c:\users\{0}\AppData\Roaming\Apple Computer\MobileSync\Backup\";
        
        static void Main(string[] args)
        {
            var topLevelFolderPath = string.Format(topLevelFolderFormat, Environment.UserName);

            var backups = Directory.GetDirectories(topLevelFolderPath);

            Console.WriteLine("Choose your backup:");
            for(int i = 0; i < backups.Length; i++)
            {
                var lastWriteTime = Directory.GetLastWriteTime(backups[i]);
                Console.WriteLine("  {0}. {1}", i, lastWriteTime);
            }

            var choice = int.Parse(Console.ReadLine());
            var myBackup = backups[choice];

            var fullPathToDb = Path.Combine(Path.Combine(topLevelFolderPath, myBackup), smsDbFileName);
            var connectionString = string.Format("Data Source={0}", fullPathToDb);

            var groupDiscoverySql = @"SELECT address FROM group_member";

            var contacts = new List<string>();
            Console.WriteLine("Choose from available contacts:");
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var contactIndex = 0;
                
                using (var command = new SQLiteCommand(groupDiscoverySql, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine("  {0}. {1}", contactIndex + 1, reader["address"]);
                        contacts.Add(reader["address"] as string);
                        contactIndex++;
                    }
                }

                connection.Close();
            }

            var contactAddress = int.Parse(Console.ReadLine()) - 1;
            var contact = contacts[contactAddress];
            
            var sql = string.Format(@"SELECT * FROM message WHERE madrid_handle LIKE '%{0}%' OR address LIKE '%{0}%'", contact);

            var outputFilePath = @"sms.html";

            using(var stream = File.OpenWrite(outputFilePath))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("<HEAD>");
                writer.WriteLine("  <BODY>");
                writer.WriteLine("    <TABLE>");
                writer.WriteLine("      <TR>");
                writer.WriteLine("        <TH>Name</TH>");
                writer.WriteLine("        <TH>Timestamp</TH>");
                writer.WriteLine("        <TH>Message</TH>");
                writer.WriteLine("      </TR>");

                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand(sql, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            writer.WriteLine("      <TR>");

                            if (reader["madrid_flags"].ToString() == "36869" || reader["madrid_flags"].ToString() == "102405" || reader["flags"].ToString().Contains("3"))
                            {
                                writer.WriteLine("        <TD>Sent</TD>");
                            }
                            else if (reader["madrid_flags"].ToString() == "12289" || reader["madrid_flags"].ToString() == "77825" || reader["flags"].ToString() == "2")
                            {
                                writer.WriteLine("        <TD>Received</TD>");
                            }
                            else
                            {
                                writer.WriteLine("        <TD>***** " + reader["madrid_flags"] + " / " + reader["flags"] + "</TD>");
                            }

                            writer.Write("        <TD>");
                            var address = reader["address"].ToString();
                            if (!string.IsNullOrEmpty(address))
                            {
                                var baseDate = new DateTime(1970, 1, 1, 0, 0, 0);
                                var messageTimestamp = baseDate.AddSeconds(reader.GetInt64(2));
                                writer.Write(messageTimestamp.ToString());
                            }
                            else
                            {
                                var baseDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                                var messageTimestamp = baseDate.AddSeconds(reader.GetInt64(2));
                                writer.Write(messageTimestamp.ToString());
                            }

                            writer.WriteLine("</TD>");
                            
                            writer.WriteLine("        <TD>" + reader["text"] + "</TD>");
                            
                            writer.WriteLine("      </TR>");
                        }
                    }

                    connection.Close();
                }

                writer.WriteLine("    </TABLE>");
                writer.WriteLine("  </BODY>");
                writer.WriteLine("</HEAD>");
            }
            Console.WriteLine("All done!");
            Console.ReadLine();
        }
    }
}
