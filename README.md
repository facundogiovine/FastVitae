<p align="center"><img src="icon.png" width="128" alt="FastVitae icon"></p>

# FastVitae

A portable Windows app for making a tailored CV for each job application, fast.

You fill in a form on the left and see the finished CV on the right. FastVitae does all the formatting and writes a PDF and a Word file.

## Features

- One CV per company and role, saved in a folder named after the company.
- Start a CV from scratch, from saved content, or from a CV you already made.
- **Fields** and **Specialties** (for example Programming, then C#) hold reusable content, so a new application starts pre-filled.
- Sections in two formats: bullet columns, or name with bullet points. Add, rename, reorder and remove them.
- Optional picture header: name and title on the left, picture on the right.
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

```
Data\
  Profiles\
    First Last\
      profile.json
      picture.jpg
      Fields\Programming\C#.json
      CVs\Company\Role.json
      CVs\Company\First Last - Role.pdf
      CVs\Company\First Last - Role.docx
```

`settings.json` next to the exe remembers the data folder and the last profile used.

## License

All rights reserved. The source is public for viewing only. See `LICENSE`.

## Third-party

- [STIX Two Text](https://github.com/stipub/stixfonts) fonts, SIL Open Font License 1.1. See `src/FastVitae/Fonts/OFL.txt`.
- [FamFamFam flag icons](https://www.nuget.org/packages/FamFamFam.Flags.Wpf) by Mark James, public domain.
- [Open XML SDK](https://github.com/dotnet/Open-XML-SDK), MIT License.
- [Microsoft Edge WebView2](https://developer.microsoft.com/microsoft-edge/webview2/).
