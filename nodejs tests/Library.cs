using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Text.RegularExpressions;

namespace nodejs_tests
{
    class Library
    {
        public static List<string> stringParse(string String)
        {
            List<string> strArr = new List<string>();
            foreach (Match match in Regex.Matches(String, "\"[^\"]*\""))
            {
                strArr.Add(match.Value.Trim('"'));
            }
            return strArr;
        }
        public static string PathToURI(string String)
        {
            Uri uri = new Uri(String);
            return uri.AbsoluteUri;
        }
        public static void WriteToLog(string String)
        {
            StreamWriter sw = new StreamWriter(Config.logPath, true);
            sw.WriteLine(DateTime.Now.ToString() + ": " + String);
            sw.Flush();
            sw.Close();
        }
        public struct Res
        {
            public string OutString;
            public bool HadErr;
        }
        public static Res RunScript(string scriptText)
        {
            try
            {
                // create Powershell runspace

                Runspace runspace = RunspaceFactory.CreateRunspace();

                // open it

                runspace.Open();

                // create a pipeline and feed it the script text

                Pipeline pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(scriptText);

                // add an extra command to transform the script
                // output objects into nicely formatted strings

                // remove this line to get the actual objects
                // that the script returns. For example, the script

                // "Get-Process" returns a collection
                // of System.Diagnostics.Process instances.

                pipeline.Commands.Add("Out-String");

                // execute the script
                Collection<PSObject> results = pipeline.Invoke();

                // close the runspace
                runspace.Close();

                // convert the script result into a single string

                StringBuilder stringBuilder = new StringBuilder();
                foreach (PSObject obj in results)
                {
                    stringBuilder.AppendLine(obj.ToString());
                }

                var result = new Res
                {
                    OutString = stringBuilder.ToString(),
                    HadErr = pipeline.HadErrors
                };
                return result;
            }
            catch (Exception err)
            {
                var result = new Res
                {
                    OutString = err.Message,
                    HadErr = true
                };
                return result;
            }
            
           
        }
    }
}
