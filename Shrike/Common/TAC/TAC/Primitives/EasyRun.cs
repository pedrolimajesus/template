using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AppComponents.Primitives
{
    public class RunProgramException : ApplicationException
    {
        public int StatusCode { get; set; }

        public RunProgramException()
        {
        }

        public RunProgramException(string msg)
            : base(msg)
        {
        }


    }

    public static class EasyRun
    {

        public static string Go(Uri path, string processFile, Uri working, params string[] args)
        {
            var p = new Process();
            var furi = new Uri(path, processFile);
            p.StartInfo.FileName = furi.AbsolutePath;
            p.StartInfo.Arguments = string.Join(" ", args);
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            p.StartInfo.WorkingDirectory = working.AbsolutePath;
            p.StartInfo.UseShellExecute = false;


            p.Start();


            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                var ex = new RunProgramException(p.StandardError.ReadToEnd());
                ex.StatusCode = p.ExitCode;
                throw ex;
            }

            return p.StandardOutput.ReadToEnd();

        }

        public static string Go(string processFile, params string[] args)
        {
            var working = new Uri(Directory.GetCurrentDirectory());
            return Go(working, processFile, working, args);
        }


        private static string[] _noStrings = { };
        public static string[] Args(params object[] nameValuePairs)
        {
            if (null == nameValuePairs || nameValuePairs.Length < 2)
                return _noStrings;

            var args = new List<string>();

            for (var each = 1; each < nameValuePairs.Length; each += 2)
            {
                var name = nameValuePairs[each - 1].ToString();
                var val = nameValuePairs[each].ToString();

                args.Add(name + " " + val);
            }

            return args.ToArray();

        }


    }
}
