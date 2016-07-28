using MySql.Data.MySqlClient;
using ProcessMonitoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace PMDatabaseUpdate
{
    /*
     * This class uses the ProcessMonitor class to monitor a computer or multiple
     * computer's processes and send the process information to a specified MySql
     * database.
     */
    public class PMDatabaseUpdater
    {
        private string server;
        private string database;
        private string passwd;
        private string uid;

        /*
         * Creates a PMDatabaseUpdater with the specified credentials
         * passed as parameters. The ignoreList parameter should be 
         * a space separated string of all the .exe file names that 
         * are to be ignored during process monitoring.
         */
        public PMDatabaseUpdater(string server, string database, string passwd, string uid, string[] ignoreList)
        {
            this.server = server;
            this.database = database;
            this.passwd = passwd;
            this.uid = uid;
            ProcessMonitor processMonitor = new ProcessMonitor(ignoreList);
            processMonitor.ProcessEnded += new ProcessEndedEventHandler(ProcessEndedEvent); // Register the event to ProcessEndedEvent
        }

        /*
         * Fired when a ProcessEndedEventHandler has been fired. Information is
         * extracted form the event arguments and sent to the database. The information
         * sent to the database is the process's name, path, duration in minutes, username
         * of the user who ran the process, and computer name from which the process was run.
         */
        private void ProcessEndedEvent(object sender, ProcessEndedEventArgs e)
        {
            ProcessInfo processInfo = e.processInfo; // processInfo holds encapsulated information about the process.
            string processName = processInfo.ProcessName;
            string processPath = processInfo.ProcessPath;
            int minutes = Convert.ToInt32(processInfo.Duration.TotalMinutes);
            string username = processInfo.Username;
            string computer = processInfo.CompName;

            string myConnectionString = "server=" + server.Trim() + ";uid=" + uid.Trim() + ";pwd=" + passwd.Trim() + ";database=" + database.Trim() + "";
            using (MySql.Data.MySqlClient.MySqlConnection conn = new MySql.Data.MySqlClient.MySqlConnection(myConnectionString))
            {
                try
                {
                    conn.Open();
                    string query = "INSERT INTO " + database + " (executable, path, duration, username, hostname) VALUES (@executable, @path, @duration, @username, @hostname);";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@executable", processName);
                        cmd.Parameters.AddWithValue("@path", processPath);
                        cmd.Parameters.AddWithValue("@duration", minutes);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@hostname", computer);
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }
                catch (MySqlException) { return; };
            }    
        } 
    }
}
