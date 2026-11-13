<p align="center"><img src="icon.png" width="128" alt="Fast Vitae icon"></p>

# Fast Vitae

A portable Windows app for making a tailored CV for each job application, fast.

You fill in a form on the left and see the finished CV on the right. Fast Vitae does all the formatting and writes a PDF and a Word file.

## Download

Get `FastVitae.exe` from the [latest release](https://github.com/facundogiovine/FastVitae/releases/latest), put it in any folder and run it. There is nothing to install.

The exe is not code-signed yet, so Windows may show "Windows protected your PC" the first time. Click **More info**, then **Run anyway**.

## Features

- One CV per company and role, saved in a folder named after the company.
- Start a CV from scratch, from saved content, or from a CV you already made.
- **Fields** and **Specialties** (for example Programming, then C#) hold reusable content, so a new application starts pre-filled.
- Sections in two formats: bullet columns (up to four), or name with bullet points. Add, rename, reorder and remove them.
- Role dates picked from month and year lists, including Present.
- Right-click any CV field to set its font, size, colour, bold, italic or strikethrough. Apply one format to all dates, titles, companies or locations at once.
- English, Spanish and Portuguese, for both the app and the CV's fixed words.
- **Applications** tab: every CV you create is logged with its Field, company, role and date. Mark each one Applied, In process, Approved or Rejected, filter the list, open the CV you sent or its folder. The list lives in one Excel file per profile, and Excel is not needed to use it.
- Optional picture header: name and title on the left, picture on the right. Replaced pictures are kept, numbered.
- Move the data folder, or send only new CVs to another folder.
- Several profiles on one install. No accounts, no sign-up.
- Works fully offline. No database. Your data is plain JSON files in a folder you choose.
- One exe of about 60 MB. Nothing to install.

## Requirements

- Windows 10 or 11.
- Microsoft Edge WebView2 Runtime, which ships with Windows 11 and current Windows 10.

## Build

You need the .NET 10 SDK.

```
dotnet build
```

To make the portable exe in the `publish` folder:

```
publish.cmd
```

## Where data is stored

By default in `Documents\Fast Vitae`. You can pick another folder on first run.

```
Fast Vitae\
  Profiles\
    First Last\
      profile.json
      picture.jpg
      First_Last_Picture_1.jpg
      Applications.xlsx
      Fields\Programming\C#.json
      CVs\Company\Role.json
      CVs\Company\First Last - Role.pdf
      CVs\Company\First Last - Role.docx
```

`settings.json` next to the exe remembers the data folder, the last profile used, the language and, if set, a separate folder for new CVs.

## Privacy

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it.

## Releases

Every release is built by GitHub Actions from the tagged source in this repository. See `.github/workflows/build.yml`.

Project owner, committer and release approver: [facundogiovine](https://github.com/facundogiovine).

## License

MIT. See `LICENSE`.

## Third-party

- [STIX Two Text](https://github.com/stipub/stixfonts) fonts, SIL Open Font License 1.1. See `src/FastVitae/Fonts/OFL.txt`.
- [FamFamFam flag icons](https://www.nuget.org/packages/FamFamFam.Flags.Wpf) by Mark James, public domain.
- [Open XML SDK](https://github.com/dotnet/Open-XML-SDK), MIT License.
- [ClosedXML](https://github.com/ClosedXML/ClosedXML), MIT License.
- [Microsoft Edge WebView2](https://developer.microsoft.com/microsoft-edge/webview2/).
