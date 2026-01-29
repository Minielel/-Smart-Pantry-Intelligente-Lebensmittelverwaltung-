# Team Kakapo
# Smart Food Manager (WPF)

Eine desktopbasierte Multiview-Anwendung zur Verwaltung von Lebensmitteln, Rezepten, Einkaufslisten und Wochenplänen mit Fokus auf Haltbarkeitsmanagement und Reduzierung von Lebensmittelverschwendung.

---

## Projektübersicht

Der Smart Food Manager unterstützt Benutzer dabei, ihren Lebensmittelbestand effizient zu verwalten und Einkaufs- sowie Essensplanung intelligent zu organisieren. Die Anwendung kombiniert Lagerverwaltung, Rezeptmanagement und Wochenplanung in einem zentralen System.

Die Anwendung basiert auf einer MySQL-Datenbank und verwendet Entity Framework Core als ORM für den Datenzugriff.

---

## Hauptfunktionen

### Authentifizierung und Benutzerverwaltung

- Benutzerregistrierung und Login  
- Passwort-Hashing zur sicheren Speicherung  
- Rollenverwaltung (Administrator und Standardbenutzer)  
- Passwort-Zurücksetzung per E-Mail-Code (geplant)  

---

### Dashboard

Zentrale Übersicht mit zusammengefassten Informationen:

- Anzahl vorhandener Lebensmittel  
- Anzahl bald ablaufender Produkte  
- Einkaufsbedarf  
- Anzahl gespeicherter Rezepte  
- Hinweise zur Lebensmittelrettung  

Das Dashboard zeigt ausschließlich aggregierte Daten, keine Einzelprodukte.

---

### Lebensmittelverwaltung (Food View)

- Kachel- bzw. Bubble-Darstellung aller Lebensmittel  
- Automatische Sortierung:
  - Abgelaufene Produkte (oben, rot markiert)  
  - Bald ablaufende Produkte  
  - Normale Produkte  
- Hover-Effekte und visuelles Feedback  
- Detailansicht durch Klick  
- Vollständige CRUD-Funktionalität:
  - Hinzufügen  
  - Bearbeiten  
  - Löschen  

---

### Rezeptverwaltung

- Übersicht in Karten-/Bubble-Darstellung  
- Anzeige von:
  - Zutatenanzahl  
  - Relevanten Ablaufstatus-Informationen  
- Detailansicht für:
  - Zutatenverwaltung  
  - Mengenanpassung  
  - Bearbeitung der Anleitung  
- Möglichkeit zum Löschen von Rezepten  

---

### Wochenplan (Meal Planner)

- Automatische Rezeptvorschläge basierend auf bald ablaufenden Lebensmitteln  
- Drag-and-Drop-Unterstützung  
- Planung nach Wochentagen und Mahlzeiten:
  - Frühstück  
  - Mittagessen  
  - Abendessen  
- Haltbarkeitsprüfung bei der Planung  
- Direkte Verknüpfung mit Rezeptdaten  

---

### Einkaufsliste

- Automatische Erstellung bei fehlenden Zutaten  
- Manuelles Hinzufügen von Produkten möglich  
- Abhaken gekaufter Artikel  
- Automatische Übertragung in den Lagerbestand nach Abschluss des Einkaufs  

---

### Einstellungen

- Anpassung des Farbschemas  
- Sprachwahl  
- Änderung von Benutzername und Passwort  
- Logout-Funktion  

---

## Architektur

Die Anwendung verwendet das MVVM-Architekturmuster (Model-View-ViewModel).

Aufgabenverteilung:

- Model: Datenstrukturen und Datenbankabbildung  
- View: Benutzeroberfläche (XAML)  
- ViewModel: Logik, Zustandsverwaltung und Commands  
- Services: Geschäftslogik und Datenbankzugriffe  
- Data: Datenbankkonfiguration und DbContext  

Vorteile dieser Architektur:

- Klare Trennung von Zuständigkeiten  
- Bessere Wartbarkeit  
- Gute Testbarkeit  
- Skalierbarkeit  

---

## Datenbank

### Backend

- MySQL oder MariaDB  
- Verwaltung über phpMyAdmin  
- Zugriff über Entity Framework Core  

---

### Zentrale Tabellen

- users  
- user_settings  
- food_items  
- categories  
- recipes  
- recipe_ingredients  
- meal_plan  
- shopping_list  

Die Tabellen sind relational verknüpft und unterstützen Mehrbenutzerbetrieb.

---

## Technologie-Stack

### Frontend

- WPF (.NET)  
- XAML  
- MVVM Pattern  

---

### Backend

- C#  
- Entity Framework Core  
- Pomelo MySQL Provider  

---

### Datenbank

- MySQL  
- phpMyAdmin  

---

## Installation und Setup

### Voraussetzungen

- .NET SDK (Version 6 oder höher empfohlen)  
- MySQL Server  
- phpMyAdmin (optional, empfohlen)  
- Visual Studio oder Visual Studio Code  

---

### Datenbank einrichten

1. SQL-Setup-Skript ausführen  
2. Datenbank erstellen  
3. Tabellen automatisch generieren  

---

### Verbindung konfigurieren

Connection String im Projekt setzen:

