# UI-Konzept für paperless-scan-bridge

## Zielbild

`paperless-scan-bridge` soll sich wie ein geführter, mobiler Scan-Assistent anfühlen: wenige Entscheidungen pro Schritt, große Touch-Ziele, klare Zustände und sichere Wiederaufnahme- beziehungsweise Retry-Möglichkeiten. Die Oberfläche soll nicht aus der aktuellen Implementierung abgeleitet werden, sondern aus den Anforderungen der abgeschlossenen und geplanten User Stories.

Der ideale Ablauf führt Nutzerinnen und Nutzer durch diese Schritte:

1. Scanner auswählen und prüfen.
2. Scanmodus und Einstellungen bestätigen.
3. Einseitig scannen oder bei manuellem Duplex durch beide Scanläufe geführt werden.
4. Seiten prüfen, drehen oder entfernen.
5. PDF erzeugen.
6. Paperless-ngx-Metadaten ergänzen.
7. Dokument an Paperless-ngx senden.
8. Wiederkehrende Einstellungen als Profilvorgaben verwenden.

## Leitprinzipien

### Mobile-first, aber Desktop effizient

- Primäre Zielgeräte sind Smartphones und Tablets in der Nähe des Scanners.
- Desktop-Layouts dürfen mehr Informationen nebeneinander anzeigen, behalten aber dieselbe Informationsarchitektur.
- Hauptaktionen sind groß, eindeutig beschriftet und im aktuellen Workflow-Kontext sichtbar.

### Geführter Workflow statt technischer Oberfläche

Die Startseite stellt den Scanprozess als klare Schrittfolge dar:

1. Vorbereiten
2. Scannen
3. Prüfen
4. PDF
5. Senden

Die UI beantwortet in jedem Zustand:

- Wo bin ich gerade?
- Was muss ich als Nächstes tun?
- Muss ich am Scanner handeln?
- Kann ich abbrechen, wiederholen oder fortsetzen?

### Trennung von Nutzung und Administration

Die Anwendung erhält vier Hauptbereiche:

| Bereich | Zweck |
| --- | --- |
| Scan | Täglicher Dokumentenworkflow |
| Dokumente | Aktive oder zuletzt erzeugte temporäre Scan-Sessions, sofern verfügbar |
| Einstellungen | Profile, Scanner, Paperless, Benachrichtigungen |
| Status | Health, Diagnose, Version und Betriebsinformationen |

Technische Details wie Scanner-Capabilities, mDNS, Health-Checks, Compose-Variablen oder Log-Hinweise gehören nicht in den Hauptworkflow, sondern in Status- und Diagnosebereiche.

### Sichere Zustände sichtbar machen

Die UI macht Sicherheitsentscheidungen explizit:

- Keine doppelten Scans während aktiver Jobs.
- Kein automatischer zweiter Duplex-Lauf ohne Nutzerbestätigung.
- Keine doppelten Uploads nach erfolgreicher Annahme.
- Keine Anzeige gespeicherter API-Tokens nach dem Speichern.
- Klare Profil- und Browser-Circuit-Isolation.
- Kontrollierte Retry-Möglichkeiten bei Scan-, PDF- und Upload-Fehlern.

## Informationsarchitektur

### Mobile Navigation

Eine Bottom-Navigation mit wenigen Einträgen:

| Eintrag | Beschreibung |
| --- | --- |
| Scan | Hauptworkflow und nächster Arbeitsschritt |
| Dokumente | Aktive oder wiederherstellbare Scan-Ergebnisse |
| Einstellungen | Profil-, Scanner-, Paperless- und Benachrichtigungsvorgaben |
| Status | Diagnose, Health, Version und Betriebshinweise |

Falls ein eigenständiger Dokumentbereich noch nicht umgesetzt wird, kann dieser zunächst in den Scan-Workflow integriert bleiben.

### Desktop Navigation

Auf größeren Bildschirmen eignet sich eine schmale Sidebar oder horizontale Navigation mit denselben Einträgen. Der Hauptworkflow bleibt identisch, nutzt aber Karten in zwei oder drei Spalten.

