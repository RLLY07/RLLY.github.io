# Identity- en portfolio-API testverslag

**Uitvoerdatum:** 2026-11-23  
**Solution:** `backend/Portfolio.sln`  
**Target framework:** `net10.0`  
**Productieprovider:** SQL Server, EF Core 10.0.14  
**Testprovider:** geïsoleerde SQLite in-memory per test, met een open verbinding per factory

## Resultaat

| Uitvoering | Geslaagd | Mislukt | Overgeslagen |
|---|---:|---:|---:|
| Coverage-run | 46 | 0 | 0 |
| Isolatieherhaling | 46 | 0 | 0 |

De solution restore en build zijn ook geslaagd; de build gaf 0 warnings en 0 errors. De concrete testnamen, data-driven route/rolcombinaties en resultaten staan in de [coverage-TRX](./coverage-run/auth-coverage.trx) en [herhaal-TRX](./isolation-rerun/auth-isolation.trx). De echte console-uitvoer is bewaard in [test-output-coverage.txt](./test-output-coverage.txt) en [test-output-isolation.txt](./test-output-isolation.txt).

De 46 testcases bestaan uit:

- 24 write-policycombinaties: elk van de zes Project/Blogpost POST/PUT/DELETE-routes als anoniem, Gast, Editor en Admin.
- 4 ongeldige POST/PUT-bodies, 4 PUT/DELETE’s op onbekende IDs en 2 publieke GET-lijst/detail/404-cases.
- 10 registratie-/login-/CSRF-/rolinitialisatiecases.
- 2 security-stamp-/rolwijzigingscases, waaronder promotie en intrekking met bestaande cookie en verse login.

## Commands en coverage

Uitgevoerd vanuit de repositoryroot:

```powershell
dotnet restore .\backend\Portfolio.sln
dotnet build .\backend\Portfolio.sln --no-restore
dotnet test .\backend\Portfolio.sln --no-build --no-restore --settings .\backend\Portfolio.Api.Tests\coverage.runsettings --collect:"XPlat Code Coverage" --logger "console;verbosity=minimal" --logger "trx;LogFileName=auth-coverage.trx" --results-directory .\backend\docs\test-evidence\coverage-run
dotnet test .\backend\Portfolio.sln --no-build --no-restore --logger "console;verbosity=minimal" --logger "trx;LogFileName=auth-isolation.trx" --results-directory .\backend\docs\test-evidence\isolation-rerun
```

De collector is `coverlet.collector` 6.0.4 (XPlat Code Coverage/Cobertura). `coverage.runsettings` beperkt de meet-scope tot de productieassembly `[Portfolio.Api]*`; de tests-assembly telt niet mee. Gemeten op de volledige API-assembly, inclusief gegenereerde EF-migratiecode:

- **Line coverage: 30,53%** — 400 van 1.310 regels.
- **Branch coverage: 79,54%** — 70 van 88 branches.

Het ruwe Cobertura-bestand is [coverage.cobertura.xml](./coverage-run/42e64067-ef9a-43e4-86d0-99dfe6c5b792/coverage.cobertura.xml). De lage totale line coverage wordt mede veroorzaakt door de gegenereerde migraties en model snapshot in dezelfde assembly; de Coverage-run voert geen `dotnet ef`-migratie uit.

## Twaalf businessroutes en beslispaden

De bestaande tien CRUD-routes en de twee auth-routes zijn met echte HTTP-verzoeken door `WebApplicationFactory` getest. De volledige rolmatrix staat als afzonderlijke theoriegevallen in de TRX:

