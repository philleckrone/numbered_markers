using System;

namespace NumberedMarkers
{
    /// <summary>
    /// The window's text in English, Spanish or French, picked from Paratext's interface language.
    /// Keep this file ASCII: run tools\asciify_cs.py after typing accented letters.
    /// </summary>
    public sealed class Strings
    {
        /// <summary>The tool's version: shown in the window title and reported to Paratext.</summary>
        public const string ToolVersion = "1.7";

        public string MenuText;
        public string Title;
        public string Intro;
        public string ProjectLabel;
        public string Scanning;
        public string MixedHeader;
        public string MixedNone;
        public string ColConvert;
        public string ColMarker;
        public string ColBare;
        public string ColLevels;
        public string ColOther;
        public string ColCan;
        public string ColCannot;
        public string ColWhere;
        public string ConsistentHeader;
        public string ConsistentNone;
        public string OtherNote;
        public string IntroLabel;
        public string More;
        public string NoPermissionProject;
        public string NoPermissionBooks;
        public string SomeNotEditable;
        public string SelectAll;
        public string Clear;
        public string ConvertButton;
        public string Close;
        public string NothingSelected;
        public string ConfirmTitle;
        public string Confirm;
        public string HistoryMessage;
        public string ResultsHeader;
        public string ResultsLabel;
        public string ResChange;
        public string ResTotal;
        public string ResNothing;
        public string ResNoPermission;
        public string ResBusy;
        public string ResFailed;
        public string ResBackups;
        public string ResHistoryFailed;
        public string NotSaved;
        public string OpenProjectFirst;
        public string Error;
        public string Description;
        public string HistoryNotRecorded;
        public string HistoryNoFolder;
        public string HistoryStopped;
        public string KindOneForm;
        public string KindMixed;
        public string ColKind;
        public string ShowOneForm;
        public string IntroRemove;
        public string ResChangeRemove;
        public string DirRemove;
        public string DirAdd;
        public string ColChangeTo;
        public string BooksButton;
        public string BooksTitle;
        public string BooksIntro;
        public string BooksAll;
        public string BooksNone;
        public string BooksBible;
        public string BooksOT;
        public string BooksNT;
        public string BooksDC;
        public string BooksOther;
        public string BooksNoneChosen;
        public string Ok;
        public string Cancel;
        public string SettingsNotSaved;

        public static Strings For(string locale)
        {
            string lang = string.IsNullOrEmpty(locale) ? "" : locale.ToLowerInvariant();
            if (lang.StartsWith("es", StringComparison.Ordinal)) return Spanish();
            if (lang.StartsWith("fr", StringComparison.Ordinal)) return French();
            return English();
        }

