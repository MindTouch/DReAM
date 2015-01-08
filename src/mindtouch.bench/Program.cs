/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2014 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace MindTouch.Tools {
    class DreamBench {

        static string PATH_TO_AB = string.Empty;
        static string PATH_TO_OUTPUT = string.Empty;
        static int NUM_REQUESTS = 50;
        static int NUM_THREADS = 1;
        static string GNUPLOT_FILENAME = "gnuplotdata.txt";
        static string STDOUTPUT_FILENAME = "results.txt";
        static string HTMLOUTPUT_FILENAME = "results.html";
        static bool OUTPUT_HTML = false;
        static bool SAVE_GNOPLOT_DATA = false;
        static int URIS_TESTED = 0;
        static string BASE_URI = string.Empty;
        static string[] URI_LIST = new string[] { };

        static Dictionary<char, char> CharReplaceMap = new Dictionary<char, char>();

        static void Main(string[] args) {

            //All invalid filename chars are replaced with spaces
            foreach (char c in System.IO.Path.GetInvalidFileNameChars()) {
                CharReplaceMap.Add(c, '_');
            }

            ProcessCmdLineOptions(args);

            if (PATH_TO_AB == string.Empty) {
                PATH_TO_AB = DiscoverAbPath();
            }
            else {
                if (!File.Exists(PATH_TO_AB))
                    Die(false, "Cant find given path to 'ab'");
            }

            if( PATH_TO_OUTPUT == string.Empty){
                PATH_TO_OUTPUT = DiscoverOutputPath();
            }

            if (URI_LIST == null || URI_LIST.Length == 0)
                Die(true, "--uri or --urifile must be specified");

            Directory.CreateDirectory(PATH_TO_OUTPUT);

            Out(string.Format("Requests: {0} Threads: {1}: BaseUri: {2}", NUM_REQUESTS, NUM_THREADS, BASE_URI));
            Out(string.Format("OutputPath: {0}", PATH_TO_OUTPUT));
            Out(string.Format("AB Path: {0}", PATH_TO_AB));

            string testuri = null;
            while((testuri = GetNextTestUri()) != null){
                Out(string.Format("Testing URI: '{0}'", testuri));
                BenchAUri(testuri, NUM_REQUESTS, NUM_THREADS, OUTPUT_HTML, SAVE_GNOPLOT_DATA);
            }

            Out("Done!");
        }

        static void Out(string s) {
            Console.WriteLine(GlobalClock.UtcNow.ToLongTimeString() + ": " + s);
        }

        static void BenchAUri(string uri, int numRequests, int numThreads, bool htmlOutput, bool saveGnuPlotData) {

            StringBuilder cmdLineSB = new StringBuilder();
            cmdLineSB.AppendFormat(" -n {0}", numRequests);
            cmdLineSB.AppendFormat(" -c {0}", numThreads);
            if( htmlOutput)
                cmdLineSB.Append(" -w");
            if( saveGnuPlotData){
                cmdLineSB.AppendFormat(" -g \"{0}\"",  BuildPathWithUri(uri,GNUPLOT_FILENAME));
            }

            cmdLineSB.Append(" " + uri);

            MemoryStream stdoutStream;
            string stderror;
            int statuscode = ExecuteProcess(PATH_TO_AB, cmdLineSB.ToString(), null, 0, out stdoutStream, out stderror);

            if (statuscode == 0 && stdoutStream != null && stdoutStream.Length > 0) {
                string stdoutSavePath = BuildPathWithUri(uri, htmlOutput ? HTMLOUTPUT_FILENAME : STDOUTPUT_FILENAME);
                CopyToFile(stdoutStream, -1, stdoutSavePath);
            }
            else {
                Out("Unable to get output for url. Error: " + stderror);
            }
        }

        static void ProcessCmdLineOptions(string[] args) {

            for (int i = 0; i < args.Length; i += 2) {
                string key = args[i];
                string value = ((i + 1) < args.Length) ? args[i + 1] : "";

                switch (key) {
                    case "--help":
                        PrintUsage();
                        break;
                    case "--urifile":
                        LoadUriFile(value);
                        break;
                    case "--uri":
                        LoadUri(value);
                        break;
                    case "--threads":
                        if (!int.TryParse(value, out NUM_THREADS))
                            Die(false, string.Format("Could not parse --threads {0} ", value));
                        break;
                    case "--requests":
                        if (!int.TryParse(value, out NUM_REQUESTS))
                            Die(false, string.Format("Could not parse --requests {0} ", value));
                        break;
                    case "--outputpath":
                        PATH_TO_OUTPUT = value;
                        break;
                    case "--abpath":
                        PATH_TO_AB = value;
                        break;
                    case "--baseuri":
                        BASE_URI = value;
                        break;
                    case "--htmloutput":
                        OUTPUT_HTML = true;
                        i--;
                        break;
                    case "--savegnuplot":
                        SAVE_GNOPLOT_DATA = true;
                        i--;
                        break;
                    default:
                        PrintUsage();
                        return;
                }
            } 
        }

        private static void PrintUsage() {
            Console.WriteLine("MindTouch Bench, Copyright (c) 2006-2014 MindTouch, Inc.");
            Console.WriteLine("USAGE: mindtouch.bench.exe [arg1] ... [argN]");
            Console.WriteLine("    --urifile <filename>   Load a list of uris from a file. One per line");
            Console.WriteLine("    --uri <uri>            Test one uri");
            Console.WriteLine("    --requests <#>         Number of requests per uri (default: 50)");
            Console.WriteLine("    --threads <#>          Number of threads to use (default: 1)");
            Console.WriteLine("    --baseuri <uri>        Base uri to use including credentials/hostname/port/path (Example: http://localhost:80/@api");
            Console.WriteLine("    --outputpath <path>    Directory to store output (default: current dir)");
            Console.WriteLine("    --abpath <path>        Path to apache bench 'ab' binary (default: /usr/sbin/ab for unix)");
            Console.WriteLine("    --htmloutput           Save result output as html instead of text");
            Console.WriteLine("    --savegnuplot          Save data gnuplot");
            Console.WriteLine();
            Console.WriteLine(" Note: This requires apache bench which comes with debian");
            Console.WriteLine("       package 'apache2-utils' or with windows Apache installer");

            Environment.Exit(69);
        }

        static void Die(bool displayHelp, string error) {
            Console.Error.WriteLine("An error has occurred: ");
            Console.Error.WriteLine("\t" + error);
            if (displayHelp)
                PrintUsage();
            Environment.Exit(1);
        }

        static void LoadUriFile(string path) {
            if (!File.Exists(path)) {
                Die(false, string.Format("--urifile '{0}' does not exist!", path));

            }
            string[] urilist = File.ReadAllLines(path);
            List<string> uriOut = new List<string>();

            foreach (string uri in urilist) {
                if (uri.Trim().Length == 0)
                    continue;
                uriOut.Add(uri.Trim());
            }


            URI_LIST = uriOut.ToArray();
        }

        static void LoadUri(string value) {
            URI_LIST = new string[] { value };
        }

        static string DiscoverAbPath() {
            string path;
            bool isUnix = Environment.OSVersion.Platform == PlatformID.Unix;
            if( isUnix)
                path = "/usr/sbin/ab";
            else
                path = @"C:\Program Files\Apache Group\Apache2\bin\ab.exe";

            if (!File.Exists(path))
                Die(false, "Unable to find path to 'ab'. Please use --abpath argument");

            return path;
        }

        static string BuildPathWithUri(string uri, string filename) {
            filename = Path.GetFileNameWithoutExtension(filename) + "_" + GlobalClock.UtcNow.ToString("yyyyMMdd_HHmm") + Path.GetExtension(filename);
            string ret = Path.Combine(Path.Combine( PATH_TO_OUTPUT, CleanFileName(new Uri(uri).PathAndQuery.TrimStart('/'))), filename);
            Directory.CreateDirectory(Path.GetDirectoryName(ret));
            return ret;
        }

        static string CleanFileName(string rawfilename) {
            if (rawfilename == null)
                return null;

            StringBuilder sb = new StringBuilder();
            foreach (char c in rawfilename) {
                if (CharReplaceMap.ContainsKey(c))
                    sb.Append(CharReplaceMap[c]);
                else
                    sb.Append(c);
            }

            return sb.ToString();
        }


        static string DiscoverOutputPath() {
            string basePath = Environment.CurrentDirectory;
            //return Path.Combine(basePath, "dekibench_" + GlobalClock.UtcNow.ToString("yyyyMMddHHmmss"));
            return basePath;
        }

        static string GetNextTestUri() {
            string ret;
            if (URI_LIST.Length <= URIS_TESTED)
                ret = null;
            else {
                if (BASE_URI == string.Empty)
                    ret = URI_LIST[URIS_TESTED];
                else {
                    UriBuilder ub = new UriBuilder(BASE_URI);
                    ub.Path = ub.Path + URI_LIST[URIS_TESTED].TrimStart('/');
                    ret = ub.ToString();
                }
            }
            if( ret != null )
                URIS_TESTED++;
            return ret;
        }

        public static int ExecuteProcess(string application, string cmdline, Stream input, uint timeoutInMS, out MemoryStream output, out string stdError) {

            // start process
            Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = application;
            proc.StartInfo.Arguments = cmdline;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardInput = input != null;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.Start();

            // inject input
            if (input != null) {
                CopyStream(input, proc.StandardInput.BaseStream, -1);
                proc.StandardInput.Close();
            }
            if (timeoutInMS != 0)
                proc.WaitForExit((int) timeoutInMS);
            else
                proc.WaitForExit();

            // extract output
            output = CopyToMemoryStream(proc.StandardOutput.BaseStream, -1);
            StreamReader sr = new StreamReader(proc.StandardError.BaseStream);
            stdError = sr.ReadToEnd();
            sr.Close();

            // return result
            proc.WaitForExit();
            return proc.ExitCode;
        }

        #region stream util methods

        public static bool CopyToFile(Stream stream, long length, string filename) {
            FileStream file = null;
            try {
                using (file = File.Create(filename)) {
                    CopyStream(stream, file, length);
                }
            }
            catch (Exception) {

                // BUGBUGBUG (steveb): it's bad practice to swallow exceptions

                // check if we created a file
                if (file != null) {
                    file.Close();
                    File.Delete(filename);
                }
                return false;
            }
            finally {
                stream.Close();
            }
            return true;
        }

        public static long CopyStream(Stream source, Stream target, long length) {
            if (source == Stream.Null) {
                return 0;
            }
            byte[] buffer = new byte[32768];
            long result = 0;
            int zero_read_counter = 0;
            while (length != 0) {
                long count = source.Read(buffer, 0, buffer.Length);
                if (count > 0) {
                    zero_read_counter = 0;
                    target.Write(buffer, 0, (int) count);
                    result += count;

                    // NOTE (steveb): we stop when we've read the expected number of bytes when the length was non-negative, 
                    //                otherwise we stop when we can't read anymore bytes.
                    if (length >= 0) {
                        length -= count;
                    }
                    else if (count == 0) {
                        break;
                    }
                }
                else if (++zero_read_counter > 10) {

                    // let's abort after 10 tries to read more data
                    break;
                }
            }
            return result;
        }

        public static MemoryStream CopyToMemoryStream(Stream stream, long length) {
            MemoryStream result;
            if (stream is MemoryStream) {
                MemoryStream mem = (MemoryStream) stream;
                result = new MemoryStream(mem.GetBuffer(), 0, (int) mem.Length, false, true);
            }
            else {
                result = new MemoryStream();
                CopyStream(stream, result, length);
                if (length >= 0 && result.Position != length)
                    throw new InvalidOperationException();
                result.Position = 0;
            }
            return result;
        }

        #endregion 
    }
}
