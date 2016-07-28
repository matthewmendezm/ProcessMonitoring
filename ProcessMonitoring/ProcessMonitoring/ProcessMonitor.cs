using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Timers;

namespace ProcessMonitoring
{

    public delegate void ProcessEndedEventHandler(object sender, ProcessEndedEventArgs e);

    /*
     * EventArgs child used for passing a ProcessInfo object to a ProcessEndedEventHandler.
     */
    public class ProcessEndedEventArgs : EventArgs
    {
        public ProcessInfo processInfo;

        /*
         * Constructor: Sets processInfo field.
         */
        public ProcessEndedEventArgs(ProcessInfo processInfo)
        {
            this.processInfo = processInfo;
        }
    }

    /*
     * A ProcessMonitor object monitors process usage on the local host.
     * When a process ends, information about that process is passed into
     * a ProcessInfo object and the ProcessEndedEventHandler is fired.
     */
    public class ProcessMonitor
    {
        string[] ignoreList; // Array of .exe files that should be ignored in monitoring.
        public event ProcessEndedEventHandler ProcessEnded; // Fired when a process ends.
        private String redundantUsername; // Used when attempting to determine the username associated with a process.
        System.Timers.Timer timer; // Used for updating redundant username

       /*
        * ProcessMonitor Constructor:
        * Takes a string array which should be a list of all .exe files to be 
        * ignored in the monitoring process.
        */
        public ProcessMonitor(string[] ignoreList)
        {
            this.ignoreList = ignoreList;
            this.redundantUsername = "";

            createComputerListener();

            timer = new System.Timers.Timer(30000);
            
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        /*
         * When fired, updates the redundantUsername field with computer's currently logged in user.
         */
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            var objectQuery = new ObjectQuery( "SELECT * FROM Win32_Process WHERE Name = 'explorer.exe'");
            var searcherEntries = new ManagementObjectSearcher(objectQuery).Get();
            foreach (ManagementObject manageOb in searcherEntries)
            {
                string[] ownerInfo = new string[2];
                manageOb.InvokeMethod("GetOwner", (object[])ownerInfo);
                if (ownerInfo[1] == "USERS")
                {
                    redundantUsername = ownerInfo[0];
                    Console.WriteLine(redundantUsername);
                    break;
                }
            }
        }
       
       /*
        * Starts up a new ManagementEventWatcher monitoring
        * the local host's processes.
        */
        private void createComputerListener()
        {
            // The query will tell WMI that it wants to be notified each time a process has ended. The polling time
            // is set to 60 seconds so that too much overhead activity is avoided but the polls are accurate within
            // one minute.
            string queryString = "SELECT * FROM __InstanceDeletionEvent WITHIN 90 WHERE TargetInstance ISA 'Win32_Process'";
            string scope = @"\\.\root\cimv2";

            // Create the watcher and start to listen for events specified by the query string.
            ManagementEventWatcher watcher = new ManagementEventWatcher(scope, queryString);
            watcher.EventArrived += new EventArrivedEventHandler((sender, e) => ProcessOperationEvent(sender, e));
            watcher.Start();
        }

       /*
        * When EventArrivedEventHandler is fired, this function is called which
        * parses EventArrivedEventArgs into the process's CreationDate, Name, 
        * and ExecutablePath. It also calculates the duration of the process and 
        * finds what user and computer the process was being ran under. Finally, 
        * a ProcessInfo object is created and the ProcessCreated event is fired.
        */
        private void ProcessOperationEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject baseObject = (ManagementBaseObject)e.NewEvent;

            string processName = (string)((ManagementBaseObject)baseObject["TargetInstance"])["Name"];

            if (ignoreList.Contains(processName) || ProcessEnded == null) 
                return; // Do not proceed with the notification process if the process is on the ignore list.

            string compName = System.Net.Dns.GetHostName();  

            string processPath = (string)((ManagementBaseObject)baseObject["TargetInstance"])["ExecutablePath"];
            
            UInt32 PID = (UInt32)((ManagementBaseObject)baseObject["TargetInstance"])["ProcessId"];
            UInt32 ParentPID = (UInt32)((ManagementBaseObject)baseObject["TargetInstance"])["ParentProcessId"];

            if (processPath == null)
                return;

            // Three levels of redundancy for fetching the username (different processes behave differently
            // in terms of usernames). First attempt tries to get the username associated with the process itself,
            // second attempt tries to get the username associated with the processes' parent, and the third
            // uses the redundantUsername variable which fetches the logged in system user every few seconds
            // on a different thread.
            string username;
            username = GetUsernameByPID(Convert.ToInt32(PID));
            if (username == String.Empty)
                GetUsernameByPID(Convert.ToInt32(ParentPID));     
 
            if (username == String.Empty || username.Contains("NT AUTHORITY"))
                username = redundantUsername;

            if (username == String.Empty || username.Contains("NT AUTHORITY"))
                username = "UNKNOWN";

            if (username == "SYSTEM")
                return;
            else
                username = username.ToLower();

            // Parse out the start and end times and determine the duration of the ended process.
            string creationDate = (string)((ManagementBaseObject)baseObject["TargetInstance"])["CreationDate"];
            DateTime startTime = generateDateTimeFromCreationDate(creationDate);
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime; 

            // Encapsulate the process info into a ProcessInfo object and trigger the processended event.
            ProcessInfo processInfo = new ProcessInfo(compName, username, processName, processPath, startTime, endTime, duration);
            ProcessEnded(this, new ProcessEndedEventArgs(processInfo));
        }

        /*
         * Parses the CreationDate string from the WMI query into a DateTime object.
         * The CreationDate string appears as follows: 20020710113047.000000420
         * 4 digit year, 2 digit month, 2 digit day, 2 digit hour, 2 digit minute,
         * 2 digit second.
         */
        private DateTime generateDateTimeFromCreationDate(string creationDate)
        {
            int year = Int32.Parse(creationDate.Substring(0, 4));
            int month = Int32.Parse(creationDate.Substring(4, 2));
            int day = Int32.Parse(creationDate.Substring(6, 2));
            int hour = Int32.Parse(creationDate.Substring(8, 2));
            int minute = Int32.Parse(creationDate.Substring(10, 2));
            int second = Int32.Parse(creationDate.Substring(12, 2));
            return new DateTime(year, month, day, hour, minute, second);
        }

        
        /*
         * Returns the username associated with the give process id.
         */
        private string GetUsernameByPID(int pid)
        {
            try
            {
                ObjectQuery query = new ObjectQuery ("Select * from Win32_Process Where ProcessID = '" + pid + "'");
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                if (searcher.Get().Count == 0)
                    return String.Empty;

                foreach (ManagementObject results in searcher.Get())
                {
                    string[] buf = new String[2];
                    results.InvokeMethod("GetOwner", (object[])buf);  
                    return (buf[0] == null) ? String.Empty : buf[0];
                }
            } catch {}
            return String.Empty;
        }
    }
}