        public static Strings English()
        {
            var s = new Strings();
            s.MenuText = "Numbered Markers...";
            s.Title = "Numbered Markers";
            s.Intro = "Markers like \\q and \\q1 mean the same thing. A project that uses only one form of a marker is fine. " +
                "When a project uses the unnumbered form AND a numbered form (e.g. \\mt beside \\mt1 or \\mt2), " +
                "the unnumbered form should be changed to the \"1\" form.";
            s.ProjectLabel = "Project: {0}";
            s.Scanning = "Scanning {0}...";
            s.MixedHeader = "Mixed: unnumbered and numbered forms both used";
            s.MixedNone = "No marker uses both its unnumbered and its numbered forms. Nothing needs to change.";
            s.ColConvert = "Convert";
            s.ColMarker = "Marker";
            s.ColBare = "Unnumbered";
            s.ColLevels = "Numbered forms";
            s.ColOther = "Not in usfm.sty";
            s.ColCan = "Books you can convert";
            s.ColCannot = "Books you cannot edit";
            s.ColWhere = "Where it will change";
            s.ConsistentHeader = "Consistent, only one form used (no change needed): {0}";
            s.ConsistentNone = "none";
            s.OtherNote = "Levels under \"Not in usfm.sty\" (for example \\s5 used as a chunk marker) are listed, but they do not make a marker count as mixed.";
            s.IntroLabel = "intro";
            s.More = "... and {0} more";
            s.NoPermissionProject = "You do not have permission to edit the {0} project, so these markers cannot be converted.";
            s.NoPermissionBooks = "You cannot edit any of the books that have these markers, so they cannot be converted.";
            s.SomeNotEditable = "Some of these markers are in books you cannot edit. Those books will be left unchanged and listed.";
            s.SelectAll = "Select all";
            s.Clear = "Clear";
            s.ConvertButton = "Convert selected markers";
            s.Close = "Close";
            s.NothingSelected = "Tick at least one marker to convert.";
            s.ConfirmTitle = "Convert markers";
            s.Confirm = "Change {0} to the \"1\" form in {1} book(s) you can edit?\n\n" +
                "A point is first added to the project history, and each changed book is saved as a .bak file in:\n{2}";
            s.HistoryMessage = "Numbered Markers: before converting {0}";
            s.ResultsHeader = "Results:";
            s.ResultsLabel = "Results: after you click Convert, the changes made (and any books or chapters not changed) are listed here.";
            s.ResChange = "{0}: \\{1} -> \\{1}1, {2} change(s)";
            s.ResTotal = "Converted: {0} change(s) in {1} book(s).";
            s.ResNothing = "Nothing was converted.";
            s.ResNoPermission = "Not changed, because you do not have permission to edit: {0}";
            s.ResBusy = "Not changed, because the text is in use (finish editing it, then try again): {0}";
            s.ResFailed = "Could not change: {0}";
            s.ResBackups = "Backups of the changed books are in: {0}";
            s.ResHistoryFailed = "Could not add a point to the project history: {0}";
            s.NotSaved = "Paratext did not save the change";
            s.OpenProjectFirst = "Open a project in a Scripture window first, then choose Numbered Markers...";
            s.Error = "Numbered Markers could not finish:\n{0}";
            s.Description = "Finds markers used in both their unnumbered and numbered forms (\\mt beside \\mt1, \\q beside \\q1) and changes the unnumbered form to the \"1\" form in the books you may edit.";
            s.ColChangeTo = "Change to";
            s.DirAdd = "\\{0} \u2192 \\{0}1";
            s.DirRemove = "\\{0}1 \u2192 \\{0}";
            s.ResChangeRemove = "{0}: \\{1}1 -> \\{1}, {2} change(s)";
            s.IntroRemove = "When a marker uses no level 2 or higher (no \\io2), you can instead change its \"1\" form to the unnumbered form (\\io1 \u2192 \\io) in the \"Change to\" column; the direction with fewer changes is chosen for you.";
            s.ShowOneForm = "Also show markers that use only one form (for example only \\s), to change them too";
            s.ColKind = "Kind";
            s.KindMixed = "Mixed";
            s.KindOneForm = "One form";
            s.HistoryStopped = "Could not add a point to the project history, so nothing was changed: {0}\n\nFinish or close any editing in this project, then try again.";
            s.HistoryNoFolder = "the project folder or its history (.hg) was not found";
            s.HistoryNotRecorded = "Paratext did not record the history point (the project may be in use)";
            s.BooksButton = "Books: {0} of {1}...";
            s.BooksTitle = "Books to examine";
            s.BooksIntro = "Tick the books to examine. Unticked books are not read at all, so they are neither counted nor converted. The choice is saved for this project.";
            s.BooksAll = "All";
            s.BooksNone = "None";
            s.BooksBible = "Bible books";
            s.BooksOT = "Old Testament";
            s.BooksNT = "New Testament";
            s.BooksDC = "Deuterocanon";
            s.BooksOther = "Other books";
            s.BooksNoneChosen = "Tick at least one book.";
            s.Ok = "OK";
            s.Cancel = "Cancel";
            s.SettingsNotSaved = "The choice of books could not be saved: {0}";
            return s;
        }

