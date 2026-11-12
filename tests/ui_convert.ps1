<#
.SYNOPSIS
  Drives the Numbered Markers window (in the test host, on a copy of TestProject\PDT15 plus
  tests\fixtures\70TOBPDT15.SFM) with UI Automation, using pretend edit rights:
    1. TOB, XXA and XXD chapters 2-3 editable. \io and \li (no level 2) default to removing
       the 1; the \li row is switched to adding it. Select all, Convert, Yes. Only those
       chapters change; the results list the others as not changed for lack of permission.
    2. No rights at all: the project message is shown and Convert is disabled.
    3. Only PHM editable (none of the markers to change are there): the "cannot edit any of these books" message.
    4. The history point is refused (NM_NOHISTORY=1): Convert stops with the history message and
       no file changes.
  Scenario 1 also ticks "Also show markers that use only one form": the one-form rows appear,
  and "Select all" still ticks only the mixed rows (the result is unchanged).
  Saves screenshots (English, Spanish and French) next to the temp copy. Exit code 0 = all passed.
#>
param([string]$ExePath = '')
$ErrorActionPreference = 'Stop'
if (-not $ExePath) { $ExePath = Join-Path $PSScriptRoot 'TestHost\bin\Release\NumberedMarkersTestHost.exe' }
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms, System.Drawing
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]

