# Third-party components

Client assets are stored locally; pages do not require CDN access at runtime.

Sample activity photos are downloaded from Pexels and stored as database binary files. See [sample photo credits and source links](SAMPLE-IMAGE-SOURCES.md) for the per-photo credits and license. These are illustrative stock photos, not records of actual school events.

| Component | Version | License | Source |
|---|---|---|---|
| Bootstrap | MVC template bundled version | MIT | https://github.com/twbs/bootstrap |
| Bootswatch Materia | 5.3.8 | MIT | https://github.com/thomaspark/bootswatch |
| PDF.js | 4.10.38 | Apache-2.0 | https://github.com/mozilla/pdf.js |
| PDFsharp | 6.2.4 | MIT | https://github.com/empira/PDFsharp |
| Sarabun (Regular / SemiBold / Bold) | Local TTF files | SIL Open Font License 1.1 | https://github.com/google/fonts/tree/main/ofl/sarabun |
| jQuery and validation libraries | MVC template bundled versions | MIT | licenses included under wwwroot/lib |

Materia's external Google Fonts import was removed to keep assets local; the web interface uses bundled Sarabun fonts with the license in `wwwroot/fonts/OFL.txt`. Generated PDF reports use the server's Thai fonts. Copyright notices remain in the vendor files. NuGet dependencies and exact versions are listed in the project file and are restored from NuGet. Playwright installs its matching Chromium build separately through `playwright.ps1 install chromium`.
