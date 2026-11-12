using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using Microsoft.Win32;
using Paratext.PluginInterfaces;

namespace NumberedMarkers
{
    /// <summary>
    /// Paratext plugin that adds Tools > Numbered Markers... to Scripture windows. It lists the
    /// markers the project uses in both their unnumbered and numbered forms (\mt beside \mt1) and
    /// changes the unnumbered form to the "1" form in the chapters the user may edit, leaving the
    /// others unchanged and listed, as Paratext does.
    /// </summary>
    public class NumberedMarkersPlugin : IParatextStandalonePlugin
    {
        public const string PluginName = "Numbered Markers";
        public const string PublisherName = "Phil Leckrone (SIL)";

        private const string DefaultProjectsDir = @"C:\My Paratext 9 Projects";

        public string Name
        {
            get { return PluginName; }
        }

        public Version Version
        {
            get { return new Version(Strings.ToolVersion); }
        }

        public string VersionString
        {
            get { return Strings.ToolVersion; }
        }

        public string Publisher
        {
            get { return PublisherName; }
        }

        public IEnumerable<PluginMenuEntry> PluginMenuEntries
        {
            get
            {
                var entry = new PluginMenuEntry(Strings.English().MenuText, OnMenuClicked, PluginMenuLocation.ScrTextTools);
                entry.LocalizedTextNeeded += LocalizeMenuText;
                yield return entry;
            }
        }

        /// <summary>Menu text in Paratext's interface language; null keeps English.</summary>
        internal static string LocalizeMenuText(string defaultText, string locale)
        {
            string lang = string.IsNullOrEmpty(locale) ? "" : locale.ToLowerInvariant();
            if (lang.StartsWith("es", StringComparison.Ordinal)) return Strings.Spanish().MenuText;
            if (lang.StartsWith("fr", StringComparison.Ordinal)) return Strings.French().MenuText;
            return null;
        }

        public string GetDescription(string locale)
        {
            return Strings.For(locale).Description;
        }

        public IDataFileMerger GetMerger(IPluginHost host, string dataIdentifier)
        {
            return null;   // the plugin keeps no project data
        }

        private void OnMenuClicked(IPluginHost host, IParatextChildState state)
        {
            Strings strings = Strings.For(UiLocale(host));
            try
            {
                IProject project = state != null ? state.Project : null;
                if (project == null || project.IsResource)
                {
                    Report(host, strings, strings.OpenProjectFirst, null);
                    return;
                }
                var source = new ParatextBookSource(this, project, strings);
                using (var form = new NumberedMarkersForm(source, strings))
                    form.ShowDialog();
            }
            catch (Exception ex)
            {
                // Never let an error escape into Paratext.
                Report(host, strings, string.Format(strings.Error, ex.Message), ex);
            }
        }

        private void Report(IPluginHost host, Strings strings, string message, Exception ex)
        {
            try
            {
                if (host != null)
                    host.Log(this, message + (ex != null ? "\n" + ex : ""));
            }
            catch (Exception) { }
            MessageBox.Show(message, strings.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string UiLocale(IPluginHost host)
        {
            try
            {
                if (host != null && host.UserSettings != null)
                    return host.UserSettings.UiLocale;
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>
        /// The Paratext projects folder: Settings_Directory under SOFTWARE\Paratext\8 (Paratext 9
        /// still uses that key), checked in both registry views, then for the current user.
        /// Same as the AutoCorrect plugin.
        /// </summary>
        internal static string ProjectsDirectory()
        {
            foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                string dir = ReadSettingsDirectory(RegistryHive.LocalMachine, view);
                if (dir != null) return dir;
            }
            string user = ReadSettingsDirectory(RegistryHive.CurrentUser, RegistryView.Default);
            return user ?? DefaultProjectsDir;
        }

        private static string ReadSettingsDirectory(RegistryHive hive, RegistryView view)
        {
            try
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, view))
                using (RegistryKey key = root.OpenSubKey(@"SOFTWARE\Paratext\8"))
                {
                    if (key == null) return null;
                    string dir = key.GetValue("Settings_Directory") as string;
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                        return dir.TrimEnd('\\');
                }
            }
            catch (Exception) { }
            return null;
        }

        /// <summary>
        /// The project's folder: projectsDir\shortName when its Settings.xml Guid matches the
        /// project ID (or has no readable Guid); otherwise the subfolder whose Guid matches.
        /// Same as the AutoCorrect plugin.
        /// </summary>
        internal static string ResolveProjectDir(string projectsDir, string shortName, string projectId)
        {
            if (string.IsNullOrEmpty(projectsDir) || !Directory.Exists(projectsDir)) return null;

            string byName = null;
            if (!string.IsNullOrEmpty(shortName))
            {
                string candidate = Path.Combine(projectsDir, shortName);
                string settings = Path.Combine(candidate, "Settings.xml");
                if (File.Exists(settings))
                {
                    byName = candidate;
                    string guid = ReadProjectGuid(settings);
                    if (guid == null || string.IsNullOrEmpty(projectId) || SameId(guid, projectId))
                        return candidate;
                }
            }

            if (string.IsNullOrEmpty(projectId)) return byName;
            try
            {
                foreach (string dir in Directory.GetDirectories(projectsDir))
                {
                    string settings = Path.Combine(dir, "Settings.xml");
                    if (File.Exists(settings) && SameId(ReadProjectGuid(settings), projectId))
                        return dir;
                }
            }
            catch (Exception) { }
            return byName;
        }

