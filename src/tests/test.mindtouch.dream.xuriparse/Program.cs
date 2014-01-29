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

            // check if URI can be reproduced validly
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
