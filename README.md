# Milu

Milu ist ein eigenständiger ASP.NET-Core-MVC-Prototyp für eine modulare
Anwendungsstruktur mit einem dynamischen Router und klaren URL-Konventionen.

## Enthalten

- Module unter `Application/Modules`
- automatische Modulregistrierung per Dependency Injection und Reflection
- Frontend-Routen: `/{module}/{controller}/{action}`
- Admin-Routen: `/admin/{module}/{controller}/{action}`
- Schlüssel/Wert-Parameter: `/name/Ada/id/42`
- benutzerdefinierte View-Suche innerhalb des jeweiligen Moduls
- statische Moduldateien unter `/modules/{module}`
- Cookie-Authentifizierung für Admin-Controller
- globaler Anti-Forgery-Schutz für unsichere HTTP-Methoden
- eigenes Admin-Modul mit automatisch aufgebautem Modulmenü
- News- und Gästebuchmodul mit Erstellen, Bearbeiten und Löschen
- dauerhafte SQLite-Speicherung unter `src/Milu.Web/App_Data/milu.db`
- ASP.NET Core Identity mit separater Datenbank `App_Data/identity.db`
- öffentliche Registrierung mit automatischer Grundgruppe `Registered`
- dynamische Gruppenrechte je Modul
- individuelle Benutzerrechte mit `Vererben`, `Erlauben` und `Verbieten`
- Parser-, Modulkatalog- und HTTP-Integrationstests

## Projektstruktur

```text
Milu/
├── Milu.sln
├── src/
│   └── Milu.Web/
│       ├── Application/
│       │   └── Modules/
│       │       └── Sample/
│       │           ├── Controllers/
│       │           │   └── Admin/
│       │           ├── Models/
│       │           ├── Resources/
│       │           ├── Static/
│       │           └── Views/
│       │               └── Admin/
│       └── Infrastructure/
│           ├── Authentication/
│           ├── Modules/
│           └── Routing/
└── tests/
    └── Milu.Web.Tests/
```

## Starten

```powershell
dotnet run --project .\src\Milu.Web --launch-profile http
```

Danach `http://localhost:5127` öffnen.

Beispielrouten:

- `/`
- `/sample/index/index`
- `/sample/index/hello/name/Ada`
- `/news`
- `/guestbook`
- `/admin`
- `/admin/modules`
- `/account/register`
- `/admin/users`
- `/admin/groups`
- `/admin/sample/index/index`

Der lokale Demo-Login lautet:

```text
Benutzername: admin
Passwort:     milu-demo
```

Registrierte Benutzer können sich mit Benutzername, eindeutigem Anzeigenamen
oder E-Mail-Adresse anmelden. Neue Registrierungen fragen einen eigenen,
eindeutigen Benutzernamen ab. Bestehende Konten bleiben kompatibel.

Das Demopasswort steht ausschließlich in `appsettings.Development.json`.
In anderen Umgebungen ist kein Passwort vorkonfiguriert. Für eine echte
Anwendung sollte ASP.NET Core Identity oder ein externer Identity Provider
verwendet werden.

## Rechte-Modell

Jedes automatisch erkannte Modul besitzt die Rechte `ModuleView`, `ContentView`,
`ContentCreate`, `ContentEdit` und `ContentDelete`. Gruppen gewähren Rechte. Eine
individuelle Benutzerentscheidung überschreibt die Gruppen: `allow` erlaubt,
`deny` verbietet und `inherit` übernimmt die Gruppenrechte. Die virtuelle
Systemgruppe `Guest` gilt für nicht angemeldete Besucher. `Registered` enthält
die Grundrechte neuer Konten, `Administrator` besitzt Vollzugriff.

Die Benutzer- und Gruppenverwaltung ist unter `/admin/users` und
`/admin/groups` erreichbar. Änderungen werden ohne Codeänderung in
`identity.db` gespeichert.

## Tests

```powershell
dotnet test .\Milu.sln
```

## Neues Modul ergänzen

1. Ordner `Application/Modules/<Name>` anlegen.
2. Eine Klasse erstellen, die `IMiluModule` implementiert.
3. Controller mit `[Area("<AreaName>")]` ergänzen.
4. Views unter `Views/<Controller>/<Action>.cshtml` ablegen.
5. Für Admin-Controller den Klassennamen `Admin<Controller>Controller` und den
   View-Pfad `Views/Admin/<Controller>/<Action>.cshtml` verwenden.

Eine manuelle Registrierung in `Program.cs` ist nicht erforderlich. Das Modul
wird beim Start automatisch entdeckt.