## Hauptscreen: Scan

### Grundaufbau

```text
Scan Bridge
Profil: Haushalt / Max / Anna

Vorbereiten → Scannen → Prüfen → PDF → Senden

Aktueller Zustand
Bereit zum Scannen

Dokument
[Einseitig] [Beidseitig manuell]

Scanner-Einstellungen
Quelle, Farbe, Auflösung

[Scan starten]
```

### Vorbereitungszustand

Der Startzustand zeigt:

- Gewählten Scanner.
- Scanmodus: einseitig oder manueller Duplex.
- Quelle: Flachbett oder ADF.
- Farbmodus.
- Auflösung.
- Hinweis, ob die Werte aus Profilvorgaben stammen.
- Aktion zum Aktualisieren der Scannerwerte.

Beispiel:

```text
Bereit zum Scannen

Scanner
HP Color Laser MFP 179fnw
Zuletzt geprüft: vor 3 Stunden

Dokument
○ Einseitig
● Beidseitig manuell

Einstellungen
Quelle: Automatischer Einzug
Farbe: Farbe
Auflösung: 300 dpi

[Scan starten]
[Scannerwerte aktualisieren]
```

Wenn kein Scanner ausgewählt ist, erscheint statt der Scanaktion eine prominente Karte:

```text
Noch kein Scanner ausgewählt

Suche im Netzwerk nach einem kompatiblen eSCL/AirScan-Scanner.

[Scanner einrichten]
```

## Scanner-Auswahl und Diagnose

### Screen: Scanner einrichten

```text
Scanner einrichten

[Scanner im Netzwerk suchen]

Gefundene Scanner
┌──────────────────────────────┐
│ HP Color Laser MFP 179fnw    │
│ eSCL verfügbar               │
│ HTTPS bevorzugt              │
│ [Auswählen und prüfen]       │
└──────────────────────────────┘

Kein Scanner gefunden?
- Gerät eingeschaltet?
- Gleiches Netzwerk?
- mDNS/UDP 5353 erlaubt?

[Diagnose anzeigen]
```

### Zustände

Die Scanner-UI unterscheidet:

- Suche läuft.
- Keine Geräte gefunden.
- Mehrere Anzeigen desselben physischen Geräts wurden zusammengeführt.
- Validierung läuft.
- HTTPS wurde bevorzugt validiert.
- Sicherer HTTP-Fallback nach zertifikatsspezifischem HTTPS-Fehler.
- Scanner ausgewählt.
- Scanner nicht kompatibel.
- Scanner-Capabilities konnten nicht gelesen werden.

### Fehlerton

Fehler starten mit einer verständlichen Nutzererklärung und bieten technische Details erst aufklappbar an:

```text
Scanner wurde gefunden, konnte aber nicht geprüft werden.

Mögliche Ursachen:
- Der Scanner ist gerade beschäftigt.
- Das Netzwerk blockiert die Verbindung.
- Das Gerät antwortet nicht auf eSCL.

[Erneut versuchen]
[Technische Details anzeigen]
```

## Scan-Zustände

### Wartend

```text
Scan wird vorbereitet …

Bitte lasse das Browserfenster geöffnet.
```

### Laufend

```text
Scan läuft

3 Seiten empfangen.
Der Scanner arbeitet. Das kann bei hoher Auflösung einige Minuten dauern.

[Scan abbrechen]
```

Die UI sollte keine künstlichen Prozentwerte anzeigen, wenn keine echte Fortschrittsinformation vorhanden ist. Besser sind Seitenanzahl, aktueller Zustand und klare Handlungsoptionen.

### Timeout-Entscheidung

```text
Der Scan dauert ungewöhnlich lange

Der Scannerprozess läuft möglicherweise noch.
Was möchtest du tun?

[Weiter warten]
[Scan jetzt abbrechen]
```

### Abgebrochen

```text
Scan abgebrochen

Teildaten wurden entfernt.
Du kannst einen neuen Scan starten.

[Neuen Scan starten]
```

### Fehler

