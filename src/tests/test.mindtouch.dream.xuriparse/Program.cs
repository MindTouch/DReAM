using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MindTouch.Dream;

namespace MindTouchTest.Dream {
    public class Program {

        //--- Class Methods ---
        private static void Main(string[] args) {
            if(args.Length == 0) {
                Console.WriteLine("missing argument");
                return;
            }

            // load csv
            string[] lines = null;
            Timer("loading CSV file", () => {
                lines = File.ReadAllLines(args[0]);
            });

            // extract uris
            var uris = new List<string>(lines.Length);
            Timer("extracting URIs from CSV", () => {
                var first = true;
                foreach(var line in lines) {
                    if(first) {
                        first = false;
                        continue;
                    }
                    var uri = ExtractUri(line);
                    if(uri != null) {

                        // check if this URI contains "http://" more than once
                        if((uri.IndexOf("http://", 1, StringComparison.Ordinal) == -1) && (uri.IndexOf("https://", 1, StringComparison.Ordinal) == -1)) {
                            uris.Add(uri);
                        }
                    } else {
                        Console.WriteLine("invalid line: {0}", line);
                    }
                }
            });
            Console.WriteLine("{0:#,##0} URIs accepted; {1:#,##0} lines rejected", uris.Count, lines.Length - 1 - uris.Count);

            // convert uris
            var failed = 0;
            var failedList = new List<string>(10000);
            Timer("parse URIs using regex parser", () => {
                foreach(var uri in uris) {
                    var u = XUri.TryParse(uri);
                    if(u == null) {
                        ++failed;
                        failedList.Add(uri);
                    }
                }
            });
            if(failed > 0) {
                Console.WriteLine("{0:#,##0} uris failed to parse", failed);
                foreach(var failedUri in failedList) {
                    Console.WriteLine(failedUri);
                }
                Console.WriteLine();
            }

            // convert uris
            failed = 0;
            failedList.Clear();
            Timer("parse URIs using custom parser", () => {
                foreach(var uri in uris) {
                    var u = XUriParser.TryParse(uri);
                    if(u == null) {
                        ++failed;
                        failedList.Add(uri);
                    }
                }
            });
            if(failed > 0) {
                Console.WriteLine("{0:#,##0} uris failed to parse", failed);
                foreach(var failedUri in failedList) {
                    Console.WriteLine(failedUri);
                }
                Console.WriteLine();
            }

            // TODO (steveb): compare results produced by XUri and XUriParser
            var passed = 0;
            var skipped = 0;
            failed = 0;
            foreach(var uri in uris) {
                string schemeRegex;
                string userRegex;
                string passwordRegex;
                string hostnameRegex;
                int portRegex;
                bool usesDefaultPortRegex;
                bool trailingSlashRegex;
                string[] segmentsRegex;
                KeyValuePair<string, string>[] paramsRegex;
                string fragmentRegex;
                var successRegex = XUri.TryParse(uri, out schemeRegex, out userRegex, out passwordRegex, out hostnameRegex, out portRegex, out usesDefaultPortRegex, out segmentsRegex, out trailingSlashRegex, out paramsRegex, out fragmentRegex);

                string schemeCustom;
                string userCustom;
                string passwordCustom;
                string hostnameCustom;
                int portCustom;
                bool usesDefaultPortCustom;
                bool trailingSlashCustom;
                string[] segmentsCustom;
                KeyValuePair<string, string>[] paramsCustom;
                string fragmentCustom;
                var successCustom = XUriParser.TryParse(uri, out schemeCustom, out userCustom, out passwordCustom, out hostnameCustom, out portCustom, out usesDefaultPortCustom, out segmentsCustom, out trailingSlashCustom, out paramsCustom, out fragmentCustom);

                if(successRegex != successCustom) {
                    Console.WriteLine("FAILED TryParse: {0}", uri);
                    ++failed;
                    continue;
                }
                if(!successRegex) {

                    // if parsing failed, no point in comparing the outcome of the out arguments since the behavior does not need to be the same
                    continue;
                }
                if(schemeRegex != schemeCustom) {
                    Console.WriteLine("FAILED scheme: {0}", uri);
                    ++failed;
                    continue;
                }
                if(userRegex != userCustom) {
                    Console.WriteLine("FAILED user: {0}", uri);
                    ++failed;
                    continue;
                }
                if(passwordRegex != passwordCustom) {
                    Console.WriteLine("FAILED password: {0}", uri);
                    ++failed;
                    continue;
                }
                if(portRegex != portCustom) {
                    Console.WriteLine("FAILED port: {0}", uri);
                    ++failed;
                    continue;
                }
                if(usesDefaultPortRegex != usesDefaultPortCustom) {
                    Console.WriteLine("FAILED usesDefaultPort: {0}", uri);
                    ++failed;
                    continue;
                }
                if(trailingSlashRegex != trailingSlashCustom) {
                    Console.WriteLine("FAILED trailingSlash: {0}", uri);
                    ++failed;
                    continue;
                }
                if((segmentsRegex == null && segmentsCustom != null) || (segmentsRegex != null && segmentsCustom == null)) {
                    Console.WriteLine("FAILED segments: {0}", uri);
                    ++failed;
                    continue;
                }
                if(segmentsRegex != null) {
                    if(segmentsRegex.Length != segmentsCustom.Length) {
                        Console.WriteLine("FAILED segments: {0}", uri);
                        ++failed;
                        continue;
                    }
                    for(var i = 0; i < segmentsRegex.Length; ++i) {
                        if(segmentsRegex[i] != segmentsCustom[i]) {
                            if(segmentsCustom[i].Replace('\\', '/') != segmentsRegex[i]) {
                                Console.WriteLine("FAILED : {0}", uri);
                                ++failed;
                                goto skip;
                            }

                            // regex-parser does not preserve '\' in original uri
                            ++skipped;
                        }
                    }
                }
                if((paramsRegex == null && paramsCustom != null) || (paramsRegex != null && paramsCustom == null)) {
                    Console.WriteLine("FAILED params: {0}", uri);
                    ++failed;
                    continue;
                }
                if(paramsRegex != null) {
                    if(paramsRegex.Length != paramsCustom.Length) {
                        Console.WriteLine("FAILED params: {0}", uri);
                        ++failed;
                        continue;
                    }
                    for(var i = 0; i < paramsRegex.Length; ++i) {
                        if(paramsRegex[i].Key != paramsCustom[i].Key) {
                            Console.WriteLine("FAILED : {0}", uri);
                            ++failed;
                            goto skip;
                        }
                        if(paramsRegex[i].Value != paramsCustom[i].Value) {
                            Console.WriteLine("FAILED : {0}", uri);
                            ++failed;
                            goto skip;
                        }
                    }
                }
                if(fragmentRegex != fragmentCustom) {
                    Console.WriteLine("FAILED : {0}", uri);
                    ++failed;
                    continue;
                }
                ++passed;
            skip:
                var x = 0;
            }
            Console.WriteLine("{0:#,##0} uris passed comparison between regex and custom parser; {1:#,##0} were skipped; and {2:#,##0} failed", passed, skipped, failed);

            // TODO (steveb): check if URI can be reproduced validly
            return;
            foreach(var uri in uris) {
                var u = XUriParser.TryParse(uri);
                if((u != null) && !u.ToString().EqualsInvariant(uri)) {
                    Console.WriteLine("{0} -> {1}", uri, u);
                }
            }
        }

        private static void Timer(string description, Action action) {
            Console.Write("{0}...", description);
            var timer = Stopwatch.StartNew();
            try {
                action();
            } finally {
                timer.Stop();
                Console.WriteLine(" done ({0:#,##0.00} sec)", timer.Elapsed.TotalMilliseconds);
            }
        }

        private static string ExtractUri(string line) {
            var closingQuote = line.LastIndexOf('"');
            if(closingQuote <= 0) {
                return null;
            }
            var openingQuote = line.LastIndexOf('"', closingQuote - 1);
            return (openingQuote < 0) ? null : line.Substring(openingQuote + 1, closingQuote - openingQuote - 1);
        }
    }
}
