using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Management;

class Program
{
    static void Main()
    {
        Console.Title = "Laptop Gaming Optimizer Console";
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Laptop Gaming Optimizer ===");
            Console.WriteLine("1. Show System Info");
            Console.WriteLine("2. Clean RAM (Empty Standby Memory)");
            Console.WriteLine("3. Set Power Plan to High Performance");
            Console.WriteLine("4. Kill Background Apps (simple)");
            Console.WriteLine("5. Exit");
            Console.Write("Choose an option: ");

            var input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    ShowSystemInfo();
                    break;
                case "2":
                    CleanRAM();
                    break;
                case "3":
                    SetHighPerformancePowerPlan();
                    break;
                case "4":
                    KillBackgroundApps();
                    break;
                case "5":
                    Console.WriteLine("Exiting...");
                    return;
                default:
                    Console.WriteLine("Invalid choice, try again.");
                    break;
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    static void ShowSystemInfo()
    {
        Console.WriteLine("\n-- System Info --");
        Console.WriteLine(string.Format("CPU: {0}", GetCpuName()));
        Console.WriteLine(string.Format("Total RAM: {0} MB", GetTotalRAM()));
        Console.WriteLine(string.Format("Available RAM: {0} MB", GetAvailableRAM()));
        Console.WriteLine(string.Format("GPU: {0}", GetGpuName()));
    }

    static string GetCpuName()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("select Name from Win32_Processor"))
            {
                foreach (var item in searcher.Get())
                {
                    return item["Name"] != null ? item["Name"].ToString() : "Unknown CPU";
                }
            }
        }
        catch { }
        return "Unknown CPU";
    }

    static ulong GetTotalRAM()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize from Win32_OperatingSystem"))
            {
                foreach (var item in searcher.Get())
                {
                    return ((ulong)item["TotalVisibleMemorySize"]) / 1024;
                }
            }
        }
        catch { }
        return 0;
    }

    static ulong GetAvailableRAM()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("select FreePhysicalMemory from Win32_OperatingSystem"))
            {
                foreach (var item in searcher.Get())
                {
                    return ((ulong)item["FreePhysicalMemory"]) / 1024;
                }
            }
        }
        catch { }
        return 0;
    }

    static string GetGpuName()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("select Name from Win32_VideoController"))
            {
                foreach (var item in searcher.Get())
                {
                    return item["Name"] != null ? item["Name"].ToString() : "Unknown GPU";
                }
            }
        }
        catch { }
        return "Unknown GPU";
    }

    static void CleanRAM()
{
        Console.WriteLine("\nCleaning RAM by emptying standby list...");
        bool result = EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        if (result)
            Console.WriteLine("Freed memory from current process.");
        else
            Console.WriteLine("Failed to free memory from current process.");

        // Clear standby memory using empty working set on all processes (requires admin)
        var allProcs = Process.GetProcesses();
        int freedCount = 0;
        foreach (var proc in allProcs)
        {
            try
            {
                // Skip system processes (usually SessionId == 0)
                if (proc.SessionId == 0)
                    continue;

                if (EmptyWorkingSet(proc.Handle))
                {
                    Console.WriteLine(string.Format("Freed memory from process: {0} (PID: {1})", proc.ProcessName, proc.Id));
                    freedCount++;
                }
            }
            catch 
            {
                // Ignoring processes we can't access
            }
        }
        Console.WriteLine(string.Format("Successfully freed memory on {0} processes. You may need to run as Administrator.", freedCount));
    }

    [DllImport("psapi.dll")]
    static extern bool EmptyWorkingSet(IntPtr hProcess);

    static void SetHighPerformancePowerPlan()
{
        Console.WriteLine("\nSetting Power Plan to High Performance...");
        try
        {
            var startInfo = new ProcessStartInfo("powercfg", "/setactive SCHEME_MIN")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
            }
            Console.WriteLine("Power Plan set to High Performance.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(string.Format("Failed to set power plan: {0}", ex.Message));
        }
    }

    static void KillBackgroundApps()
    {
        Console.WriteLine("\nKilling common background apps...");
        string[] heavyApps = new string[]
        {
            "OneDrive",
            "Dropbox",
            "Spotify",
            "Discord",
            "Teams",
            "Edge",
        };

        int killed = 0;
        foreach (var proc in Process.GetProcesses())
        {
            try
            {
                if (heavyApps.Any(app => proc.ProcessName.IndexOf(app, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    proc.Kill();
                    Console.WriteLine(string.Format("Killed {0}", proc.ProcessName));
                    killed++;
                }
            }
            catch { }
        }
        Console.WriteLine(string.Format("Killed {0} processes. Use with caution.", killed));
    }
}