```text
Scan fehlgeschlagen

Der Scanner konnte keine Seite liefern.
Deine bisherigen Dokumente wurden nicht verändert.

[Erneut versuchen]
[Scanner prüfen]
[Details anzeigen]
```

## Manueller Duplex-Workflow

Der manuelle Duplex-Workflow benötigt eine eigene, sehr klare Nutzerführung, weil eine physische Handlung am Papierstapel erforderlich ist.

### Schritt 1: Vorderseiten scannen

```text
Manueller Duplex-Scan

Schritt 1 von 3
Vorderseiten scannen

Lege den Stapel mit der Vorderseite nach oben in den Einzug.

[Vorderseiten scannen]
```

### Schritt 2: Stapel wenden

```text
Vorderseiten fertig

Jetzt Stapel wenden

1. Nimm den gesamten Stapel aus dem Ausgabefach.
2. Drehe den Stapel wie angezeigt.
3. Lege ihn wieder in den Einzug.
4. Ändere die Reihenfolge der Seiten nicht.

[Illustration: Stapel wenden]

☐ Die allerletzte Rückseite des Dokuments ist leer

[Stapel liegt richtig – Rückseiten scannen]
[Abbrechen]
```

Eine schematische Illustration sollte Einzugsrichtung, Stapelrotation und Vorder-/Rückseitenposition zeigen. Die Anleitung sollte nicht generisch formuliert sein, sondern zur validierten Feeder-Orientierung passen.

### Schritt 3: Rückseiten scannen und zusammenführen

```text
Rückseiten werden gescannt …

Danach ordnen wir die Seiten automatisch:
1, 2, 3, 4, …
```

### Seitenanzahl passt nicht

```text
Die Seitenanzahl passt nicht zusammen

Vorderseiten: 5
Rückseiten: 3

Die App kann die Reihenfolge nicht sicher bestimmen.
Bitte prüfe den Papierstapel und starte den Duplex-Scan erneut.

[Neu starten]
[Scan abbrechen]
```

## Vorschau und Bearbeitung

### Ziel

Die Vorschau dient der schnellen Korrektur offensichtlicher Probleme:

- Falsche Ausrichtung.
- Leere oder falsche Seite.
- Fehlgeschlagener Scan.
- Plausible Reihenfolge.

### Mobile Layout

```text
Seiten prüfen

6 Seiten gescannt

┌─────────────┐
│ Seite 1     │
│ [Thumbnail] │
│ [Drehen]    │
│ [Entfernen] │
└─────────────┘

┌─────────────┐
│ Seite 2     │
│ [Thumbnail] │
│ [Drehen]    │
│ [Entfernen] │
└─────────────┘

[PDF erstellen]
```

### Desktop Layout

Auf Desktop-Breite werden die Seiten als Grid mit zwei bis vier Spalten dargestellt:

```text
[Seite 1] [Seite 2] [Seite 3]
[Seite 4] [Seite 5] [Seite 6]
```

### Aktionen pro Seite

- **Drehen** führt eine 90-Grad-Drehung im Uhrzeigersinn aus.
- **Entfernen** ist zweistufig und benötigt eine Inline-Bestätigung.

```text
Seite 3 entfernen?

[Ja, entfernen]
[Abbrechen]
```

Nach dem Entfernen werden die sichtbaren Seiten sofort neu nummeriert. Historische Seitennummern sollten im Hauptlabel nicht weitergeführt werden.

### Fehlerhafte Seite

```text
Seite 4 kann nicht angezeigt werden

Die Originaldatei ist beschädigt oder fehlt.
PDF-Erstellung ist blockiert, bis die Seite entfernt oder neu gescannt wurde.

[Seite entfernen]
```

## PDF-Erstellung

### Bereit für PDF

```text
Bereit für PDF

6 Seiten
300 dpi
Rotationen und entfernte Seiten werden berücksichtigt.

[PDF erstellen]
```

### Erstellung läuft

```text
PDF wird erstellt …

Bitte warte einen Moment.
```

### Erfolg

```text
PDF ist fertig

6 Seiten wurden erfolgreich erstellt.

[PDF herunterladen]
[An Paperless senden]
[Zurück zur Vorschau]
```

