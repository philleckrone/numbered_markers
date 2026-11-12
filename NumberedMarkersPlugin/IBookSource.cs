using System.Collections.Generic;

namespace NumberedMarkers
{
    public sealed class BookId
    {
        public readonly int Number;
        public readonly string Code;

        public BookId(int number, string code)
        {
            Number = number;
            Code = code;
        }
    }

    public enum WriteOutcome
    {
        Written,
        NoPermission,
        Busy,
        Failed,
    }

    /// <summary>
    /// What the window needs from a project. ParatextBookSource wraps the plugin API's IProject;
    /// the tests use a folder of SFM files.
    /// </summary>
    public interface IBookSource
    {
        string ProjectName { get; }

        /// <summary>The project's books, in canonical order.</summary>
        IList<BookId> Books { get; }

        /// <summary>The whole book, or null.</summary>
        string GetBook(int bookNum);

        /// <summary>One chapter (chapter 1 starts with \id), or null.</summary>
        string GetChapter(int bookNum, int chapter);

        /// <summary>Whether the user may edit the chapter (from ProjectUserAccess.xml).</summary>
        bool CanEdit(int bookNum, int chapter);

        WriteOutcome WriteChapter(int bookNum, int chapter, string usfm, out string error);

        /// <summary>
        /// Records a point in the project history. Returns false (with the reason) when the point
        /// could not be made or confirmed; nothing may be changed then.
        /// </summary>
        bool MakeHistoryPoint(string message, out string error);

        /// <summary>Folder for the .bak backups.</summary>
        string BackupDirectory { get; }
    }
}
