using System.ComponentModel;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Server;

namespace RSChatApp.Shared.Infrastructure.Mcp.ReportServer.Mcp;

[McpServerResourceType]
public class TerminalResource
{
    [McpServerResource, 
     Description("List terminal commands of the reportserver")]
    public string ListTerminalCommands()
    {
        return TerminalCommands;
    }
    
    private const string TerminalCommands = @"
        Dateisystem & Navigation
        cd - Verzeichnis wechseln (z.B. cd fileserver/bin)
        mkdir - Verzeichnis erstellen (z.B. mkdir tmp)
        ls - Verzeichnisinhalt anzeigen
        pwd - aktuelles Verzeichnis anzeigen
        rm - Dateien/Verzeichnisse löschen
        mv - Dateien/Verzeichnisse verschieben
        cp - Dateien/Verzeichnisse kopieren
        Dateibearbeitung
        createTextFile - Neue Textdatei erstellen (z.B. createTextFile helloworld.groovy)
        editTextFile - Textdatei bearbeiten (z.B. editTextFile helloworld.groovy)
        cat - Dateiinhalt im Terminal anzeigen (z.B. cat file.txt)
        echo - Text ausgeben und in Dateien schreiben (z.B. echo foobar > file.txt oder echo more >> file.txt)
        Script-Ausführung & Monitoring
        exec - Script ausführen (z.B. exec helloworld.groovy)
        Flags: -s (silent/Hintergrund), -w (neues Fenster), -n (kein eigener Thread)
        ps - Liste der laufenden Scripts anzeigen
        kill - Script-Ausführung beenden
        kill ID - Script unterbrechen
        kill -f ID - Script hart beenden (force)
        Konfiguration
        config reload - Konfiguration neu laden (nach Änderungen an Config-Dateien)
        diffconfigfiles - Hilfe bei fehlenden Config-Dateien nach Upgrades
        Objekt-Informationen
        desc - Objekt-Beschreibung anzeigen (z.B. desc User id:User:3)
        Flag: -w (in neuem Fenster anzeigen)
        Scheduler
        scheduleScript - Scripts zeitgesteuert ausführen
        scheduleScript list - geplante Scripts auflisten
        scheduleScript execute - Script planen (z.B. scheduleScript execute myScript.groovy """" every day at 15:23)
        scheduler - Scheduler-Verwaltung
        scheduler listFireTimes - nächste Ausführungszeiten anzeigen
        scheduler remove - geplante Aufgabe entfernen
        scheduler daemon start/stop - Scheduler aktivieren/deaktivieren
        LDAP-Verwaltung
        ldaptest - LDAP-Konfiguration testen
        ldaptest users - Benutzer testen
        ldaptest groups - Gruppen testen
        ldaptest organizationalUnits - OUs testen
        ldaptest filter - Filter testen
        Flag: -s (Schema anzeigen)
        ldapfilter - LDAP-Filter analysieren
        ldapschema - LDAP-Schema erkunden (z.B. ldapschema objectClassInfo organizationalPerson)
        ldapguid - LDAP GUID-Informationen
        ldapinfo - LDAP-Informationen
        ldapimport - LDAP-Import durchführen
        ssltest - SSL-Konfiguration für LDAP testen
        Logging
        listlogfiles - Log-Dateien auflisten
        Flag: -e (per Email versenden)
        Flag: -f (Filter)
        Pakete & Installation
        pkg install - Pakete installieren (z.B. pkg install -d demobuilder -VERSION_NR)        
        Besondere Hinweise:
        Tab-Vervollständigung: Das Terminal unterstützt Autocomplete mit der TAB-Taste

        Pipes und Weiterleitungen:

        > - Ausgabe in Datei umleiten (überschreiben)
        >> - Ausgabe an Datei anhängen
        Rückgabewerte: Die letzte Zeile eines Scripts wird als Rückgabewert interpretiert und im Terminal angezeigt

        Terminal-Output: Das tout-Objekt kann für Ausgaben während der Script-Ausführung verwendet werden:

        tout.println('Hello World')";
}