### Fehler

```text
PDF konnte nicht erstellt werden

Eine oder mehrere Seiten sind nicht verfügbar.
Deine gescannten Seiten bleiben erhalten.

[Zur Vorschau]
[Erneut versuchen]
```

## Paperless-Upload

Der Upload-Screen wird nach erfolgreicher PDF-Erstellung angeboten.

### Verbindung und Metadaten

```text
An Paperless senden

Verbindung
Paperless: https://paperless.example.test
Status: Noch nicht geprüft

[Verbindung prüfen und Metadaten laden]

Metadaten
Titel
[________________________]

Korrespondent
[Auswählen]

Dokumenttyp
[Auswählen]

Tags
[+ Tag auswählen]

[An Paperless senden]
```

### Verbindung erfolgreich

```text
Verbindung erfolgreich

Metadaten wurden geladen.
```

### Authentifizierungsfehler

```text
Paperless lehnt den Zugriff ab

Der API-Token ist ungültig oder abgelaufen.
Bitte prüfe die Paperless-Einstellungen.

[Zu den Einstellungen]
[Erneut prüfen]
```

### Berechtigungsfehler

```text
Berechtigung fehlt

Der Token ist gültig, darf aber keine Dokumente hochladen oder Metadaten lesen.

[Details anzeigen]
```

### Upload läuft

```text
Upload läuft …

Bitte nicht erneut senden.
```

Der Button zum Senden ist deaktiviert, solange der Upload läuft oder bereits erfolgreich angenommen wurde.

### Erfolg

```text
Dokument wurde an Paperless übergeben

Paperless-Auftrags-ID: 12345

Paperless verarbeitet OCR und Ablage im Hintergrund.

[Neuen Scan starten]
[PDF herunterladen]
```

### Retry nach Fehler

```text
Upload fehlgeschlagen

Das PDF bleibt erhalten.
Du kannst den Upload erneut versuchen oder das PDF manuell herunterladen.

[Erneut senden]
[PDF herunterladen]
```

## Einstellungen

### Übersicht

```text
Einstellungen

Profil
- Aktives Profil
- Anmelde- und Profilmodus
- Abmelden

Scanner-Vorgaben
- Standardscanner
- Quelle
- Farbmodus
- Auflösung

Paperless
- Verbindungsquelle
- URL
- API-Token
- Metadatenvorgaben

Benachrichtigungen
- Aktivieren oder deaktivieren
- Status des Browsers

Erweitert
- Diagnose
- Version
- Health
```

### Scanner-Vorgaben

```text
Scanner-Vorgaben

Standardscanner
[HP Color Laser MFP 179fnw]

Quelle
[Automatischer Einzug]

Farbmodus
[Farbe]

Auflösung
[300 dpi]

[Speichern]
[Auf Werkseinstellungen zurücksetzen]
```

Vor dem Speichern validiert die UI die Auswahl gegen die gespeicherten oder frisch geladenen Scannerfähigkeiten.

### Paperless-Vorgaben

```text
Paperless-Vorgaben

[Paperless-Verbindung prüfen und Auswahl laden]

Standardtitel
[________________]

Korrespondent
[Auswählen]

Dokumenttyp
[Auswählen]

Tags
[Tag auswählen]

[Speichern]
```

Entfernte oder nicht mehr unterstützte Paperless-Werte werden markiert und müssen korrigiert werden, bevor sie erneut gespeichert werden.

## Profil- und Auth-Konzept

Die geplanten Profil-Stories sollten bereits in der Informationsarchitektur berücksichtigt werden.

### Profilmodus wählen

```text
Profilmodus wählen

○ Einzelhaushalt ohne Anmeldung
Alle Besucher verwenden dasselbe lokale Profil.
Einfacher Betrieb, aber keine Trennung zwischen Personen.

○ Anmeldung mit OpenID Connect
Jede Person meldet sich an und bekommt ein eigenes Profil.
Empfohlen für geteilte Haushalte mit getrennten Paperless-Konten.

[Speichern]
```

### Anonymer Modus

