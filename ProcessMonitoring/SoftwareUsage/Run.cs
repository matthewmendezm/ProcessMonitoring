using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMDatabaseUpdate;
using ProcessMonitoring;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Diagnostics;


namespace SoftwareUsage
{
    class Run
    {
        /*
         * Gets a pointer to the console window object
         */
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        /*
         * Shows or hides the given window 
         */
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        static void Main(String[] args)
        {

            string mysqlServer = "xxx.xxx.xxx.xxx";
            string softwareUsageTableName = "xxxxxxxxxxx";
            string username = "xxxxxxxxxxxxxx";
            string password = "xxxxxxxxxxxx";


            // Make sure another instance of SoftwareUsage is not running.
            int count = 0;
            Process[] processes = Process.GetProcesses();
            foreach (Process process in processes)
                if (process.ProcessName == "SoftwareUsage")
                    count++;

            if (count > 1)
               return;

            string[] ignoreList = {};

            // Attempt reading the ignore list from file and expanding it into a single string variable.
            try
            {
                ignoreList = System.IO.File.ReadAllLines(@"C:\Program Files (x86)\Software Usage\ignoreList.txt");
            }
            catch (Exception) { }

            // The executable will not be readable to users because of how the program will be installed on the lab computers so
            // no need to worry about putting authentication credentials directly in source code (usually users could take the exe and 
            // use a program to reverse engineer it back into source code)
            new PMDatabaseUpdater(mysqlServer, softwareUsageTableName, username, password, ignoreList);

            ShowWindow(GetConsoleWindow(), 0);
            
            Console.Read(); // Allows the program to continue running until killed.   
        }
    }
}
