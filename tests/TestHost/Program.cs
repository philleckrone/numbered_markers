using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NumberedMarkers;

namespace NumberedMarkersTests
{
    /// <summary>
    /// Test host for the Numbered Markers plugin code, run on a folder of SFM files:
    ///   TestHost scan FOLDER
    ///   TestHost convert FOLDER FAMILIES|All EDITSPEC BACKUPDIR
    ///   TestHost form FOLDER EDITSPEC LANG BACKUPDIR
    /// EDITSPEC: ALL, NONE, or e.g. TOB,XXA,HEB:2-3.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: TestHost scan|convert|form FOLDER ...");
                return 2;
            }
            string folder = args[1];
            switch (args[0])
            {
                case "scan":
                    PrintScan(Scan(new FolderBookSource(folder, "TEST", "ALL", null)));
                    return 0;
                case "convert":
                {
                    var source = new FolderBookSource(folder, "TEST", args[3], args[4]);
                    ScanResult scan = Scan(source);
                    // FAMILIES: All (each mixed family in its default direction), or e.g.
                    // "io:remove,mt" (":remove" = \io1 to \io; otherwise \mt to \mt1).
                    var names = new Dictionary<string, ChangeDirection>();
                    if (args[2].Equals("All", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (FamilyStats f in scan.Mixed)
                            names[f.Name] = f.DefaultDirection;
                    }
                    else
                    {
                        foreach (string part in args[2].Split(','))
                        {
                            string[] nd = part.Trim().TrimStart('\\').Split(':');
                            names[nd[0]] = nd.Length > 1 && nd[1] == "remove"
                                ? ChangeDirection.RemoveNumber : ChangeDirection.AddNumber;
                        }
                    }
                    var runner = new ConversionRunner(source);
                    ConversionReport report = runner.Run(scan, names, "test history point");
                    foreach (Change c in report.Changes)
                        Console.WriteLine("CHANGE {0} {1} {2} {3}", c.Book, c.Family, c.Direction, c.Count);
                    Console.WriteLine("TOTAL {0} BOOKS {1}", report.Total, report.BooksChanged);
                    Console.WriteLine("NOPERMISSION " + string.Join(", ", report.NoPermission.ToArray()));
                    Console.WriteLine("BUSY " + string.Join(", ", report.Busy.ToArray()));
                    Console.WriteLine("FAILED " + string.Join(" | ", report.Failed.ToArray()));
                    Console.WriteLine("HISTORY " + string.Join(" | ", source.HistoryPoints.ToArray()));
                    Console.WriteLine("WRITES " + string.Join(", ", source.Writes.ToArray()));
                    Console.WriteLine("BACKUPS " + string.Join(", ", report.Backups.Select(System.IO.Path.GetFileName).ToArray()));
                    return 0;
                }
                case "form":
                {
                    var source = new FolderBookSource(folder, "PDT15", args[2], args[4]);
                    Application.EnableVisualStyles();
                    Application.Run(new NumberedMarkersForm(source, Strings.For(args[3])));
                    return 0;
                }
            }
            Console.Error.WriteLine("unknown mode " + args[0]);
            return 2;
        }

        private static ScanResult Scan(IBookSource source)
        {
            ScanResult scan = MarkerScanner.NewResult();
            foreach (BookId book in source.Books)
                MarkerScanner.ScanBook(scan, book.Code, source.GetBook(book.Number), "intro");
            return scan;
        }

        private static void PrintScan(ScanResult scan)
        {
            foreach (FamilyStats f in scan.Families.Where(f => f.IsUsed))
                Console.WriteLine("{0}\tbare={1}\tlevels={2}\tother={3}\tmixed={4}\tcanRemove={5}\tdefault={6}\toneForm={7}\tlevel1={8}\tbooks={9}",
                    f.Name, f.Bare, f.LevelList(true), f.LevelList(false), f.IsMixed, f.CanRemoveNumber,
                    f.DefaultDirection, f.OneFormDirection.HasValue ? f.OneFormDirection.Value.ToString() : "-",
                    ChapterList(f.Level1Places), ChapterList(f.BarePlaces));
        }

        private static string ChapterList(Places places)
        {
            return string.Join(",", places.Chapters.Select(kv => kv.Key + ":" +
                string.Join("/", kv.Value.Select(c => c.ToString()).ToArray())).ToArray());
        }
    }
}