Im Header erscheint:

```text
Profil: Gemeinsames Haushaltsprofil
```

In den Einstellungen wird der Trade-off erklärt:

```text
Dieses Profil wird von allen Personen geteilt, die diese Anwendung öffnen können.
```

### Authentifizierter Modus

Angemeldet:

```text
Profil: Max
[Abmelden]
```

Nicht angemeldet:

```text
Anmeldung erforderlich

Bitte melde dich an, um zu scannen, Dokumente zu sehen oder Einstellungen zu ändern.

[Mit Google anmelden]
```

Öffentliche Bereiche bleiben Health, Sign-in, Sign-out-Callbacks und Fehlerseiten. Scan, Vorschau, PDF, Upload, Einstellungen und Dokumentzugriffe sind geschützt.

### Migration vorhandener lokaler Vorgaben

```text
Vorhandene lokale Einstellungen gefunden

Diese Installation enthält bereits Scanner- und Paperless-Vorgaben.
Wie sollen sie verwendet werden?

○ In gemeinsames anonymes Profil übernehmen
○ Meinem angemeldeten Profil zuweisen
○ Zurücksetzen und neu beginnen

[Auswahl übernehmen]
```

## Paperless-Konfiguration pro Profil

### Profilgespeicherte Verbindung

```text
Paperless-Verbindung

Aktive Quelle:
Profil-Einstellungen

URL
https://paperless.max.example

API-Token
••••••••••••••••

[Token ersetzen]
[Token löschen]
[Verbindung prüfen]
```

### Deployment-Fallback

```text
Paperless-Verbindung

Aktive Quelle:
Deployment-Konfiguration

Die URL und der Token wurden vom Administrator über die Umgebung konfiguriert.
Sie werden nicht in deinem Profil gespeichert.

[Verbindung prüfen]
```

### Token ersetzen

```text
API-Token ersetzen

Neuer Token
[________________]

Nach dem Speichern kann der Token nicht mehr angezeigt werden.
Du kannst ihn später nur ersetzen oder löschen.

[Speichern und Verbindung prüfen]
```

## Benachrichtigungen

### Einstieg im Scan-Screen

```text
Benachrichtigungen

Lass dich informieren, wenn du den Papierstapel wenden musst oder ein Scan abgeschlossen ist.

[Benachrichtigungen aktivieren]
```

### Zustände

Nicht aktiviert:

```text
Benachrichtigungen sind aus.
Du wirst nur in diesem geöffneten Tab informiert.
```

Aktiviert:

```text
Benachrichtigungen sind für diesen Tab aktiv.
```

Vom Browser abgelehnt:

```text
Benachrichtigungen wurden vom Browser blockiert.
Du kannst die Berechtigung in den Website-Einstellungen ändern.
```

Nicht unterstützt:

```text
Dieser Browser unterstützt keine Benachrichtigungen für diese Seite.
Nutze HTTPS oder localhost.
```

Die UI kommuniziert die technische Begrenzung klar:

```text
Hinweis: Die Scan-Seite muss geöffnet bleiben.
```

## Status- und Diagnosebereich

### Inhalte

```text
Status

Anwendung
- Version / Commit
- Health: OK
- SQLite: OK
- Temporärer Speicher: OK
- Data-Protection-Keys: OK

Scanner
- Ausgewählter Scanner
- Letzte Prüfung
- Unterstützte Quellen
- Unterstützte Auflösungen
- [Scanner neu suchen]

Paperless
- Konfiguration vorhanden
- Letzte erfolgreiche Verbindung
- [Verbindung prüfen]

Deployment
- Host-Networking-Hinweis
- Compose- und Umgebungsvariablen-Hilfe
```

### Diagnose-IDs

Fehler erhalten eine kopierbare Diagnose-ID:

```text
Fehler-ID: scan-session-abc123
Diese ID kann in den Container-Logs gesucht werden.
```

Die UI zeigt keine Tokens, keine Dokumentinhalte, keine privaten Metadaten und keine unnötigen Dateinamen.

## Visuelles Design

### Stilrichtung