        public static Strings Spanish()
        {
            var s = new Strings();
            s.MenuText = "Marcadores numerados...";
            s.Title = "Marcadores numerados";
            s.Intro = "Marcadores como \\q y \\q1 significan lo mismo. Un proyecto que usa una sola forma de un marcador est\u00E1 bien. " +
                "Cuando un proyecto usa la forma sin n\u00FAmero Y una forma numerada (p. ej. \\mt junto a \\mt1 o \\mt2), " +
                "la forma sin n\u00FAmero debe cambiarse a la forma \"1\".";
            s.ProjectLabel = "Proyecto: {0}";
            s.Scanning = "Revisando {0}...";
            s.MixedHeader = "Mezclados: se usan la forma sin n\u00FAmero y las numeradas";
            s.MixedNone = "Ning\u00FAn marcador usa a la vez su forma sin n\u00FAmero y sus formas numeradas. No hay nada que cambiar.";
            s.ColConvert = "Convertir";
            s.ColMarker = "Marcador";
            s.ColBare = "Sin n\u00FAmero";
            s.ColLevels = "Formas numeradas";
            s.ColOther = "No est\u00E1n en usfm.sty";
            s.ColCan = "Libros que puede convertir";
            s.ColCannot = "Libros que no puede editar";
            s.ColWhere = "D\u00F3nde cambiar\u00E1";
            s.ConsistentHeader = "Coherentes, se usa una sola forma (no hace falta cambiar nada): {0}";
            s.ConsistentNone = "ninguno";
            s.OtherNote = "Los niveles de \"No est\u00E1n en usfm.sty\" (por ejemplo \\s5 usado como marca de bloque) se muestran, pero no hacen que un marcador cuente como mezclado.";
            s.IntroLabel = "introducci\u00F3n";
            s.More = "... y {0} m\u00E1s";
            s.NoPermissionProject = "Usted no tiene permiso para editar el proyecto {0}, as\u00ED que estos marcadores no se pueden convertir.";
            s.NoPermissionBooks = "Usted no puede editar ninguno de los libros que tienen estos marcadores, as\u00ED que no se pueden convertir.";
            s.SomeNotEditable = "Algunos de estos marcadores est\u00E1n en libros que usted no puede editar. Esos libros no se cambiar\u00E1n y se mostrar\u00E1n en la lista.";
            s.SelectAll = "Seleccionar todo";
            s.Clear = "Borrar";
            s.ConvertButton = "Convertir los marcadores seleccionados";
            s.Close = "Cerrar";
            s.NothingSelected = "Marque al menos un marcador para convertir.";
            s.ConfirmTitle = "Convertir marcadores";
            s.Confirm = "\u00BFCambiar {0} a la forma \"1\" en {1} libro(s) que usted puede editar?\n\n" +
                "Primero se agrega un punto al historial del proyecto, y cada libro cambiado se guarda como archivo .bak en:\n{2}";
            s.HistoryMessage = "Marcadores numerados: antes de convertir {0}";
            s.ResultsHeader = "Resultados:";
            s.ResultsLabel = "Resultados: despu\u00E9s de hacer clic en Convertir, aqu\u00ED se muestran los cambios hechos (y los libros o cap\u00EDtulos que no se cambiaron).";
            s.ResChange = "{0}: \\{1} -> \\{1}1, {2} cambio(s)";
            s.ResTotal = "Convertido: {0} cambio(s) en {1} libro(s).";
            s.ResNothing = "No se convirti\u00F3 nada.";
            s.ResNoPermission = "No se cambiaron, porque usted no tiene permiso para editar: {0}";
            s.ResBusy = "No se cambiaron, porque el texto est\u00E1 en uso (termine de editarlo y vuelva a intentarlo): {0}";
            s.ResFailed = "No se pudo cambiar: {0}";
            s.ResBackups = "Las copias de seguridad de los libros cambiados est\u00E1n en: {0}";
            s.ResHistoryFailed = "No se pudo agregar un punto al historial del proyecto: {0}";
            s.NotSaved = "Paratext no guard\u00F3 el cambio";
            s.OpenProjectFirst = "Abra primero un proyecto en una ventana de Escritura y luego elija Marcadores numerados...";
            s.Error = "Marcadores numerados no pudo terminar:\n{0}";
            s.Description = "Encuentra marcadores que se usan en su forma sin n\u00FAmero y en sus formas numeradas (\\mt junto a \\mt1, \\q junto a \\q1) y cambia la forma sin n\u00FAmero a la forma \"1\" en los libros que usted puede editar.";
            s.ColChangeTo = "Cambiar a";
            s.DirAdd = "\\{0} \u2192 \\{0}1";
            s.DirRemove = "\\{0}1 \u2192 \\{0}";
            s.ResChangeRemove = "{0}: \\{1}1 -> \\{1}, {2} cambio(s)";
            s.IntroRemove = "Cuando un marcador no usa el nivel 2 ni superiores (no hay \\io2), puede en cambio pasar su forma \"1\" a la forma sin n\u00FAmero (\\io1 \u2192 \\io) en la columna \"Cambiar a\"; se elige por usted la direcci\u00F3n con menos cambios.";
            s.ShowOneForm = "Mostrar tambi\u00E9n los marcadores que usan una sola forma (por ejemplo solo \\s), para cambiarlos tambi\u00E9n";
            s.ColKind = "Tipo";
            s.KindMixed = "Mezclado";
            s.KindOneForm = "Una forma";
            s.HistoryStopped = "No se pudo agregar un punto al historial del proyecto, as\u00ED que no se cambi\u00F3 nada: {0}\n\nTermine o cierre cualquier edici\u00F3n en este proyecto y vuelva a intentarlo.";
            s.HistoryNoFolder = "no se encontr\u00F3 la carpeta del proyecto o su historial (.hg)";
            s.HistoryNotRecorded = "Paratext no registr\u00F3 el punto del historial (puede que el proyecto est\u00E9 en uso)";
            s.BooksButton = "Libros: {0} de {1}...";
            s.BooksTitle = "Libros a revisar";
            s.BooksIntro = "Marque los libros que desea revisar. Los libros sin marcar no se leen, as\u00ED que no se cuentan ni se convierten. La selecci\u00F3n se guarda para este proyecto.";
            s.BooksAll = "Todos";
            s.BooksNone = "Ninguno";
            s.BooksBible = "Libros de la Biblia";
            s.BooksOT = "Antiguo Testamento";
            s.BooksNT = "Nuevo Testamento";
            s.BooksDC = "Deuterocan\u00F3nicos";
            s.BooksOther = "Otros libros";
            s.BooksNoneChosen = "Marque al menos un libro.";
            s.Ok = "Aceptar";
            s.Cancel = "Cancelar";
            s.SettingsNotSaved = "No se pudo guardar la selecci\u00F3n de libros: {0}";
            return s;
        }

