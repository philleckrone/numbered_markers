using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace NumberedMarkers
{
    /// <summary>
    /// The books to examine, saved per project in &lt;project&gt;\local\NumberedMarkers\settings.xml,
    /// which Send/Receive does not share.
    /// </summary>
    public sealed class BookChoice
    {
        /// <summary>
        /// Codes of the books not to examine, separated by spaces; null when none were saved, so
        /// every book is examined. An empty string also examines every book.
        /// </summary>
        public string ExcludedBooks = null;

        /// <summary>Books that are not Bible text: front and back matter, extras, glossary, concordance.</summary>
        public static readonly HashSet<string> OtherBooks = new HashSet<string>(
            "FRT BAK OTH INT CNC GLO TDX NDX XXA XXB XXC XXD XXE XXF XXG".Split(' '), StringComparer.OrdinalIgnoreCase);

        public static bool IsBibleBook(string code)
        {
            return !OtherBooks.Contains(code);
        }

        /// <summary>Whether a book is left out of the scan (never, when no choice was saved).</summary>
        public bool IsExcluded(BookId book)
        {
            if (ExcludedBooks == null) return false;
            foreach (string code in ExcludedBooks.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
                if (string.Equals(code, book.Code, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string SettingsPath(string localDirectory)
        {
            return localDirectory == null ? null : Path.Combine(localDirectory, "settings.xml");
        }

        /// <summary>The saved choice, or all books when there is none (or it cannot be read).</summary>
        public static BookChoice Load(string path)
        {
            var c = new BookChoice();
            if (path == null || !File.Exists(path)) return c;
            try
            {
                var doc = new XmlDocument();
                // No DTD processing and nothing fetched from outside the file.
                using (XmlReader r = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null }))
                    doc.Load(r);
                XmlNode node = doc.DocumentElement != null ? doc.DocumentElement.SelectSingleNode("ExcludedBooks") : null;
                if (node != null) c.ExcludedBooks = node.InnerText.Trim();
            }
            catch (Exception)
            {
                return new BookChoice();
            }
            return c;
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.Encoding = new UTF8Encoding(false);
            using (XmlWriter w = XmlWriter.Create(path, settings))
            {
                w.WriteStartElement("NumberedMarkersSettings");
                if (ExcludedBooks != null) w.WriteElementString("ExcludedBooks", ExcludedBooks);
                w.WriteEndElement();
            }
        }
    }
}