- Ruhig, sachlich und produktiv.
- Bootstrap-kompatibel.
- Keine zusätzliche komplexe Design-System-Abhängigkeit.
- Große Karten und klare Primäraktionen.
- Dezente Statusfarben.

### Farbsemantik

| Zustand | Farbe |
| --- | --- |
| Bereit / Erfolg | Grün |
| Aktion läuft | Blau |
| Nutzerentscheidung nötig | Gelb / Amber |
| Fehler / blockiert | Rot |
| Technische Info | Grau |

### Komponenten

Empfohlene wiederverwendbare UI-Bausteine:

- `WorkflowStepper`
- `StatusCard`
- `ScannerCard`
- `ScanSettingsSummary`
- `DuplexFlipInstruction`
- `PagePreviewCard`
- `ConfirmAction`
- `PdfResultCard`
- `PaperlessMetadataForm`
- `ProfileModeBanner`
- `DiagnosticsPanel`

## Barrierefreiheit und Bedienbarkeit

Empfehlungen:

- Buttons mindestens 44 × 44 px.
- Statusmeldungen über ARIA-Live-Regionen.
- Zustände nicht ausschließlich über Farbe kommunizieren.
- Formulare mit sichtbaren Labels.
- Fehlermeldungen direkt am Feld und zusätzlich zusammenfassend anzeigen.
- Fokus nach Schrittwechsel auf Überschrift oder Statusmeldung setzen.
- Kritische Bestätigungen inline statt ausschließlich in Modals anbieten.

Besonders wichtig sind:

- Duplex-Wendeanleitung.
- Timeout-Entscheidung.
- Seite entfernen.
- Upload erneut senden.
- Token ersetzen oder löschen.
- Profilmodus wechseln.

## Beispielhafter Gesamtablauf

### 1. Start

```text
Scan Bridge

Bereit zum Scannen

Profil: Gemeinsames Haushaltsprofil
Scanner: HP Color Laser MFP 179fnw

Was möchtest du scannen?
[Einseitiges Dokument]
[Beidseitiges Dokument]

Einstellungen
Quelle: ADF
Farbe: Farbe
Auflösung: 300 dpi

[Scan starten]
```

### 2. Scan läuft

```text
Scan läuft

2 Seiten empfangen.
Bitte warte, bis der Scanner fertig ist.

[Scan abbrechen]
```

### 3. Vorschau

```text
6 Seiten gescannt

Bitte prüfe die Reihenfolge und Ausrichtung.

[Seitenkarten]

[PDF erstellen]
```

### 4. PDF fertig

```text
PDF ist fertig

[PDF herunterladen]
[An Paperless senden]
```

### 5. Upload

```text
An Paperless senden

Titel
[Rechnung Strom August]

Korrespondent
[Stadtwerke]

Dokumenttyp
[Rechnung]

Tags
[Strom] [Haushalt]

[An Paperless senden]
```

### 6. Abschluss

```text
Dokument wurde übergeben

Paperless-Auftrags-ID: 12345

[Neuen Scan starten]
```

## Empfohlene nächste UX-Artefakte

1. Low-fidelity-Wireframes für Mobile und Desktop.
2. UI-State-Matrix für Scanner-, Scan-, Duplex-, PDF- und Upload-Zustände.
3. Deutsches Microcopy-Konzept für Fehler, Warnungen und Entscheidungen.
4. Komponentenliste mit Verantwortlichkeiten und Zustandsmodellen.
5. Klickbarer Prototyp, unabhängig von der aktuellen UI.

## Offene Produktfragen

1. Soll die UI dauerhaft ausschließlich Deutsch sein oder perspektivisch mehrsprachig werden?
2. Soll es einen eigenständigen Bereich „Dokumente“ für wiederherstellbare temporäre Sessions geben?
3. Welche visuelle Richtung ist bevorzugt: neutraler Bootstrap-Stil, Appliance-artige Haushaltsgeräte-UI oder Admin-Dashboard?
4. Soll die Duplex-Wendeanleitung als statische Grafik, kleine Animation oder hardwarebezogener Hilfedialog umgesetzt werden?
