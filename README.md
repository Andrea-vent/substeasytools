# substEasyTools

Piccola utility Windows con interfaccia Fluent (Mica/Win11) per creare al volo lettere di unità che puntano a percorsi di rete UNC o cartelle locali lunghe, aggirando i limiti di Windows sui path lunghi.

![Icona](icon-source.png)

## Cosa fa

- Inserisci un percorso (UNC tipo `\\server\condivisione` oppure locale tipo `C:\percorso\molto\lungo`)
- L'app rileva automaticamente le lettere di unità libere
- Crea il collegamento provvisorio:
  - `net use X: \\server\share /persistent:no` per i percorsi UNC
  - `subst X: C:\percorso` per i percorsi locali
- Mostra l'elenco dei collegamenti attivi nella sessione, ognuno con un pulsante **Elimina percorso provvisorio**
- All'uscita pulisce automaticamente tutti i collegamenti creati (opzionale, attivo di default)

Nessun privilegio amministratore richiesto.

## Requisiti

- Windows 10 / 11 x64
- Per usare l'eseguibile: nessun runtime richiesto (build self-contained)
- Per compilare da sorgente: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build

```powershell
dotnet publish -c Release -o publish
```

Output: `publish/substEasyTools.exe` (~70 MB, single-file self-contained).

## Struttura

| File | Descrizione |
|------|-------------|
| `PathMapper.csproj` | Progetto WPF .NET 8, riferimento a [WPF-UI](https://github.com/lepoco/wpfui) |
| `App.xaml` / `App.xaml.cs` | Bootstrap applicazione, tema dark Fluent |
| `MainWindow.xaml` | UI: `FluentWindow` con backdrop Mica e angoli arrotondati |
| `MainWindow.xaml.cs` | Logica: rilevamento lettere, esecuzione `subst`/`net use`, cleanup |
| `app.ico` | Icona multi-size (16-256px) |
| `icon-source.png` | Sorgente PNG dell'icona |
| `make-icon.ps1` | Script per rigenerare `app.ico` da una sorgente PNG |

## Note tecniche

- `subst` non accetta direttamente percorsi UNC: l'app fa il routing automatico verso `net use`
- I collegamenti `net use` sono creati con `/persistent:no` per non sopravvivere al riavvio
- All'avvio l'elenco delle lettere disponibili viene popolato escludendo le unità già montate dal sistema

## Licenza

Uso interno.
