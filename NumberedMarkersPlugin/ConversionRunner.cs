using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NumberedMarkers
{
    /// <summary>
    /// Which chapters of a book hold the markers the chosen families would change, split into
    /// those the user may edit and those the user may not.
    /// </summary>
    public sealed class BookPlan
    {
        public readonly BookId Book;
        public readonly List<int> Editable = new List<int>();
        public readonly List<int> NotEditable = new List<int>();

        public BookPlan(BookId book)
        {
            Book = book;
        }

        /// <summary>"GEN", or "GEN 2, 5" when only some of the book's chapters are meant.</summary>
        public string Describe(List<int> chapters)
        {
            if (chapters.Count == 0) return "";
            if (Editable.Count == 0 || NotEditable.Count == 0) return Book.Code;
            return Book.Code + " " + string.Join(", ", chapters.Select(c => c.ToString()).ToArray());
        }
    }

    /// <summary>The changes made to one family in one book.</summary>
    public sealed class Change
    {
        public readonly string Book;
        public readonly string Family;
        public readonly ChangeDirection Direction;
        public readonly int Count;

        public Change(string book, string family, ChangeDirection direction, int count)
        {
            Book = book;
            Family = family;
            Direction = direction;
            Count = count;
        }
    }

    public sealed class ConversionReport
    {
        /// <summary>The changes, in book order.</summary>
        public readonly List<Change> Changes = new List<Change>();
        public readonly List<string> NoPermission = new List<string>();
        public readonly List<string> Busy = new List<string>();
        public readonly List<string> Failed = new List<string>();
        public readonly List<string> Backups = new List<string>();
        public string HistoryError;
        /// <summary>True when no history point could be made, so nothing was changed.</summary>
        public bool Stopped;
        public int BooksChanged;

        public int Total
        {
            get { return Changes.Sum(c => c.Count); }
        }
    }

    /// <summary>
    /// Plans and runs the conversion the way Paratext treats edits: chapters the user may edit are
    /// changed, the others are left alone and reported. Only chapters that change are written.
    /// </summary>
    public sealed class ConversionRunner
    {
        private readonly IBookSource source;
        private readonly Dictionary<long, bool> canEdit = new Dictionary<long, bool>();

        /// <summary>Message for a chapter Paratext accepted but did not store (set from Strings).</summary>
        public string NotSavedText = "not saved";

        public ConversionRunner(IBookSource source)
        {
            this.source = source;
        }

        public bool CanEdit(int bookNum, int chapter)
        {
            long key = bookNum * 10000L + chapter;
            bool result;
            if (!canEdit.TryGetValue(key, out result))
            {
                try { result = source.CanEdit(bookNum, chapter); }
                catch (Exception) { result = false; }
                canEdit[key] = result;
            }
            return result;
        }

        /// <summary>The books holding the markers one family would change, in canonical order.</summary>
        public List<BookPlan> Plan(ScanResult scan, string family, ChangeDirection direction)
        {
            var choice = new Dictionary<string, ChangeDirection>();
            choice[family] = direction;
            return Plan(scan, choice);
        }

        /// <summary>
        /// The books holding the markers the chosen families would change (the unnumbered form when
        /// adding the number, the "1" form when removing it), in canonical order.
        /// </summary>
        public List<BookPlan> Plan(ScanResult scan, IDictionary<string, ChangeDirection> families)
        {
            var plans = new List<BookPlan>();
            foreach (BookId book in source.Books)
            {
                var chapters = new SortedSet<int>();
                foreach (var kv in families)
                {
                    SortedSet<int> c;
                    if (scan.ByName[kv.Key].PlacesFor(kv.Value).Chapters.TryGetValue(book.Code, out c))
                        chapters.UnionWith(c);
                }
                if (chapters.Count == 0) continue;
                var plan = new BookPlan(book);
                foreach (int chapter in chapters)
                {
                    if (CanEdit(book.Number, chapter)) plan.Editable.Add(chapter);
                    else plan.NotEditable.Add(chapter);
                }
                plans.Add(plan);
            }
            return plans;
        }

        /// <summary>True when the user may edit no book of the project at all.</summary>
        public bool NoPermissionAtAll()
        {
            foreach (BookId book in source.Books)
                if (CanEdit(book.Number, 0)) return false;
            return true;
        }

        /// <summary>
        /// Converts the families in every chapter the user may edit. Each changed book is first
        /// saved whole as Backup_BOOK_time.bak (UTF-8). historyMessage is first recorded as a
        /// project history point; if that fails, nothing is changed (report.Stopped).
        /// </summary>
        public ConversionReport Run(ScanResult scan, IDictionary<string, ChangeDirection> families, string historyMessage)
        {
            var report = new ConversionReport();
            List<string> add = families.Where(kv => kv.Value == ChangeDirection.AddNumber).Select(kv => kv.Key).ToList();
            List<string> remove = families.Where(kv => kv.Value == ChangeDirection.RemoveNumber).Select(kv => kv.Key).ToList();
            List<BookPlan> plans = Plan(scan, families);
            if (plans.All(p => p.Editable.Count == 0))
            {
                foreach (BookPlan p in plans)
                    report.NoPermission.Add(p.Describe(p.NotEditable));
                return report;
            }

            // A history point is required: without one nothing is changed.
            string historyError;
            bool made;
            try { made = source.MakeHistoryPoint(historyMessage, out historyError); }
            catch (Exception ex)
            {
                made = false;
                historyError = ex.Message;
            }
            if (!made)
            {
                report.HistoryError = historyError ?? "";
                report.Stopped = true;
                return report;
            }

            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            foreach (BookPlan plan in plans)
            {
                if (plan.NotEditable.Count > 0)
                    report.NoPermission.Add(plan.Describe(plan.NotEditable));
                if (plan.Editable.Count == 0) continue;

                string backupError;
                string backup = SaveBackup(plan.Book, stamp, out backupError);
                if (backup == null)
                {
                    report.Failed.Add(plan.Book.Code + ": " + backupError);
                    continue;
                }

                // Families are either added to or removed from, never both, so one count per family.
                var counts = new Dictionary<string, int>();
                var busy = new List<int>();
                bool changed = false;
                foreach (int chapter in plan.Editable)
                {
                    string old = source.GetChapter(plan.Book.Number, chapter);
                    if (old == null && chapter == 1) old = source.GetBook(plan.Book.Number);
                    if (old == null) continue;
                    var chapterCounts = new Dictionary<string, int>();
                    string text = MarkerScanner.ConvertBare(old, add, chapterCounts);
                    text = MarkerScanner.ConvertLevel1ToBare(text, remove, chapterCounts);
                    if (text == old) continue;

                    string error;
                    WriteOutcome outcome = source.WriteChapter(plan.Book.Number, chapter, text, out error);
                    if (outcome == WriteOutcome.Written)
                    {
                        // Paratext normalizes what it stores; check the markers really changed.
                        string now = source.GetChapter(plan.Book.Number, chapter);
                        if (now == null && chapter == 1) now = source.GetBook(plan.Book.Number);
                        if (now != null && MarkerScanner.CountBare(now, add) + MarkerScanner.CountLevel1(now, remove) > 0)
                        {
                            report.Failed.Add(plan.Book.Code + " " + chapter + ": " + NotSavedText);
                            continue;
                        }
                        changed = true;
                        foreach (var kv in chapterCounts)
                        {
                            int n;
                            counts.TryGetValue(kv.Key, out n);
                            counts[kv.Key] = n + kv.Value;
                        }
                    }
                    else if (outcome == WriteOutcome.NoPermission)
                        report.NoPermission.Add(plan.Book.Code + " " + chapter);
                    else if (outcome == WriteOutcome.Busy)
                        busy.Add(chapter);
                    else
                        report.Failed.Add(plan.Book.Code + " " + chapter + ": " + error);
                }
                if (busy.Count > 0)
                    report.Busy.Add(plan.Book.Code + " " + string.Join(", ", busy.Select(c => c.ToString()).ToArray()));
                if (changed)
                {
                    report.BooksChanged++;
                    report.Backups.Add(backup);
                    foreach (string name in MarkerScanner.FamilyNames)
                    {
                        int n;
                        if (counts.TryGetValue(name, out n) && n > 0)
                            report.Changes.Add(new Change(plan.Book.Code, name, families[name], n));
                    }
                }
                else
                {
                    try { File.Delete(backup); }
                    catch (Exception) { }
                }
            }
            return report;
        }

        private string SaveBackup(BookId book, string stamp, out string error)
        {
            error = null;
            try
            {
                string text = source.GetBook(book.Number);
                if (text == null)
                {
                    error = "empty book";
                    return null;
                }
                string dir = source.BackupDirectory;
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "Backup_" + book.Code + "_" + stamp + ".bak");
                File.WriteAllText(path, text, new UTF8Encoding(false));
                return path;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }
    }
}
