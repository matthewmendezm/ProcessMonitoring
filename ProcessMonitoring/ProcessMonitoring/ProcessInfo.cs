using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProcessMonitoring
{
    /*
     * A ProcessInfo object encapsulates information about a finished process.
     * This information includes the process's name, path, start time, end time, 
     * duration, which user was running the process and which computer was running
     * the process.
     */
    public class ProcessInfo
    {
        private string compName;
        private string username;
        private string processName;
        private string processPath;
        private DateTime startTime;
        private DateTime endTime;
        private TimeSpan duration;

        // Set up the Properties
        public string CompName { get { return this.compName; } }
        public string Username { get { return this.username; } }
        public string ProcessName { get { return this.processName; } }
        public string ProcessPath { get { return this.processPath; } }
        public DateTime StartTime { get { return this.startTime; } }
        public DateTime EndTime { get { return this.endTime; } }
        public TimeSpan Duration { get { return this.duration; } }

        /*
         * Simple constructor for passing and encapsulating the already created and parsed info.
         */
        public ProcessInfo(string compName, string username, string processName, string processPath, DateTime startTime, DateTime endTime, TimeSpan duration)
        {
            this.compName = compName;
            this.username = username;
            this.processName = processName;
            this.processPath = processPath;
            this.startTime = startTime;
            this.endTime = endTime;
            this.duration = duration;
        }
    }
}