- Publieke `GET /api/projects`, `GET /api/projects/{id}`, `GET /api/blogposts` en `GET /api/blogposts/{id}`: 200 met gecontroleerde DTO-velden; ontbrekende IDs 404.
- De zes schrijfroutecombinaties voor Projects en Blogposts: anoniem 401, Gast 403, Editor en Admin 201 voor POST en 204 voor PUT/DELETE. POST controleert ID, `Location` en opslag via nieuwe GET; PUT/DELETE controleren gewijzigde/ongewijzigde staat en 404 na verwijderen.
- Ongeldige POST/PUT: 400 zonder mutatie. Onbekende PUT/DELETE-IDs: 404 zonder mutatie.
- `POST /api/auth/register`: 201 en uitsluitend Gast; rolmanipulatie, duplicaat, zwak wachtwoord en ongeldig e-mailadres geven de verwachte 400.
- `POST /api/auth/login`: 200 met HttpOnly Secure-cookie die de client echt hergebruikt; verkeerde inloggegevens geven generieke 401 zonder sessie.
- `/api/auth/csrf`: tokenuitgifte en 400 bij een ingelogde write zonder geldig token. De hulproute maakt geen deel uit van de twaalf businessroutes.
- Security-stamp: oude cookies na promotie/intrekking 401; verse Editor-login mag schrijven (201), verse Gast-login niet (403). Gelijkblijvende roltoekenning is idempotent.

De testfactory vervangt SQL Server door een eigen SQLite in-memory-verbinding vóór de host start; iedere testcase heeft een aparte verbinding/database en cookiecontainer per identiteit. Geen test migreert, wist of vult de gewone Portfolio-database. De tests gebruiken de Identity EF-stores en echte login/cookiepipeline, niet een test-authhandler of zelfgemaakte role claims.

## SQL-migratie en gegevensbehoud

Los van de SQLite-testprovider is de migratie ook tegen een tijdelijke, unieke SQL Server Express-database gecontroleerd. De initiële migratie maakte de lege bestaande Projects/Blogposts-schema’s aan; daarna zijn sentinel-rijen toegevoegd en is `AddIdentity` toegepast. De controle na migratie gaf **Projects=1, Blogposts=1** en een geslaagde query op de Identity-tabellen. De tijdelijke verificatiedatabase `PortfolioIdentityVerification_20261123_123745` is daarna verwijderd. De gewone `PortfolioDb` is niet gemigreerd of gewijzigd.

De diff van `AddIdentity` bevat alleen `CREATE TABLE`/indexen voor Identity en voegt geen verwijdering of wijziging van de bestaande portfoliotabellen toe.

## Niet door de automated coverage afgedekte paden

- De lokale CLI-starttak en haar onvolledige argumentvalidatie: [Program.cs](../../Portfolio.Api/Program.cs) regels 90–100; de twee branches op regel 91. De rolservice zelf is wel via Identity-managers getest; het starten van de CLI als apart proces zit niet in xUnit.
- De fouttak als Gast-roltoekenning tijdens registratie mislukt: [AuthController.cs](../../Portfolio.Api/Controllers/AuthController.cs) regels 44–45. De rol bestaat na idempotente initialisatie; de test forceert geen storage-/concurrencyfout.
- De fouttak als een rol niet kan worden aangemaakt: [IdentityRoleInitializer.cs](../../Portfolio.Api/Services/IdentityRoleInitializer.cs) regels 22–25.
- De foutvertaling van een mislukte Identity-rolmutatie: [RoleAdministrationService.cs](../../Portfolio.Api/Services/RoleAdministrationService.cs) regels 44–49. Succes, promotie, intrekking, idempotentie en onbekende rol/gebruiker zijn wel getest.
- De SQL Server-migratiecode en gegenereerde designers/snapshot zijn geen onderdeel van de SQLite-testcoverage. De migratie is afzonderlijk op SQL Server toegepast en op behoud van bestaande records gecontroleerd zoals hierboven beschreven.

## Lokale screenshots

Er zijn geen screenshots als testresultaat opgenomen. Voer met een lokaal gemigreerde API eenmaal de interactieve demonstratie uit; de zeven pauzes en exacte opdracht staan in de [backend-README](../README.md#zeven-lokale-demonstratiestappen). Die demo vraagt wachtwoorden verborgen en schrijft geen wachtwoorden, cookies, ruwe headers of CSRF-tokens naar de uitvoer.