$repo = Split-Path (Split-Path $PSScriptRoot)
$tmp = Join-Path $env:TEMP ('numbered_markers_ui_' + [guid]::NewGuid().ToString('N'))
$failures = 0
function Check([bool]$ok, [string]$what) {
    if ($ok) { Write-Host "ok   $what" } else { Write-Host "FAIL $what"; $script:failures++ }
}
function NewProject([string]$name) {
    $dir = Join-Path $tmp $name
    New-Item -ItemType Directory -Force $dir | Out-Null
    Get-ChildItem (Join-Path $repo 'TestProject\PDT15') -Filter '*PDT15.SFM' | Copy-Item -Destination $dir
    Copy-Item (Join-Path $PSScriptRoot 'fixtures\70TOBPDT15.SFM') $dir
    return $dir
}
function FindById($root, [string]$id) {
    $c = New-Object System.Windows.Automation.PropertyCondition($AE::AutomationIdProperty, $id)
    return $root.FindFirst($TS::Descendants, $c)
}
function FindByName($root, [string]$name, $type) {
    $c1 = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $name)
    $c2 = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $type)
    return $root.FindFirst($TS::Descendants, (New-Object System.Windows.Automation.AndCondition($c1, $c2)))
}
function Invoke($el) { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }
Add-Type -Namespace NM -Name U -MemberDefinition @'
[DllImport("user32.dll")] public static extern bool PostMessage(System.IntPtr h, int msg, System.IntPtr w, System.IntPtr l);
[DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int SendMessage(System.IntPtr h, int msg, int w, System.Text.StringBuilder l);
[DllImport("user32.dll")] public static extern int SendMessage(System.IntPtr h, int msg, int w, int l);
[DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(System.IntPtr h, System.Text.StringBuilder s, int n);
'@ -ErrorAction SilentlyContinue
# WinForms buttons show up as panes without InvokePattern: click them with BM_CLICK.
function ClickByName($win, [string]$name) {
    $c = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $name)
    $el = $win.FindFirst($TS::Descendants, $c)
    if (-not $el) { throw "no control named $name" }
    [NM.U]::PostMessage([IntPtr]$el.Current.NativeWindowHandle, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
}
function ElementByName($win, [string]$name) {
    $c = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, $name)
    return $win.FindFirst($TS::Descendants, $c)
}
# The text of the label that starts with the given words, or ''.
function TextStartingWith($win, [string]$prefix) {
    $c = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::Text)
    foreach ($e in $win.FindAll($TS::Descendants, $c)) { if ($e.Current.Name.StartsWith($prefix)) { return $e.Current.Name } }
    return ''
}
# The results box: the window's multi-line WinForms EDIT control (UI Automation shows it
# as an unnamed pane), read with WM_GETTEXT.
function CellValue($win, [string]$name) {
    $el = ElementByName $win $name
    if (-not $el) { return $null }
    return $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).Current.Value
}
function SetCellValue($win, [string]$name, [string]$value) {
    $el = ElementByName $win $name
    $el.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($value)
    Start-Sleep -Milliseconds 500
}
function ElementByPrefix($win, [string]$prefix) {
    foreach ($e in $win.FindAll($TS::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
        if ($e.Current.Name.StartsWith($prefix)) { return $e }
    }
    return $null
}
function ClickByPrefix($win, [string]$prefix) {
    $el = ElementByPrefix $win $prefix
    if (-not $el) { throw "no control starting with $prefix" }
    [NM.U]::PostMessage([IntPtr]$el.Current.NativeWindowHandle, 0x00F5, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 800
}
function RowCount($win) {
    $n = 0
    while (ElementByName $win ('Marker Row ' + $n)) { $n++ }
    return $n
}
function ResultsText($win) {
    $c = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $CT::Pane)
    foreach ($e in $win.FindAll($TS::Descendants, $c)) {
        $h = [IntPtr]$e.Current.NativeWindowHandle
        if ($h -eq [IntPtr]::Zero) { continue }
        $cls = New-Object System.Text.StringBuilder 256
        [NM.U]::GetClassName($h, $cls, 256) | Out-Null
        if ($cls.ToString() -notmatch 'EDIT') { continue }
        $len = [NM.U]::SendMessage($h, 0x000E, 0, 0)
        $sb = New-Object System.Text.StringBuilder ($len + 1)
        [NM.U]::SendMessage($h, 0x000D, $len + 1, $sb) | Out-Null
        return $sb.ToString()
    }
    return ''
}
function StartHost([string]$dir, [string]$spec, [string]$lang) {
    $bak = Join-Path $dir 'backups'
    $args = '"form" "' + $dir + '" "' + $spec + '" "' + $lang + '" "' + $bak + '"'
    $p = Start-Process -FilePath $ExePath -ArgumentList $args -PassThru
    $win = $null
    for ($i = 0; $i -lt 60 -and -not $win; $i++) {
        Start-Sleep -Milliseconds 500; $p.Refresh()
        if ($p.MainWindowHandle -ne 0) { $win = $AE::FromHandle($p.MainWindowHandle) }
    }
    if (-not $win) { throw 'window did not appear' }
    # The scan runs when the window is shown; the header reads "Scanning ..." until it is done.
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Milliseconds 500
        if (-not (TextStartingWith $win 'Scanning') -and -not (TextStartingWith $win 'Revisando') -and -not (TextStartingWith $win 'Analyse de')) { break }
    }
    Start-Sleep -Milliseconds 500
    return @($p, $win)
}
function Screenshot($win, [string]$path) {
    $r = $win.Current.BoundingRectangle
    $bmp = New-Object System.Drawing.Bitmap ([int]$r.Width), ([int]$r.Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen([int]$r.X, [int]$r.Y, 0, 0, $bmp.Size)
    $bmp.Save($path)
    $g.Dispose(); $bmp.Dispose()
}
function Stop($p) { if (-not $p.HasExited) { $p.Kill(); $p.WaitForExit() } }

try {
    # 1. Partial rights.
    $dir = NewProject 'partial'
    $orig = @{}
    Get-ChildItem $dir -Filter *.SFM | ForEach-Object { $orig[$_.Name] = [IO.File]::ReadAllBytes($_.FullName) }
    $p, $win = StartHost $dir 'TOB,XXA,XXD:2-3' 'en'
    Check ((TextStartingWith $win 'Some of these markers are in books you cannot edit') -ne '') 'partial: warning about books you cannot edit'
    Check ((ElementByName $win 'Convert selected markers').Current.IsEnabled) 'partial: Convert enabled'
    $version = [regex]::Match((Get-Content (Join-Path $repo 'Numbered_Markers\NumberedMarkersPlugin\Strings.cs') -Raw), 'ToolVersion = "([^"]+)"').Groups[1].Value
    Check ($win.Current.Name -eq ('Numbered Markers ' + $version + ' - PDT15')) ('title shows the version: ' + $win.Current.Name)
    Check ((TextStartingWith $win 'Results: after you click Convert') -ne '') 'results box has a label'
    Check ((CellValue $win 'Change to Row 0') -eq '\io1 → \io') 'io defaults to removing the 1'
    Check ((CellValue $win 'Change to Row 4') -eq '\li1 → \li') 'li defaults to removing the 1'
    SetCellValue $win 'Change to Row 4' '\li → \li1'
    Check ((CellValue $win 'Change to Row 4') -eq '\li → \li1') 'li switched to adding the 1'
    Check ((CellValue $win 'Books you can convert Row 4') -eq 'XXD 2, 3') 'li row now lists XXD 2, 3'
    Check ((RowCount $win) -eq 5) 'one-form markers hidden at first (5 mixed rows)'
    ClickByPrefix $win 'Also show markers that use only one form'
    Check ((RowCount $win) -eq 9) ('one-form rows shown: ' + (RowCount $win) + ' rows')
    Check ((CellValue $win 'Kind Row 5') -eq 'One form') 'row 5 is a one-form marker'
    Check ((CellValue $win 'Change to Row 4') -eq '\li → \li1') 'li keeps its chosen direction after the rebuild'
    Screenshot $win (Join-Path $tmp 'window_en.png')
    ClickByName $win 'Select all'
    Start-Sleep -Milliseconds 300
    ClickByName $win 'Convert selected markers'
    $confirm = $null
    for ($i = 0; $i -lt 20 -and -not $confirm; $i++) {
        Start-Sleep -Milliseconds 300
        $confirm = FindByName $AE::RootElement 'Convert markers' $CT::Window
        if (-not $confirm) { $confirm = FindByName $win 'Convert markers' $CT::Window }
    }
    Check ($confirm -ne $null) 'partial: confirmation shown'
    ClickByName $confirm 'Yes'
    $results = ''
    for ($i = 0; $i -lt 40 -and -not $results; $i++) {
        Start-Sleep -Milliseconds 500
        $results = ResultsText $win
    }
    Write-Host $results
    Check ($results -match 'Converted: 4745 change\(s\) in 3 book\(s\)') 'partial: 4745 changes in 3 books'
    Check ($results -match 'TOB: \\io1 -> \\io, 1 change') 'partial: TOB io1 -> io reported'
    Check ($results -match 'Not changed, because you do not have permission to edit: XXC, XXD 1, 4, 5') 'partial: not-permitted books listed'
    Check ($results -match 'XXE, XXF, CNC') 'partial: XXE, XXF, CNC listed'
    $changed = @(Get-ChildItem $dir -Filter *.SFM | Where-Object {
        -not [Linq.Enumerable]::SequenceEqual([byte[]]$orig[$_.Name], [byte[]][IO.File]::ReadAllBytes($_.FullName)) } | ForEach-Object { $_.Name })
    Check (($changed -join ',') -eq '70TOBPDT15.SFM,94XXAPDT15.SFM,97XXDPDT15.SFM') ('partial: changed files ' + ($changed -join ','))
    Check (@(Get-ChildItem (Join-Path $dir 'backups') -Filter 'Backup_*.bak').Count -eq 3) 'partial: 3 .bak backups'
    Stop $p

    # 2. No rights at all.
    $dir = NewProject 'none'
    $p, $win = StartHost $dir 'NONE' 'en'
    Check ((TextStartingWith $win 'You do not have permission to edit the PDT15 project, so these markers cannot be converted.') -ne '') 'none: project message'
    Check (-not (ElementByName $win 'Convert selected markers').Current.IsEnabled) 'none: Convert disabled'
    Stop $p

    # 3. Rights only to a book without any of the markers to change.
    $dir = NewProject 'phm'
    $p, $win = StartHost $dir 'PHM' 'en'
    Check ((TextStartingWith $win 'You cannot edit any of the books that have these markers') -ne '') 'phm: no editable book with markers'
    Check (-not (ElementByName $win 'Convert selected markers').Current.IsEnabled) 'phm: Convert disabled'
    Stop $p

    # 4. The history point is refused: nothing may change.
    $dir = NewProject 'nohistory'
    $orig = @{}
    Get-ChildItem $dir -Filter *.SFM | ForEach-Object { $orig[$_.Name] = [IO.File]::ReadAllBytes($_.FullName) }
    $env:NM_NOHISTORY = '1'
    try { $p, $win = StartHost $dir 'ALL' 'en' } finally { Remove-Item Env:NM_NOHISTORY }
    ClickByName $win 'Select all'
    Start-Sleep -Milliseconds 300
    ClickByName $win 'Convert selected markers'
    $confirm = $null
    for ($i = 0; $i -lt 20 -and -not $confirm; $i++) {
        Start-Sleep -Milliseconds 300
        $confirm = FindByName $AE::RootElement 'Convert markers' $CT::Window
        if (-not $confirm) { $confirm = FindByName $win 'Convert markers' $CT::Window }
    }
    ClickByName $confirm 'Yes'
    $stopBox = $null
    for ($i = 0; $i -lt 20 -and -not $stopBox; $i++) {
        Start-Sleep -Milliseconds 300
        $stopBox = FindByName $AE::RootElement 'Numbered Markers' $CT::Window
        if (-not $stopBox) { $stopBox = FindByName $win 'Numbered Markers' $CT::Window }
    }
    Check ($stopBox -ne $null) 'nohistory: message shown'
    if ($stopBox) { ClickByName $stopBox 'OK' }
    Start-Sleep -Milliseconds 500
    $results = ResultsText $win
    Check ($results -match 'Could not add a point to the project history, so nothing was changed') 'nohistory: results say nothing was changed'
    $changed = @(Get-ChildItem $dir -Filter *.SFM | Where-Object {
        -not [Linq.Enumerable]::SequenceEqual([byte[]]$orig[$_.Name], [byte[]][IO.File]::ReadAllBytes($_.FullName)) })
    Check ($changed.Count -eq 0) 'nohistory: no file changed'
    Check (-not (Test-Path (Join-Path $dir 'backups'))) 'nohistory: no backups made'
    Stop $p

    # 5. Books chooser: None + OK is refused; Bible books only (no XX*, CNC) is saved and scanned.
    $dir = NewProject 'books'
    $p, $win = StartHost $dir 'ALL' 'en'
    $all = (ElementByPrefix $win 'Books: ').Current.Name
    $m = [regex]::Match($all, '^Books: (\d+) of (\d+)\.\.\.$')
    Check ($m.Success -and $m.Groups[1].Value -eq $m.Groups[2].Value) ('books: all ticked at first: ' + $all)
    function OpenChooser {
        ClickByPrefix $win 'Books: '
        $d = $null
        for ($i = 0; $i -lt 20 -and -not $d; $i++) {
            Start-Sleep -Milliseconds 300
            $d = FindByName $win 'Books to examine' $CT::Window
            if (-not $d) { $d = FindByName $AE::RootElement 'Books to examine' $CT::Window }
        }
        return $d
    }
    $dlg = OpenChooser
    Check ($dlg -ne $null) 'books: chooser shown'
    ClickByName $dlg 'None'; Start-Sleep -Milliseconds 300
    ClickByName $dlg 'OK'
    $box = $null
    for ($i = 0; $i -lt 20 -and -not $box; $i++) {
        Start-Sleep -Milliseconds 300
        $box = FindByName $dlg 'Books to examine' $CT::Window
    }
    Check ($box -ne $null -and (ElementByName $box 'Tick at least one book.') -ne $null) 'books: none chosen is refused'
    if ($box) { ClickByName $box 'OK'; Start-Sleep -Milliseconds 500 }
    ClickByName $dlg 'Bible books'; Start-Sleep -Milliseconds 300
    ClickByName $dlg 'OK'
    Start-Sleep -Milliseconds 3000
    $nt = (ElementByPrefix $win 'Books: ').Current.Name
    $m2 = [regex]::Match($nt, '^Books: (\d+) of (\d+)\.\.\.$')
    Check ($m2.Success -and [int]$m2.Groups[1].Value -lt [int]$m2.Groups[2].Value -and [int]$m2.Groups[1].Value -gt 0) ('books: fewer ticked: ' + $nt)
    $cells = @(); for ($r = 0; $r -lt (RowCount $win); $r++) { $cells += CellValue $win ('Books you can convert Row ' + $r) }
    Check ($cells.Count -gt 0 -and ($cells -join ' ') -match '\bTOB\b') ('books: TOB rows still listed: ' + ($cells -join ' | '))
    Check (-not (($cells -join ' ') -match '\b(XX[A-G]|CNC)\b')) 'books: no unticked book listed'
    $settings = Join-Path $dir 'backups\settings.xml'
    Check ((Test-Path $settings) -and ((Get-Content $settings -Raw) -match '<ExcludedBooks>[^<]*\bXXD\b')) 'books: choice saved in settings.xml'
    Screenshot $win (Join-Path $tmp 'window_books.png')
    Stop $p
    $p, $win = StartHost $dir 'ALL' 'en'
    Check ((ElementByPrefix $win 'Books: ').Current.Name -eq $nt) 'books: choice kept when reopened'
    Stop $p

    # Spanish screenshot, one-form markers shown.
    $dir = NewProject 'es'
    $p, $win = StartHost $dir 'TOB,XXA,XXD:2-3' 'es'
    Check ((ElementByName $win 'Convertir los marcadores seleccionados') -ne $null) 'es: Spanish button text'
    ClickByPrefix $win 'Mostrar también'
    Check ((CellValue $win 'Tipo Row 5') -eq 'Una forma') 'es: one-form rows in Spanish'
    Screenshot $win (Join-Path $tmp 'window_es.png')
    Stop $p

    # French screenshot, one-form markers shown.
    $dir = NewProject 'fr'
    $p, $win = StartHost $dir 'TOB,XXA,XXD:2-3' 'fr'
    Check ((ElementByName $win 'Convertir les marqueurs sélectionnés') -ne $null) 'fr: French button text'
    ClickByPrefix $win 'Afficher aussi'
    Check ((CellValue $win 'Type Row 5') -eq 'Une forme') 'fr: one-form rows in French'
    Screenshot $win (Join-Path $tmp 'window_fr.png')
    Stop $p
}
finally {
    Write-Host "Files and screenshots: $tmp"
}
if ($failures) { Write-Host "$failures check(s) failed"; exit 1 }
Write-Host 'All checks passed'
exit 0
