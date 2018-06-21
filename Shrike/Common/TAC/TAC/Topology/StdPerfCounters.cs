using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppComponents.Topology
{
    public static class StdPerfCounters
    {
        private const string Memory = "Memory";
        private const string ASPCategory = "ASP.NET Apps v4.0.30319";
        private const string Total = "__Total__";
        private const string Global = "_Global_";
        

        public static void LoadCLRPerfCounters(IPerfCounterSpec spec)
        {
            spec.AddCategory(Resource.StdPerfCounters_LoadCLRPerfCounters_Application_Performance)
                .AddMetric(Resource.StdPerfCounters_LoadCLRPerfCounters_Available_Memory, Memory, "Available MBytes", null, "{0} MB")
                .AddMetric(Resource.StdPerfCounters_LoadCLRPerfCounters_CPU_Usage, "Processor", "% Processor Time", "_Total", "{0:F2}%")
                .AddMetric(Resource.StdPerfCounters_LoadCLRPerfCounters_System_Uptime__seconds_, "System", "System Up Time", null, "{0} seconds")
                .AddMetric(Resource.StdPerfCounters_LoadCLRPerfCounters_Locks_and_Thread_Contention, ".NET CLR LocksAndThreads", "Contention Rate / sec", Global, @"{0:F2} / s")
                .AddMetric(Resource.StdPerfCounters_LoadCLRPerfCounters_Memory_Usage, ".NET CLR Memory", "# Bytes in all Heaps", Global, "{0} bytes")
                .AddMetric(Resource.StdPerfCounters_LoadCLRPerfCounters_Memory_Cleanup_Time__, ".NET CLR Memory", "% Time in GC", Global, "{0:F2}%")
                .AddMetric(Resource.StdPerfCounters_LoadCLRPerfCounters_Errors___sec, ".NET CLR Exceptions", "# of Exceps Thrown / sec", Global, @"{0:F2} / s");
        }

        public static void LoadASPPerfCounters(IPerfCounterSpec spec)
        {
            spec.AddCategory(Resource.StdPerfCounters_LoadASPPerfCounters_Web_Performance)
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Bytes_Received, ASPCategory, "Request Bytes In Total", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Bytes_Sent, ASPCategory, "Request Bytes Out Total", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Request_Execution_Time, ASPCategory, "Request Execution Time", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Disconnected_Requests, ASPCategory, "Requests Disconnected", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Requests_in_Execution, ASPCategory, "Requests Executing", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Queued_Requests, ASPCategory, "Requests In Application Queue", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Rejected_Requests, ASPCategory, "Requests Rejected", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Completed_Requests, ASPCategory, "Requests Succeeded", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Timed_Out_Requests, ASPCategory, "Requests Timed Out", Total, "{0}")
                .AddMetric(Resource.StdPerfCounters_LoadASPPerfCounters_Error_Rate, ASPCategory, "Errors Total/Sec", Total, @"{0:F2} / s");


        }
    }
}
