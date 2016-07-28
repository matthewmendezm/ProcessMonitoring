# ProcessMonitoring #
### SUMMARY ###

Reports software usage info from the host machine to a specified database.

### STRUCTURE ###
**ProcessMonitoring**: Defines the following objects:
1. ProcessMonitor.cs: A ProcessMonitor listens for terminating processes on local host (ignoring processes in the specified ignore list file) and fires a ProcessEnded event on each termination. This class is modular: any outside class can listen for a ProcessEnded event.
2. ProcessInfo.cs: The structure that is wrapped up and passed through the ProcessEnded event so the event listener can get info about the terminated process. The structure's fields include computer name, process username, process name, process path, startTime, endTime, and duration (the duration).

**PMDatabaseUpdate.PMDatabaseUpdater.cs**: Takes database info (server, username, password) as constructor parameters. Defines and registers an event handler function to the ProcessMonitor.ProcessEnded event. The handler atomically adds an entry to the associated database for each ended process. The entry is of the form (executable, duration, username, hostname)

**SoftwareUsage.Run.cs**: Contains the main method. Creates a new PMDatabseUpdater. All ended processes not on the ignore list will be entered into the database.

### Run ###
**To run once on a single machine**: Must first fill in the database info into X'd out fields in SoftwareUsage/Run.cs Make sure SoftwareUsage.exe, PMDatabaseUpdate.dll, ProcessMonitoring.dll, MySql.Data.dll, and ignorelist.txt are in the same folder, then run the executable.
