using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NumberedMarkers
{
    /// <summary>
    /// Counts, for each marker family (\q, \mt, \s ...), the unnumbered form and each numbered
    /// level, and changes the unnumbered form to the "1" form. No Paratext types, so the tests
    /// can run it on SFM files. Ported from the retired CMS check (NumberedMarkers_src.py).
    /// </summary>
    public static class MarkerScanner
    {
        /// <summary>
        /// Paragraph markers whose unnumbered form is level 1, with the highest level usfm.sty
        /// (3.0.11) defines. Left out on purpose: \h and \tr (numbered forms deprecated), \toc
        /// (\toc is not \toc1), \toca and the table cells (no unnumbered form), \p1/\p2 and
        /// \k1/\k2 (periph markers, not levels).
        /// </summary>
        public static readonly KeyValuePair<string, int>[] Families = new[]
        {
            Fam("imt", 4), Fam("imte", 2), Fam("is", 2), Fam("io", 4), Fam("ili", 2), Fam("iq", 3),
            Fam("mt", 4), Fam("mte", 2), Fam("ms", 3), Fam("s", 4), Fam("sd", 4), Fam("pi", 3),
            Fam("q", 4), Fam("qm", 3), Fam("li", 4), Fam("lim", 4), Fam("liv", 5), Fam("ph", 3),
        };

        public const int MaxWhere = 50;

        private static KeyValuePair<string, int> Fam(string name, int maxLevel)
        {
            return new KeyValuePair<string, int>(name, maxLevel);
        }

        public static IEnumerable<string> FamilyNames
        {
            get { return Families.Select(f => f.Key); }
        }

        public static int MaxLevel(string family)
        {
            foreach (var f in Families)
                if (f.Key == family) return f.Value;
            return 0;
        }

        /// <summary>Longest names first, so \imte is not read as \imt + "e".</summary>
        private static string Alternation(IEnumerable<string> names)
        {
            return string.Join("|", names.OrderByDescending(n => n.Length).Select(Regex.Escape).ToArray());
        }

        // The lookahead after the digits stops \q matching \qs, \qc, \qr, \qa, \qt, \li matching
        // \lit, \s matching \sp, \sr, \sig and so on.
        private static readonly Regex TokenRegex = new Regex(
            @"\\(?:(c|v)[ \t]+(\d+)|(" + Alternation(FamilyNames) + @")(\d*)(?=[ \t\r\n]|$))",
            RegexOptions.CultureInvariant);

        private static Regex BareRegex(IEnumerable<string> names)
        {
            return new Regex(@"\\(" + Alternation(names) + @")(?=[ \t\r\n]|$)", RegexOptions.CultureInvariant);
        }

        public static ScanResult NewResult()
        {
            return new ScanResult();
        }

        /// <summary>
        /// Adds one book to the counts. Text before \c 1 belongs to chapter 1 (as in Paratext,
        /// where chapter 1 starts with \id); a book without \c is all chapter 1.
        /// </summary>
        public static void ScanBook(ScanResult result, string book, string usfm, string introLabel)
        {
            if (string.IsNullOrEmpty(usfm)) return;
            result.BooksScanned.Add(book);
            string chap = "0";
            string verse = "0";
            foreach (Match m in TokenRegex.Matches(usfm))
            {
                if (m.Groups[1].Value == "c")
                {
                    chap = m.Groups[2].Value;
                    verse = "0";
                    continue;
                }
                if (m.Groups[1].Value == "v")
                {
                    verse = m.Groups[2].Value;
                    continue;
                }
                FamilyStats fam = result.ByName[m.Groups[3].Value];
                string num = m.Groups[4].Value;
                int chapter = chap == "0" ? 1 : int.Parse(chap);
                string loc;
                if (chap == "0") loc = book + " (" + introLabel + ")";
                else if (verse == "0") loc = book + " " + chap;
                else loc = book + " " + chap + ":" + verse;
                if (num.Length > 0)
                {
                    int level;
                    if (!int.TryParse(num, out level)) continue;
                    int count;
                    fam.Levels.TryGetValue(level, out count);
                    fam.Levels[level] = count + 1;
                    if (level == 1)
                        fam.Level1Places.Add(book, chapter, loc);
                    continue;
                }
                fam.Bare++;
                int inBook;
                fam.BareBooks.TryGetValue(book, out inBook);
                fam.BareBooks[book] = inBook + 1;
                fam.BarePlaces.Add(book, chapter, loc);
            }
        }

        /// <summary>
        /// Changes the "1" form of the given families to the unnumbered form (\io1 to \io), for
        /// families that use no level 2 or higher. counts receives the changes per family.
        /// </summary>
        public static string ConvertLevel1ToBare(string usfm, ICollection<string> families, IDictionary<string, int> counts)
        {
            if (string.IsNullOrEmpty(usfm) || families.Count == 0) return usfm;
            return Level1Regex(families).Replace(usfm, m =>
            {
                string name = m.Groups[1].Value;
                int n;
                counts.TryGetValue(name, out n);
                counts[name] = n + 1;
                return "\\" + name;
            });
        }

        /// <summary>Number of "1" forms of the given families left in the text.</summary>
        public static int CountLevel1(string usfm, ICollection<string> families)
        {
            if (string.IsNullOrEmpty(usfm) || families.Count == 0) return 0;
            return Level1Regex(families).Matches(usfm).Count;
        }

        private static Regex Level1Regex(IEnumerable<string> names)
        {
            return new Regex(@"\\(" + Alternation(names) + @")1(?=[ \t\r\n]|$)", RegexOptions.CultureInvariant);
        }

        /// <summary>
        /// Changes the unnumbered form of the given families to the "1" form. counts receives the
        /// number of changes per family. Returns the text unchanged when nothing matched.
        /// </summary>
        public static string ConvertBare(string usfm, ICollection<string> families, IDictionary<string, int> counts)
        {
            if (string.IsNullOrEmpty(usfm) || families.Count == 0) return usfm;
            return BareRegex(families).Replace(usfm, m =>
            {
                string name = m.Groups[1].Value;
                int n;
                counts.TryGetValue(name, out n);
                counts[name] = n + 1;
                return "\\" + name + "1";
            });
        }

        /// <summary>Number of unnumbered markers of the given families left in the text.</summary>
        public static int CountBare(string usfm, ICollection<string> families)
        {
            if (string.IsNullOrEmpty(usfm) || families.Count == 0) return 0;
            return BareRegex(families).Matches(usfm).Count;
        }
    }

    public sealed class ScanResult
    {
        public readonly List<FamilyStats> Families = new List<FamilyStats>();
        public readonly Dictionary<string, FamilyStats> ByName = new Dictionary<string, FamilyStats>();
        public readonly List<string> BooksScanned = new List<string>();

        public ScanResult()
        {
            foreach (var f in MarkerScanner.Families)
            {
                var stats = new FamilyStats(f.Key, f.Value);
                Families.Add(stats);
                ByName[f.Key] = stats;
            }
        }

        public IEnumerable<FamilyStats> Mixed
        {
            get { return Families.Where(f => f.IsMixed); }
        }

        public IEnumerable<FamilyStats> Consistent
        {
            get { return Families.Where(f => f.IsUsed && !f.IsMixed); }
        }

        /// <summary>Consistent families that could still be changed (only \s, or only \is1).</summary>
        public IEnumerable<FamilyStats> OneForm
        {
            get { return Families.Where(f => f.OneFormDirection.HasValue); }
        }
    }

    /// <summary>Which way a mixed family is changed.</summary>
    public enum ChangeDirection
    {
        /// <summary>\io to \io1 (always possible for a mixed family).</summary>
        AddNumber,
        /// <summary>\io1 to \io (only when no level 2 or higher is used).</summary>
        RemoveNumber,
    }

    /// <summary>Where one form of a marker occurs: chapters per book, and grouped locations.</summary>
    public sealed class Places
    {
        /// <summary>Book code -> chapters holding the form (the book's intro is chapter 1).</summary>
        public readonly Dictionary<string, SortedSet<int>> Chapters = new Dictionary<string, SortedSet<int>>();
        public readonly List<string> Where = new List<string>();
        public readonly Dictionary<string, int> WhereCount = new Dictionary<string, int>();
        public int WhereMore;

        public void Add(string book, int chapter, string loc)
        {
            SortedSet<int> chapters;
            if (!Chapters.TryGetValue(book, out chapters))
            {
                chapters = new SortedSet<int>();
                Chapters[book] = chapters;
            }
            chapters.Add(chapter);
            int seen;
            if (WhereCount.TryGetValue(loc, out seen))
                WhereCount[loc] = seen + 1;
            else if (Where.Count < MarkerScanner.MaxWhere)
            {
                Where.Add(loc);
                WhereCount[loc] = 1;
            }
            else
                WhereMore++;
        }

        public string WhereText(string moreFormat)
        {
            var parts = Where.Select(loc =>
            {
                int n = WhereCount[loc];
                return n == 1 ? loc : loc + " [" + n + "]";
            }).ToArray();
            string s = string.Join(", ", parts);
            if (WhereMore > 0)
                s += " " + string.Format(moreFormat, WhereMore);
            return s;
        }
    }

    public sealed class FamilyStats
    {
        public readonly string Name;
        public readonly int MaxLevel;
        public int Bare;
        public readonly SortedDictionary<int, int> Levels = new SortedDictionary<int, int>();
        /// <summary>Book code -> number of unnumbered markers.</summary>
        public readonly Dictionary<string, int> BareBooks = new Dictionary<string, int>();
        /// <summary>Where the unnumbered form (\io) is.</summary>
        public readonly Places BarePlaces = new Places();
        /// <summary>Where the "1" form (\io1) is.</summary>
        public readonly Places Level1Places = new Places();

        public FamilyStats(string name, int maxLevel)
        {
            Name = name;
            MaxLevel = maxLevel;
        }

        /// <summary>Book code -> chapters holding the unnumbered form.</summary>
        public Dictionary<string, SortedSet<int>> BareChapters
        {
            get { return BarePlaces.Chapters; }
        }

        public int Level1
        {
            get
            {
                int n;
                Levels.TryGetValue(1, out n);
                return n;
            }
        }

        /// <summary>Whether a level from 2 up to the highest usfm.sty defines is used (\s5 does not count).</summary>
        public bool HasHigherLevels
        {
            get { return Levels.Any(l => l.Key >= 2 && l.Key <= MaxLevel && l.Value > 0); }
        }

        /// <summary>A mixed family with no level 2 or higher may also be changed from \io1 to \io.</summary>
        public bool CanRemoveNumber
        {
            get { return IsMixed && !HasHigherLevels; }
        }

        /// <summary>The direction with fewer changes (adding the number on a tie).</summary>
        public ChangeDirection DefaultDirection
        {
            get { return CanRemoveNumber && Level1 < Bare ? ChangeDirection.RemoveNumber : ChangeDirection.AddNumber; }
        }

        /// <summary>
        /// For a family that is not mixed: the one change that could be made, or null.
        /// Only \s (no numbered form): \s to \s1. Only \is1 (no bare form, no level 2 or higher):
        /// \is1 to \is. Anything else (e.g. \mt1 with \mt2) has nothing to offer.
        /// </summary>
        public ChangeDirection? OneFormDirection
        {
            get
            {
                if (IsMixed) return null;
                if (Bare > 0 && Numbered == 0) return ChangeDirection.AddNumber;
                if (Bare == 0 && Level1 > 0 && !HasHigherLevels) return ChangeDirection.RemoveNumber;
                return null;
            }
        }

        /// <summary>Where the markers changed in the given direction are.</summary>
        public Places PlacesFor(ChangeDirection direction)
        {
            return direction == ChangeDirection.RemoveNumber ? Level1Places : BarePlaces;
        }

        /// <summary>Uses of the numbered levels usfm.sty defines (\s5 does not count).</summary>
        public int Numbered
        {
            get { return Levels.Where(l => l.Key <= MaxLevel).Sum(l => l.Value); }
        }

        public bool IsMixed
        {
            get { return Bare > 0 && Numbered > 0; }
        }

        public bool IsUsed
        {
            get { return Bare > 0 || Levels.Count > 0; }
        }

        /// <summary>e.g. "\mt1 (91), \mt2 (30)"; standard=false lists the levels usfm.sty lacks.</summary>
        public string LevelList(bool standard)
        {
            return string.Join(", ", Levels.Where(l => (l.Key <= MaxLevel) == standard)
                .Select(l => "\\" + Name + l.Key + " (" + l.Value + ")").ToArray());
        }

    }
}
