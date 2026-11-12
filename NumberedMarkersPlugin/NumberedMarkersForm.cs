using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace NumberedMarkers
{
    /// <summary>
    /// Lists the mixed marker families of a project with the books the user can and cannot edit,
    /// and converts the ticked families in every chapter the user may edit.
    /// </summary>
    public sealed class NumberedMarkersForm : Form
    {
        private readonly IBookSource source;
        private readonly Strings s;
        private ScanResult scan;
        private ConversionRunner runner;
        private BookChoice books;
        /// <summary>True while FillGrid builds the rows (no per-row refresh then).</summary>
        private bool filling;

        private readonly Label lblProject = new Label();
        private readonly Label lblIntro = new Label();
        private readonly Label lblMixed = new Label();
        private readonly CheckBox chkOneForm = new CheckBox();
        private readonly Button btnBooks = new Button();
        private readonly Label lblWarning = new Label();
        private readonly DataGridView grid = new DataGridView();
        private readonly Label lblConsistent = new Label();
        private readonly Label lblOther = new Label();
        private readonly Button btnAll = new Button();
        private readonly Button btnClear = new Button();
        private readonly Button btnConvert = new Button();
        private readonly Button btnClose = new Button();
        private readonly Label lblResults = new Label();
        private readonly TextBox txtResults = new TextBox();

        public NumberedMarkersForm(IBookSource source, Strings strings)
        {
            this.source = source;
            s = strings;
            books = BookChoice.Load(BookChoice.SettingsPath(source.BackupDirectory));
            BuildLayout();
            Shown += delegate { Rescan(); };
        }

        private void BuildLayout()
        {
            Text = s.Title + " " + Strings.ToolVersion + " - " + source.ProjectName;
            Name = "NumberedMarkersForm";
            AutoScaleMode = AutoScaleMode.Font;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(1150, 720);
            MinimumSize = new Size(700, 450);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            var table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.Padding = new Padding(10);
            table.ColumnCount = 1;
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            for (int i = 0; i < 10; i++) table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles[4] = new RowStyle(SizeType.Percent, 62f);
            table.RowStyles[9] = new RowStyle(SizeType.Percent, 38f);

            lblProject.AutoSize = true;
            lblProject.Font = new Font(Font.FontFamily, 11f, FontStyle.Bold);
            lblProject.Text = string.Format(s.ProjectLabel, source.ProjectName);
            lblProject.Margin = new Padding(3, 0, 3, 4);

            ConfigureWrappingLabel(lblIntro, table);
            lblIntro.Text = s.Intro + " " + s.IntroRemove;

            lblMixed.AutoSize = true;
            lblMixed.Font = new Font(Font, FontStyle.Bold);
            lblMixed.Text = s.MixedHeader;
            lblMixed.Margin = new Padding(3, 3, 24, 2);

            // Off by default: mixed markers are the priority.
            chkOneForm.AutoSize = true;
            chkOneForm.Text = s.ShowOneForm;
            chkOneForm.Name = "ShowOneForm";
            chkOneForm.Margin = new Padding(3, 0, 3, 0);
            chkOneForm.CheckedChanged += delegate { if (scan != null) FillGrid(); };

            var header = new FlowLayoutPanel();
            header.AutoSize = true;
            header.WrapContents = true;
            header.Margin = new Padding(0, 6, 0, 2);
            header.Controls.Add(lblMixed);
            header.Controls.Add(chkOneForm);
            SetupButton(btnBooks, BooksButtonText(), "Books");
            btnBooks.Margin = new Padding(16, 0, 0, 0);
            btnBooks.Click += delegate { ChooseBooks(); };
            header.Controls.Add(btnBooks);

            ConfigureWrappingLabel(lblWarning, table);
            lblWarning.ForeColor = Color.FromArgb(0x8A, 0x1C, 0x00);
            lblWarning.Font = new Font(Font, FontStyle.Bold);
            lblWarning.Name = "Warning";
            lblWarning.Visible = false;

            BuildGrid();

            ConfigureWrappingLabel(lblConsistent, table);
            lblConsistent.Margin = new Padding(3, 6, 3, 2);
            ConfigureWrappingLabel(lblOther, table);
            lblOther.ForeColor = SystemColors.GrayText;
            lblOther.Text = s.OtherNote;
            lblOther.Visible = false;

            var buttons = new FlowLayoutPanel();
            buttons.AutoSize = true;
            buttons.FlowDirection = FlowDirection.LeftToRight;
            buttons.WrapContents = false;
            buttons.Margin = new Padding(0, 6, 0, 6);
            SetupButton(btnAll, s.SelectAll, "SelectAll");
            SetupButton(btnClear, s.Clear, "Clear");
            SetupButton(btnConvert, s.ConvertButton, "Convert");
            btnConvert.Font = new Font(Font, FontStyle.Bold);
            SetupButton(btnClose, s.Close, "Close");
            buttons.Controls.Add(btnAll);
            buttons.Controls.Add(btnClear);
            buttons.Controls.Add(btnConvert);
            buttons.Controls.Add(btnClose);
            btnAll.Click += delegate { SetAllChecks(true); };
            btnClear.Click += delegate { SetAllChecks(false); };
            btnConvert.Click += delegate { ConvertSelected(); };
            btnClose.Click += delegate { Close(); };
            CancelButton = btnClose;

            ConfigureWrappingLabel(lblResults, table);
            lblResults.Font = new Font(Font, FontStyle.Bold);
            lblResults.Text = s.ResultsLabel;
            lblResults.Margin = new Padding(3, 4, 3, 2);

            txtResults.Multiline = true;
            txtResults.ReadOnly = true;
            txtResults.ScrollBars = ScrollBars.Both;
            txtResults.WordWrap = false;
            txtResults.Dock = DockStyle.Fill;
            txtResults.Name = "Results";
            txtResults.AccessibleName = s.ResultsHeader.TrimEnd(':');
            txtResults.BackColor = SystemColors.Window;

            table.Controls.Add(lblProject, 0, 0);
            table.Controls.Add(lblIntro, 0, 1);
            table.Controls.Add(header, 0, 2);
            table.Controls.Add(lblWarning, 0, 3);
            table.Controls.Add(grid, 0, 4);
            table.Controls.Add(lblConsistent, 0, 5);
            table.Controls.Add(lblOther, 0, 6);
            table.Controls.Add(buttons, 0, 7);
            table.Controls.Add(lblResults, 0, 8);
            table.Controls.Add(txtResults, 0, 9);
            Controls.Add(table);
        }

        private static void ConfigureWrappingLabel(Label label, TableLayoutPanel table)
        {
            label.AutoSize = true;
            label.Margin = new Padding(3, 2, 3, 2);
            label.MaximumSize = new Size(1100, 0);
            table.SizeChanged += delegate { label.MaximumSize = new Size(Math.Max(200, table.ClientSize.Width - 30), 0); };
        }

        private static void SetupButton(Button b, string text, string name)
        {
            b.Text = text;
            b.Name = name;
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            b.Padding = new Padding(8, 2, 8, 2);
            b.Margin = new Padding(0, 0, 8, 0);
        }

        private void BuildGrid()
        {
            grid.Name = "MixedMarkers";
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.BackgroundColor = SystemColors.Window;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            var check = new DataGridViewCheckBoxColumn();
            check.Name = "Convert";
            check.HeaderText = s.ColConvert;
            check.Width = 70;
            // At the top of the cell, level with the marker name, also in tall (wrapped) rows.
            check.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopCenter;
            grid.Columns.Add(check);
            AddTextColumn("Marker", s.ColMarker, 70, false);
            AddTextColumn("Kind", s.ColKind, 80, false);
            var changeTo = new DataGridViewComboBoxColumn();
            changeTo.Name = "ChangeTo";
            changeTo.HeaderText = s.ColChangeTo;
            changeTo.Width = 110;
            changeTo.SortMode = DataGridViewColumnSortMode.NotSortable;
            changeTo.FlatStyle = FlatStyle.Flat;
            grid.Columns.Add(changeTo);
            AddTextColumn("Unnumbered", s.ColBare, 85, false);
            AddTextColumn("Numbered", s.ColLevels, 170, true);
            AddTextColumn("Other", s.ColOther, 90, true);
            AddTextColumn("CanConvert", s.ColCan, 150, true);
            AddTextColumn("CannotEdit", s.ColCannot, 150, true);
            AddTextColumn("Where", s.ColWhere, 330, true);
            grid.Columns["Unnumbered"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopRight;
            grid.Columns["CannotEdit"].DefaultCellStyle.ForeColor = Color.FromArgb(0x8A, 0x1C, 0x00);
            grid.Columns["Where"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;

            // Commit a tick or a "Change to" choice at once, so the value is current when Convert
            // is clicked and the row can show the books for the chosen direction.
            grid.CurrentCellDirtyStateChanged += delegate
            {
                if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (filling || e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "ChangeTo") return;
                RefreshRow(grid.Rows[e.RowIndex]);
                UpdateWarning();
            };
            // A combo value that is not in its list must not raise Windows Forms' error dialog.
            grid.DataError += delegate(object sender, DataGridViewDataErrorEventArgs e) { e.ThrowException = false; };
        }

        private void AddTextColumn(string name, string header, int width, bool wrap)
        {
            var col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = header;
            col.Width = width;
            col.ReadOnly = true;
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
            if (wrap) col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.Columns.Add(col);
        }

        private void Rescan()
        {
            Cursor old = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            lblMixed.Text = string.Format(s.Scanning, source.ProjectName);
            lblMixed.Refresh();
            try
            {
                var result = MarkerScanner.NewResult();
                foreach (BookId book in source.Books)
                    if (!books.IsExcluded(book))
                        MarkerScanner.ScanBook(result, book.Code, source.GetBook(book.Number), s.IntroLabel);
                btnBooks.Text = BooksButtonText();
                scan = result;
                runner = new ConversionRunner(source);
                runner.NotSavedText = s.NotSaved;
                FillGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(s.Error, ex.Message), s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                lblMixed.Text = s.MixedHeader;
                Cursor.Current = old;
            }
        }

        private string BooksButtonText()
        {
            return string.Format(s.BooksButton, source.Books.Count(b => !books.IsExcluded(b)), source.Books.Count);
        }

        /// <summary>The book chooser; OK saves the choice for the project and scans again.</summary>
        private void ChooseBooks()
        {
            using (var dialog = new BookChooserForm(source.Books, books.IsExcluded, s))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                books.ExcludedBooks = dialog.ExcludedBooks;
            }
            string path = BookChoice.SettingsPath(source.BackupDirectory);
            if (path != null)
            {
                try { books.Save(path); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, string.Format(s.SettingsNotSaved, ex.Message), s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            Rescan();
        }

        private string DirectionText(string family, ChangeDirection direction)
        {
            return string.Format(direction == ChangeDirection.RemoveNumber ? s.DirRemove : s.DirAdd, family);
        }

        private ChangeDirection RowDirection(DataGridViewRow row)
        {
            string family = (string)row.Tag;
            object v = row.Cells["ChangeTo"].Value;
            return v is string && (string)v == DirectionText(family, ChangeDirection.RemoveNumber)
                ? ChangeDirection.RemoveNumber : ChangeDirection.AddNumber;
        }

        /// <summary>Fills the book and location cells of a row for its chosen direction.</summary>
        private void RefreshRow(DataGridViewRow row)
        {
            FamilyStats f = scan.ByName[(string)row.Tag];
            ChangeDirection direction = RowDirection(row);
            List<BookPlan> plans = runner.Plan(scan, f.Name, direction);
            row.Cells["CanConvert"].Value = string.Join(", ", plans.Where(p => p.Editable.Count > 0)
                .Select(p => p.Describe(p.Editable)).ToArray());
            row.Cells["CannotEdit"].Value = string.Join(", ", plans.Where(p => p.NotEditable.Count > 0)
                .Select(p => p.Describe(p.NotEditable)).ToArray());
            row.Cells["Where"].Value = f.PlacesFor(direction).WhereText(s.More);
        }

        private bool IsMixedRow(DataGridViewRow row)
        {
            return (string)row.Cells["Kind"].Value == s.KindMixed;
        }

        /// <summary>
        /// The mixed families, then (when the checkbox is ticked) the families that use only one
        /// form but could still be changed. A rebuild keeps each family's tick and direction.
        /// </summary>
        private void FillGrid()
        {
            var ticked = new Dictionary<string, bool>();
            var chosen = new Dictionary<string, object>();
            foreach (DataGridViewRow old in grid.Rows)
            {
                object v = old.Cells["Convert"].Value;
                ticked[(string)old.Tag] = v is bool && (bool)v;
                chosen[(string)old.Tag] = old.Cells["ChangeTo"].Value;
            }

            filling = true;
            grid.Rows.Clear();
            foreach (FamilyStats f in scan.Mixed)
            {
                DataGridViewRow row = AddRow(f, s.KindMixed);
                var combo = (DataGridViewComboBoxCell)row.Cells["ChangeTo"];
                combo.Items.Add(DirectionText(f.Name, ChangeDirection.AddNumber));
                if (f.CanRemoveNumber)
                    combo.Items.Add(DirectionText(f.Name, ChangeDirection.RemoveNumber));
                else
                    MakeFixed(combo);
                combo.Value = DirectionText(f.Name, f.DefaultDirection);
            }
            if (chkOneForm.Checked)
            {
                foreach (FamilyStats f in scan.OneForm)
                {
                    DataGridViewRow row = AddRow(f, s.KindOneForm);
                    row.DefaultCellStyle.BackColor = Color.FromArgb(0xF3, 0xF3, 0xF3);
                    var combo = (DataGridViewComboBoxCell)row.Cells["ChangeTo"];
                    string only = DirectionText(f.Name, f.OneFormDirection.Value);
                    combo.Items.Add(only);
                    MakeFixed(combo);
                    combo.Value = only;
                }
            }
            foreach (DataGridViewRow row in grid.Rows)
            {
                string family = (string)row.Tag;
                bool tick;
                if (ticked.TryGetValue(family, out tick)) row.Cells["Convert"].Value = tick;
                object direction;
                var combo = (DataGridViewComboBoxCell)row.Cells["ChangeTo"];
                if (chosen.TryGetValue(family, out direction) && direction != null && combo.Items.Contains(direction))
                    combo.Value = direction;
                RefreshRow(row);
            }
            filling = false;
            UpdateWarning();
            grid.Visible = grid.Rows.Count > 0;

            string consistent = string.Join(", ", scan.Consistent.Select(f =>
                f.Bare > 0 ? "\\" + f.Name + " (" + f.Bare + ")" : f.LevelList(true)).ToArray());
            lblConsistent.Text = string.Format(s.ConsistentHeader, consistent.Length > 0 ? consistent : s.ConsistentNone);
            lblOther.Visible = scan.Families.Any(f => f.IsUsed && f.LevelList(false).Length > 0);
        }

        private DataGridViewRow AddRow(FamilyStats f, string kind)
        {
            int index = grid.Rows.Add(false, "\\" + f.Name, kind, null, f.Bare, f.LevelList(true), f.LevelList(false), "", "", "");
            DataGridViewRow row = grid.Rows[index];
            row.Tag = f.Name;
            return row;
        }

        /// <summary>Only one way is possible: show it as plain text.</summary>
        private static void MakeFixed(DataGridViewComboBoxCell combo)
        {
            combo.ReadOnly = true;
            combo.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
        }

        /// <summary>The permission line and the Convert controls, for the rows and directions shown.</summary>
        private void UpdateWarning()
        {
            bool anyEditable = false;
            bool anyBlocked = false;
            int mixed = 0;
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (IsMixedRow(row)) mixed++;
                if (((string)row.Cells["CanConvert"].Value ?? "").Length > 0) anyEditable = true;
                if (((string)row.Cells["CannotEdit"].Value ?? "").Length > 0) anyBlocked = true;
            }
            bool canConvert = grid.Rows.Count > 0 && anyEditable;
            if (grid.Rows.Count == 0)
                ShowWarning(s.MixedNone, SystemColors.ControlText);
            else if (!anyEditable && runner.NoPermissionAtAll())
                ShowWarning(string.Format(s.NoPermissionProject, source.ProjectName), null);
            else if (!anyEditable)
                ShowWarning(s.NoPermissionBooks, null);
            else if (mixed == 0)
                ShowWarning(s.MixedNone, SystemColors.ControlText);
            else if (anyBlocked)
                ShowWarning(s.SomeNotEditable, null);
            else
                lblWarning.Visible = false;

            grid.Columns["Convert"].Visible = canConvert;
            btnClear.Enabled = btnConvert.Enabled = canConvert;
            btnAll.Enabled = canConvert && mixed > 0;
        }

        private void ShowWarning(string text, Color? color)
        {
            lblWarning.Text = text;
            lblWarning.ForeColor = color.HasValue ? color.Value : Color.FromArgb(0x8A, 0x1C, 0x00);
            lblWarning.Visible = true;
        }

        /// <summary>Select all ticks only the mixed markers (the priority); Clear clears every row.</summary>
        private void SetAllChecks(bool value)
        {
            grid.EndEdit();
            foreach (DataGridViewRow row in grid.Rows)
                if (!value || IsMixedRow(row))
                    row.Cells["Convert"].Value = value;
        }

        /// <summary>The ticked families with their chosen direction, in table order.</summary>
        private Dictionary<string, ChangeDirection> CheckedFamilies(out string description)
        {
            grid.EndEdit();
            var chosen = new Dictionary<string, ChangeDirection>();
            var parts = new List<string>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                object v = row.Cells["Convert"].Value;
                if (!(v is bool && (bool)v)) continue;
                string family = (string)row.Tag;
                ChangeDirection direction = RowDirection(row);
                chosen[family] = direction;
                parts.Add(DirectionText(family, direction));
            }
            description = string.Join(", ", parts.ToArray());
            return chosen;
        }

        private void ConvertSelected()
        {
            string markers;
            Dictionary<string, ChangeDirection> names = CheckedFamilies(out markers);
            if (names.Count == 0)
            {
                MessageBox.Show(this, s.NothingSelected, s.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int books = runner.Plan(scan, names).Count(p => p.Editable.Count > 0);
            string question = string.Format(s.Confirm, markers, books, source.BackupDirectory);
            if (MessageBox.Show(this, question, s.ConfirmTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            Cursor old = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            ConversionReport report;
            try
            {
                report = runner.Run(scan, names, string.Format(s.HistoryMessage, markers));
            }
            catch (Exception ex)
            {
                Cursor.Current = old;
                MessageBox.Show(this, string.Format(s.Error, ex.Message), s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Cursor.Current = old;
            if (report.Stopped)
            {
                // No history point, so nothing was changed: say so and leave the table as it is.
                string stopped = string.Format(s.HistoryStopped, report.HistoryError);
                txtResults.Text = stopped.Replace("\r\n", "\n").Replace("\n", "\r\n");
                MessageBox.Show(this, stopped, s.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            txtResults.Text = FormatReport(report);
            Rescan();
        }

        private string FormatReport(ConversionReport report)
        {
            var sb = new StringBuilder();
            foreach (Change c in report.Changes)
                sb.AppendLine(string.Format(c.Direction == ChangeDirection.RemoveNumber ? s.ResChangeRemove : s.ResChange,
                    c.Book, c.Family, c.Count));
            if (report.Changes.Count > 0)
                sb.AppendLine(string.Format(s.ResTotal, report.Total, report.BooksChanged));
            else
                sb.AppendLine(s.ResNothing);
            if (report.NoPermission.Count > 0)
                sb.AppendLine(string.Format(s.ResNoPermission, string.Join(", ", report.NoPermission.ToArray())));
            if (report.Busy.Count > 0)
                sb.AppendLine(string.Format(s.ResBusy, string.Join(", ", report.Busy.ToArray())));
            foreach (string f in report.Failed)
                sb.AppendLine(string.Format(s.ResFailed, f));
            if (report.Backups.Count > 0)
                sb.AppendLine(string.Format(s.ResBackups, source.BackupDirectory));
            return sb.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
    }
}