        private static bool SameId(string a, string b)
        {
            return a != null && b != null && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        internal static string ReadProjectGuid(string settingsPath)
        {
            try
            {
                var xmlSettings = new XmlReaderSettings();
                xmlSettings.DtdProcessing = DtdProcessing.Ignore;
                using (XmlReader reader = XmlReader.Create(settingsPath, xmlSettings))
                {
                    if (reader.ReadToFollowing("Guid"))
                    {
                        string guid = reader.ReadElementContentAsString();
                        return string.IsNullOrEmpty(guid) ? null : guid.Trim();
                    }
                }
            }
            catch (Exception) { }
            return null;
        }
    }

    /// <summary>
    /// The plugin API's view of a project. Text comes in and goes out through Paratext
    /// (GetUSFM / PutUSFM), which normalizes it as its own editor does; permissions are
    /// Paratext's own (ProjectUserAccess.xml); each write takes a lock on just that chapter.
    /// </summary>
    internal sealed class ParatextBookSource : IBookSource
    {
        private readonly IPluginObject plugin;
        private readonly IProject project;
        private readonly List<BookId> books;
        private readonly Strings strings;
        private string backupDirectory;
        private string projectDirectory;
        private bool projectDirectoryResolved;

        public ParatextBookSource(IPluginObject plugin, IProject project, Strings strings)
        {
            this.plugin = plugin;
            this.project = project;
            this.strings = strings;
            books = project.AvailableBooks.OrderBy(b => b.Number).Select(b => new BookId(b.Number, b.Code)).ToList();
        }

        public string ProjectName
        {
            get { return project.ShortName; }
        }

        public IList<BookId> Books
        {
            get { return books; }
        }

        public string GetBook(int bookNum)
        {
            return project.GetUSFM(bookNum, 0);
        }

        public string GetChapter(int bookNum, int chapter)
        {
            return project.GetUSFM(bookNum, chapter);
        }

        public bool CanEdit(int bookNum, int chapter)
        {
            return project.CanEdit(plugin, bookNum, chapter);
        }

        public WriteOutcome WriteChapter(int bookNum, int chapter, string usfm, out string error)
        {
            error = null;
            // RequestWriteLock returns null when the user may not edit the chapter or someone
            // else holds the lock. The lock is held only for this one write.
            IWriteLock writeLock = project.RequestWriteLock(plugin, ReleaseRequested, bookNum, chapter);
            if (writeLock == null)
                return CanEdit(bookNum, chapter) ? WriteOutcome.Busy : WriteOutcome.NoPermission;
            try
            {
                project.PutUSFM(writeLock, usfm, bookNum);
                writeLock.SendNotifications();
                return WriteOutcome.Written;
            }
            catch (Exception ex)
            {
                error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return error.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0
                    ? WriteOutcome.NoPermission : WriteOutcome.Failed;
            }
            finally
            {
                writeLock.Dispose();
            }
        }

        private static void ReleaseRequested(IWriteLock writeLock)
        {
            // Each lock is released right after its write, so there is nothing to hand back early.
        }

        /// <summary>
        /// Makes a history point and confirms it. IProject.MakeHistoryPoint is void and Paratext's
        /// commit fails silently (for example when it cannot lock the whole project); without
        /// force it also makes no point when nothing changed since the last one. So the commit
        /// is forced, and the point counts as made only if the project's Mercurial changelog
        /// (.hg\store\00changelog.*) changed.
        /// </summary>
        public bool MakeHistoryPoint(string message, out string error)
        {
            error = null;
            string dir = ProjectDirectory;
            string store = dir != null ? Path.Combine(Path.Combine(dir, ".hg"), "store") : null;
            if (store == null || !Directory.Exists(store))
            {
                error = strings.HistoryNoFolder;
                return false;
            }
            string before = ChangelogState(store);
            try
            {
                project.MakeHistoryPoint(message, true);
            }
            catch (Exception ex)
            {
                error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return false;
            }
            if (ChangelogState(store) == before)
            {
                error = strings.HistoryNotRecorded;
                return false;
            }
            return true;
        }

        /// <summary>Size and time of each 00changelog.* file: changes with every commit.</summary>
        private static string ChangelogState(string store)
        {
            var parts = new List<string>();
            foreach (string path in Directory.GetFiles(store, "00changelog.*").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var info = new FileInfo(path);
                parts.Add(info.Name + ":" + info.Length + ":" + info.LastWriteTimeUtc.Ticks);
            }
            return string.Join("|", parts.ToArray());
        }

        /// <summary>The project's folder (null when it cannot be found).</summary>
        private string ProjectDirectory
        {
            get
            {
                if (!projectDirectoryResolved)
                {
                    projectDirectory = NumberedMarkersPlugin.ResolveProjectDir(
                        NumberedMarkersPlugin.ProjectsDirectory(), project.ShortName, project.ID);
                    projectDirectoryResolved = true;
                }
                return projectDirectory;
            }
        }

        public string BackupDirectory
        {
            get
            {
                if (backupDirectory == null)
                {
                    string dir = ProjectDirectory;
                    backupDirectory = dir != null
                        ? Path.Combine(Path.Combine(dir, "local"), "NumberedMarkers")
                        : Path.Combine(Path.Combine(Path.GetTempPath(), "NumberedMarkers"), project.ShortName);
                }
                return backupDirectory;
            }
        }
    }
}
