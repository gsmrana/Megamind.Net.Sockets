using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Megamind.Net.Sockets
{
    public static class ProcessPorts
    {
        public static List<ProcessPort> GetProcessPortMap()
        {
            var ProcessPorts = new List<ProcessPort>();

            using (Process Proc = new Process())
            {
                var param = new ProcessStartInfo
                {
                    FileName = "netstat.exe",
                    Arguments = "-a -n -o",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                Proc.StartInfo = param;
                Proc.Start();

                var stdOutput = Proc.StandardOutput;
                var stdError = Proc.StandardError;
                var netStatContent = stdOutput.ReadToEnd() + stdError.ReadToEnd();
                if (Proc.ExitCode != 0) throw new Exception("NetStat command failed. This may require elevated permissions.");

                var netStatRows = Regex.Split(netStatContent, "\r\n");
                foreach (var row in netStatRows)
                {
                    var tokens = Regex.Split(row, "\\s+");
                    if (tokens.Length > 4 && (tokens[1].Equals("UDP") || tokens[1].Equals("TCP")))
                    {
                        var IpEndpoint = Regex.Replace(tokens[2], @"\[(.*?)\]", "1.1.1.1");
                        if (IpEndpoint.Contains("1.1.1.1")) continue;  //remove ipv6
                        ProcessPorts.Add(new ProcessPort
                        {
                            ProcessId = tokens[1] == "UDP" ? Convert.ToInt16(tokens[4]) : Convert.ToInt16(tokens[5]),
                            ProcessName = tokens[1] == "UDP" ? GetProcessName(Convert.ToInt16(tokens[4])) : GetProcessName(Convert.ToInt16(tokens[5])),
                            Protocol = IpEndpoint.Contains("1.1.1.1") ? string.Format("{0}v6", tokens[1]) : string.Format("{0}", tokens[1]),
                            EndPoint = IpEndpoint
                        });
                    }
                }
            }

            return ProcessPorts;
        }

        /// <summary>
        /// Private method that handles pulling the process name (if one exists) from the process id.
        /// </summary>
        /// <param name="ProcessId"></param>
        /// <returns></returns>
        private static string GetProcessName(int ProcessId)
        {
            var procName = "UNKNOWN";
            try
            {
                procName = Process.GetProcessById(ProcessId).ProcessName;
            }
            catch { }
            return procName;
        }
    }

    /// <summary>
    /// A mapping for processes to ports and ports to processes that are being used in the system.
    /// </summary>
    public class ProcessPort
    {
        public int ProcessId { get; set; }

        public string ProcessName { get; set; }

        public string Protocol { get; set; }

        public string EndPoint { get; set; }
    }
}