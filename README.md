<p align="center"><img src="icon.png" width="128" alt="Fast Vitae icon"></p>

# Fast Vitae

A portable Windows app for making a tailored CV for each job you apply to, and keeping track of those applications.

You fill in a form on the left and see the finished CV on the right. Fast Vitae does all the formatting and writes a PDF and a Word file.

## Download

Get `FastVitae.exe` from the [latest release](https://github.com/facundogiovine/FastVitae/releases/latest), put it in any folder and run it. There is nothing to install.

The exe is not code-signed yet, so Windows may show "Windows protected your PC" the first time. Click **More info**, then **Run anyway**.

When a new version is out, a green **Update** button appears in the top bar. One click downloads it, replaces the exe and restarts the app.

## Features

### CVs
- One CV per Field and role, for example Software Development, then C# Developer.
- Start a new CV from scratch, from saved content (a Specialty), or from a CV you already made.
- A CV from scratch shows every section with its title and an example line in the preview, so you can see the layout before filling it in. Examples never reach the PDF or Word file.
- Sections in two formats: bullet columns (up to four), or name with bullet points. Add, rename, reorder and remove them.
- Role dates picked from month and year lists, including Present.
- Right-click any CV field to set its font, size, colour, bold, italic or strikethrough. Apply one format to all dates, titles, companies or locations at once. Fonts missing on the PC are grayed out.
- Optional picture header: name and title on the left, picture on the right. Pick which part of the photo to use, with a live preview. Replaced pictures are kept, numbered.
- Creating a CV again never overwrites the earlier PDF; the new one is numbered.

### Languages
- The app comes in English, Spanish and Portuguese.
- Each CV has its own language, chosen when you create it and changeable in the editor. Section titles, month names and "Present" follow it.

### Fields and Specialties
- **Fields** (for example Hospitality or Software Development) group your CVs.
- **Specialties** inside a Field hold reusable content, so a new CV starts pre-filled.

### Applications
- Add an application with the company, the Field, the CV you sent, the expected salary range and the link to the job posting.
- The salary currency list is searchable by country or currency name. US Dollar, Euro, British Pound, Canadian Dollar and Australian Dollar come first.
- A frozen copy of the CV's PDF is kept with each application, so you always know exactly what you sent, even after editing the CV.
- Mark each application Applied, In process, Approved or Rejected, filter the list, and open the job link, the CV you sent or its folder.
- The list lives in one Excel file per profile. Excel is not needed to use it.

### Everything else
- Several profiles on one PC. No accounts, no sign-up.
- Move the data folder, or send only new CVs to another folder.
- No database. Your data is plain JSON files in a folder you choose.
- One exe of about 60 MB. Nothing to install.

## Requirements

- Windows 10 or 11.
- Microsoft Edge WebView2 Runtime, which ships with Windows 11 and current Windows 10.

## Where data is stored

By default in `Documents\Fast Vitae`. You can pick another folder on first run or later with **Save Folder**.

```
Fast Vitae\
  Profiles\
    First Last\
      profile.json
      picture.jpg
      First_Last_Picture_1.jpg                       (replaced picture, kept)
      Applications.xlsx
      Applications\
        Acme - C# Developer - 2026-11-18.pdf          (frozen copy of the CV sent)
      Fields\
        Software Development\
          Specialties\C#.json
          CVs\C# Developer.json
          CVs\First Last - C# Developer.pdf
          CVs\First Last - C# Developer.docx
          CVs\First Last - C# Developer_Pic.pdf       (CV with picture)
          CVs\First Last - C# Developer_2.pdf         (created again; never overwritten)
```

Settings (data folder, last profile, language, and a separate folder for new CVs if set) are kept in `%AppData%\FastVitae\settings.json`, so every copy of the exe on the same PC opens the same profile. Older versions kept `settings.json` next to the exe; the first time a new version runs, it moves that file to `%AppData%`.

## Privacy

Fast Vitae works offline. Its only network connection is a check for a newer version when the app opens: one request to the GitHub Releases API for this repository (`api.github.com/repos/facundogiovine/FastVitae/releases/latest`). No personal data, CV content or usage information is sent. If an update is found, the new exe is downloaded from GitHub only when you click **Update**.

To turn the check off, set `"CheckForUpdates": false` in `%AppData%\FastVitae\settings.json`.

## Build

You need the .NET 10 SDK.

```
dotnet build
dotnet test
```

To make the portable exe in the `publish` folder:

```
publish.cmd
```

## Releases

Every release is built by GitHub Actions from the tagged source in this repository. See `.github/workflows/build.yml`. Pushing a tag like `v1.3.3` builds `FastVitae.exe` and publishes it as a GitHub release.

Project owner, committer and release approver: [facundogiovine](https://github.com/facundogiovine).

## License

MIT. See `LICENSE`.

## Third-party

- [STIX Two Text](https://github.com/stipub/stixfonts) fonts, SIL Open Font License 1.1. See `src/FastVitae/Fonts/OFL.txt`.
- [FamFamFam flag icons](https://www.nuget.org/packages/FamFamFam.Flags.Wpf) by Mark James, public domain.
- [Open XML SDK](https://github.com/dotnet/Open-XML-SDK), MIT License.
- [ClosedXML](https://github.com/ClosedXML/ClosedXML), MIT License.
- [Microsoft Edge WebView2](https://developer.microsoft.com/microsoft-edge/webview2/). The runtime is part of Windows and is not included in the exe.
