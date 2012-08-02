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
            for (int i = 0; i < backups.Length; i++)
            {
                var lastWriteTime = Directory.GetLastWriteTime(backups[i]);
                Console.WriteLine("  {0}. {1}", i + 1, lastWriteTime);
            }

            var choice = int.Parse(Console.ReadLine()) - 1;
            var myBackup = backups[choice];

            var fullPathToDb = Path.Combine(Path.Combine(topLevelFolderPath, myBackup), smsDbFileName);
            
            Console.WriteLine("Enter your timezone offset from UTC:");
            var utcOffset = int.Parse(Console.ReadLine());

            var outputFilePath = @"sms.html";

            if (File.Exists(outputFilePath))
                File.Delete(outputFilePath);

            using (var stream = File.OpenWrite(outputFilePath))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("<HTML>");
                writer.WriteLine("  <HEAD>");
                writer.WriteLine("    <META CHARSET=\"utf-8\" />");
                writer.WriteLine("    <TITLE>SMS and iMessages</TITLE>");
                writer.WriteLine("  </HEAD>");
                writer.WriteLine("  <BODY>");
                writer.WriteLine("    <TABLE>");
                writer.WriteLine("      <TR>");
                writer.WriteLine("        <TH>Name</TH>");
                writer.WriteLine("        <TH>Timestamp</TH>");
                writer.WriteLine("        <TH>Message</TH>");
                writer.WriteLine("      </TR>");

                using (var database = new DatabaseAccess(fullPathToDb))
                {
                    database.Open();

                    Console.WriteLine("Choose from available contacts:");
                    var contacts = database.GetContacts();
                    contacts.ForEach(i => Console.WriteLine("  {0}. {1}", contacts.IndexOf(i) + 1, i));
                    var contactAddress = int.Parse(Console.ReadLine()) - 1;
                    var contact = contacts[contactAddress];
                    
                    var messages = database.GetMessages(contact, utcOffset);

                    foreach (var message in messages)
                    {
                        writer.WriteLine("      <TR>");
                        writer.WriteLine("        <TD>" + (message.Sent ? "Sent" : "Received") + "</TD>");
                        writer.WriteLine("        <TD>" + message.Timestamp.ToString() + "</TD>");
                        writer.WriteLine("        <TD>" + message.Text + "</TD>");
                        writer.WriteLine("      </TR>");

                    }
                }

                writer.WriteLine("    </TABLE>");
                writer.WriteLine("  </BODY>");
                writer.WriteLine("</HTML>");
            }
            Console.WriteLine("All done!");
            Console.ReadLine();
        }
    }
}