        public static Strings French()
        {
            var s = new Strings();
            s.MenuText = "Marqueurs num\u00E9rot\u00E9s...";
            s.Title = "Marqueurs num\u00E9rot\u00E9s";
            s.Intro = "Des marqueurs comme \\q et \\q1 veulent dire la m\u00EAme chose. Un projet qui n'utilise qu'une seule forme d'un marqueur ne pose pas de probl\u00E8me. " +
                "Quand un projet utilise la forme sans num\u00E9ro ET une forme num\u00E9rot\u00E9e (p. ex. \\mt \u00E0 c\u00F4t\u00E9 de \\mt1 ou \\mt2), " +
                "la forme sans num\u00E9ro devrait \u00EAtre chang\u00E9e en forme \u00AB 1 \u00BB.";
            s.ProjectLabel = "Projet : {0}";
            s.Scanning = "Analyse de {0}...";
            s.MixedHeader = "M\u00E9lang\u00E9s : la forme sans num\u00E9ro et les formes num\u00E9rot\u00E9es sont toutes deux utilis\u00E9es";
            s.MixedNone = "Aucun marqueur n'utilise \u00E0 la fois sa forme sans num\u00E9ro et ses formes num\u00E9rot\u00E9es. Il n'y a rien \u00E0 changer.";
            s.ColConvert = "Convertir";
            s.ColMarker = "Marqueur";
            s.ColBare = "Sans num\u00E9ro";
            s.ColLevels = "Formes num\u00E9rot\u00E9es";
            s.ColOther = "Absents de usfm.sty";
            s.ColCan = "Livres que vous pouvez convertir";
            s.ColCannot = "Livres que vous ne pouvez pas modifier";
            s.ColWhere = "O\u00F9 le changement sera fait";
            s.ConsistentHeader = "Coh\u00E9rents, une seule forme utilis\u00E9e (aucun changement n\u00E9cessaire) : {0}";
            s.ConsistentNone = "aucun";
            s.OtherNote = "Les niveaux sous \u00AB Absents de usfm.sty \u00BB (par exemple \\s5 utilis\u00E9 comme marque de section de travail) sont list\u00E9s, mais ils ne font pas compter un marqueur comme m\u00E9lang\u00E9.";
            s.IntroLabel = "introduction";
            s.More = "... et {0} de plus";
            s.NoPermissionProject = "Vous n'avez pas la permission de modifier le projet {0}, donc ces marqueurs ne peuvent pas \u00EAtre convertis.";
            s.NoPermissionBooks = "Vous ne pouvez modifier aucun des livres qui contiennent ces marqueurs, donc ils ne peuvent pas \u00EAtre convertis.";
            s.SomeNotEditable = "Certains de ces marqueurs se trouvent dans des livres que vous ne pouvez pas modifier. Ces livres resteront inchang\u00E9s et seront list\u00E9s.";
            s.SelectAll = "Tout s\u00E9lectionner";
            s.Clear = "Effacer";
            s.ConvertButton = "Convertir les marqueurs s\u00E9lectionn\u00E9s";
            s.Close = "Fermer";
            s.NothingSelected = "Cochez au moins un marqueur \u00E0 convertir.";
            s.ConfirmTitle = "Convertir les marqueurs";
            s.Confirm = "Changer {0} en forme \u00AB 1 \u00BB dans {1} livre(s) que vous pouvez modifier ?\n\n" +
                "Un point est d'abord ajout\u00E9 \u00E0 l'historique du projet, et chaque livre modifi\u00E9 est enregistr\u00E9 dans un fichier .bak dans :\n{2}";
            s.HistoryMessage = "Marqueurs num\u00E9rot\u00E9s : avant de convertir {0}";
            s.ResultsHeader = "R\u00E9sultats :";
            s.ResultsLabel = "R\u00E9sultats : apr\u00E8s avoir cliqu\u00E9 sur Convertir, les changements faits (et les livres ou chapitres non modifi\u00E9s) sont list\u00E9s ici.";
            s.ResChange = "{0} : \\{1} -> \\{1}1, {2} changement(s)";
            s.ResTotal = "Converti : {0} changement(s) dans {1} livre(s).";
            s.ResNothing = "Rien n'a \u00E9t\u00E9 converti.";
            s.ResNoPermission = "Non modifi\u00E9s, car vous n'avez pas la permission de modifier : {0}";
            s.ResBusy = "Non modifi\u00E9s, car le texte est en cours d'utilisation (terminez la modification, puis r\u00E9essayez) : {0}";
            s.ResFailed = "Impossible de modifier : {0}";
            s.ResBackups = "Les sauvegardes des livres modifi\u00E9s se trouvent dans : {0}";
            s.ResHistoryFailed = "Impossible d'ajouter un point \u00E0 l'historique du projet : {0}";
            s.NotSaved = "Paratext n'a pas enregistr\u00E9 le changement";
            s.OpenProjectFirst = "Ouvrez d'abord un projet dans une fen\u00EAtre de texte, puis choisissez Marqueurs num\u00E9rot\u00E9s...";
            s.Error = "Marqueurs num\u00E9rot\u00E9s n'a pas pu terminer :\n{0}";
            s.Description = "Trouve les marqueurs utilis\u00E9s \u00E0 la fois sous leur forme sans num\u00E9ro et sous leurs formes num\u00E9rot\u00E9es (\\mt \u00E0 c\u00F4t\u00E9 de \\mt1, \\q \u00E0 c\u00F4t\u00E9 de \\q1) et change la forme sans num\u00E9ro en forme \u00AB 1 \u00BB dans les livres que vous pouvez modifier.";
            s.ColChangeTo = "Changer en";
            s.DirAdd = "\\{0} \u2192 \\{0}1";
            s.DirRemove = "\\{0}1 \u2192 \\{0}";
            s.ResChangeRemove = "{0} : \\{1}1 -> \\{1}, {2} changement(s)";
            s.IntroRemove = "Quand un marqueur n'utilise ni le niveau 2 ni un niveau sup\u00E9rieur (pas de \\io2), vous pouvez \u00E0 la place changer sa forme \u00AB 1 \u00BB en forme sans num\u00E9ro (\\io1 \u2192 \\io) dans la colonne \u00AB Changer en \u00BB ; le sens qui demande le moins de changements est choisi pour vous.";
            s.ShowOneForm = "Afficher aussi les marqueurs qui n'utilisent qu'une seule forme (par exemple seulement \\s), pour les changer aussi";
            s.ColKind = "Type";
            s.KindMixed = "M\u00E9lang\u00E9";
            s.KindOneForm = "Une forme";
            s.HistoryStopped = "Impossible d'ajouter un point \u00E0 l'historique du projet, donc rien n'a \u00E9t\u00E9 modifi\u00E9 : {0}\n\nTerminez ou fermez toute modification en cours dans ce projet, puis r\u00E9essayez.";
            s.HistoryNoFolder = "le dossier du projet ou son historique (.hg) est introuvable";
            s.HistoryNotRecorded = "Paratext n'a pas enregistr\u00E9 le point d'historique (le projet est peut-\u00EAtre en cours d'utilisation)";
            s.BooksButton = "Livres : {0} sur {1}...";
            s.BooksTitle = "Livres \u00E0 analyser";
            s.BooksIntro = "Cochez les livres \u00E0 analyser. Les livres non coch\u00E9s ne sont pas lus du tout : ils ne sont ni compt\u00E9s ni convertis. Le choix est enregistr\u00E9 pour ce projet.";
            s.BooksAll = "Tous";
            s.BooksNone = "Aucun";
            s.BooksBible = "Livres de la Bible";
            s.BooksOT = "Ancien Testament";
            s.BooksNT = "Nouveau Testament";
            s.BooksDC = "Deut\u00E9rocanoniques";
            s.BooksOther = "Autres livres";
            s.BooksNoneChosen = "Cochez au moins un livre.";
            s.Ok = "OK";
            s.Cancel = "Annuler";
            s.SettingsNotSaved = "Impossible d'enregistrer le choix des livres : {0}";
            return s;
        }
    }
}
