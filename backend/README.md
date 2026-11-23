# Portfolio API

## Stack en structuur

De solution `Portfolio.sln` bevat de ASP.NET Core 10 Web API (`Portfolio.Api`) en het xUnit-integratieproject (`Portfolio.Api.Tests`). EF Core 10.0.14 gebruikt SQL Server voor de echte API. `PortfolioDbContext` erft van `IdentityDbContext<IdentityUser>` en roept eerst de Identity-basisconfiguratie aan; de bestaande mapping voor `Projects` en `Blogposts` blijft behouden.

Controllers en DTO’s vormen de HTTP-laag, services voeren de CRUD uit en EF Core slaat gegevens op. Zie ook [architectuur](./docs/architectuur.md).

## Vereisten

- .NET 10 SDK.
- Docker Desktop met SQL Server Developer, of een lokaal draaiende SQL Server/SQL Server Express.
- PowerShell 5.1 of hoger.
- De lokale `dotnet-ef`-tool uit `backend/Portfolio.Api/.config/dotnet-tools.json`.
- Voor de browser: een vertrouwd lokaal HTTPS-ontwikkelcertificaat.

## SQL Server lokaal starten (Docker)

Voer vanaf de repositoryroot in PowerShell uit. Het SA-wachtwoord wordt verborgen ingevoerd en niet in de opdrachtregel of een repositorybestand gezet:

```powershell
$securePassword = Read-Host "Kies een sterk lokaal SQL Server-wachtwoord" -AsSecureString
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $env:MSSQL_SA_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    docker run --name portfolio-sql --env ACCEPT_EULA=Y --env MSSQL_PID=Developer --env MSSQL_SA_PASSWORD --publish 1433:1433 --detach mcr.microsoft.com/mssql/server:2022-latest
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    $securePassword.Dispose()
    Remove-Item Env:MSSQL_SA_PASSWORD -ErrorAction SilentlyContinue
}
```

Herstart een bestaande container met `docker start portfolio-sql`. Gebruik bij een lokaal geïnstalleerde Express-instantie in plaats daarvan bijvoorbeeld `Start-Service -Name 'MSSQL$SQLEXPRESS'` en pas de connectionstring daarop aan.

## Connectionstring veilig instellen

`UserSecretsId` is al voor `Portfolio.Api` ingesteld. Sla geen wachtwoord op in `appsettings.json`. Voer dit vanuit de repositoryroot uit om de Docker-connectionstring in .NET user-secrets te zetten:

```powershell
$securePassword = Read-Host "SQL Server SA-wachtwoord" -AsSecureString
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
    $connectionString = "Server=localhost,1433;Database=PortfolioDb;User Id=sa;Password=$password;Encrypt=True;TrustServerCertificate=True"
    dotnet user-secrets set "ConnectionStrings:Portfolio" $connectionString --project .\backend\Portfolio.Api\Portfolio.Api.csproj
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    $securePassword.Dispose()
    Remove-Variable password, connectionString -ErrorAction SilentlyContinue
}
```

Voor SQL Server Express zonder SQL-login kun je de bestaande lokale standaardwaarde in `appsettings.json` laten staan. Een andere omgeving kan `ConnectionStrings__Portfolio` gebruiken; een environment variable overschrijft user-secrets.

## Restore, migratie en HTTPS starten

Deze opdrachten zijn bedoeld vanaf de repositoryroot:

```powershell
dotnet restore .\backend\Portfolio.sln
dotnet tool restore --tool-manifest .\backend\Portfolio.Api\.config\dotnet-tools.json
dotnet build .\backend\Portfolio.sln --no-restore
```

Pas de migraties toe op de **door jou geconfigureerde lokale database**. De aanvullende `AddIdentity`-migratie maakt uitsluitend de Identity-tabellen en indexen aan; zij wijzigt of verwijdert `Projects` en `Blogposts` niet. De API past bij een niet-testomgeving automatisch nog openstaande migraties toe voordat zij de Identity-rollen initialiseert.

```powershell
Push-Location .\backend\Portfolio.Api
try {
    dotnet ef database update
}
finally {
    Pop-Location
}
```

Start daarna HTTPS vanuit de repositoryroot. Vertrouw zo nodig eerst het lokale certificaat met `dotnet dev-certs https --trust`.

```powershell
dotnet run --project .\backend\Portfolio.Api\Portfolio.Api.csproj --launch-profile Portfolio.Api
```

De HTTPS-URL is `https://localhost:7099`; Swagger staat op `https://localhost:7099/swagger`. HTTP op poort 5099 blijft beschikbaar voor publieke GET-verzoeken, maar de authenticatiecookie wordt uitsluitend over HTTPS verstuurd.

## Accounts, rollen en cookies

Bij het starten maakt de API de rollen `Gast`, `Editor` en `Admin` idempotent aan. Een database moet eerst de EF-migraties hebben toegepast. `POST /api/auth/register` accepteert alleen e-mail en wachtwoord; de gebruikersnaam is de e-mail en nieuwe accounts krijgen uitsluitend `Gast`. Een publiek meegestuurd rolveld wordt genegeerd. Registratie en roltoekenning zijn één databasetransactie.

Maak drie herkenbare lokale accounts aan, ken Editor/Admin toe en test vervolgens nieuwe logins met aparte websessies met het demonstratiescript:

```powershell
.\backend\scripts\Invoke-PortfolioAuthDemo.ps1 -PauseForScreenshots
```

Het script vraagt de drie wachtwoorden verborgen, gebruikt unieke `AuthDemo`-accounts, toont alleen de gegenereerde e-mailadressen, houdt voor Gast/Editor/Admin aparte cookiecontainers aan en verwijdert uitsluitend de eigen aangemaakte project- en blogpostrecords. De accounts en door jou gekozen wachtwoorden blijven beschikbaar voor hergebruik in die lokale database.

Voor losse accounts kun je ook dit PowerShell-fragment vanaf de repositoryroot gebruiken. Het maakt drie nieuwe, unieke e-mailadressen, vraagt elk wachtwoord verborgen en logt na de eventuele roltoekenning opnieuw in met een aparte websessie. Kies wachtwoorden die aan de Identity-policy voldoen:

```powershell
$origin = "https://localhost:7099"
$tag = [Guid]::NewGuid().ToString("N").Substring(0, 8)
$accounts = @(
    [PSCustomObject]@{ Role = "Gast"; Email = "local-$tag-gast@example.test" },
    [PSCustomObject]@{ Role = "Editor"; Email = "local-$tag-editor@example.test" },
    [PSCustomObject]@{ Role = "Admin"; Email = "local-$tag-admin@example.test" }
)
$webSessions = @{}
foreach ($account in $accounts) {
    $securePassword = Read-Host "Wachtwoord voor $($account.Role)" -AsSecureString
    $passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
        $webSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
        $webSessions[$account.Role] = $webSession
        $body = @{ email = $account.Email; password = $password } | ConvertTo-Json -Compress
        $registered = Invoke-WebRequest -Uri "$origin/api/auth/register" -Method Post -WebSession $webSession -ContentType "application/json" -Body $body
        Write-Host "POST /api/auth/register rol=$($account.Role) status=$([int]$registered.StatusCode)"
        if ($account.Role -ne "Gast") {
            $roleResult = & dotnet run --project .\backend\Portfolio.Api\Portfolio.Api.csproj --launch-profile Portfolio.Api -- --role-email $account.Email --role-name $account.Role
            if ($LASTEXITCODE -ne 0) { throw "Lokale roltoekenning mislukt." }
        }
        $loginBody = @{ email = $account.Email; password = $password } | ConvertTo-Json -Compress
        $login = Invoke-WebRequest -Uri "$origin/api/auth/login" -Method Post -WebSession $webSession -ContentType "application/json" -Body $loginBody
        $cookie = $webSession.Cookies.GetCookies([Uri]$origin) | Where-Object Name -eq "Portfolio.Auth"
        Write-Host "POST /api/auth/login rol=$($account.Role) status=$([int]$login.StatusCode) cookieName=$($cookie.Name) HttpOnly=$($cookie.HttpOnly) aanwezig=$($null -ne $cookie)"
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
        $securePassword.Dispose()
        Remove-Variable password, body, loginBody -ErrorAction SilentlyContinue
    }
}
```

Een bestaand geregistreerd account kan ook afzonderlijk een rol krijgen. Voer de opdracht vanaf de repositoryroot uit; gebruik een bestaand e-mailadres:

```powershell
dotnet run --project .\backend\Portfolio.Api\Portfolio.Api.csproj --launch-profile Portfolio.Api -- --role-email "editor@example.test" --role-name Editor
dotnet run --project .\backend\Portfolio.Api\Portfolio.Api.csproj --launch-profile Portfolio.Api -- --role-email "admin@example.test" --role-name Admin
```

Toegestane doelrollen zijn `Gast`, `Editor` en `Admin`. De lokale opdracht gebruikt Identity-managers, controleert resultaten en voert verwijdering/toevoeging van rollen en security-stampwijziging atomair uit. Herhalen is veilig. Een rolwijziging verwerpt bestaande cookies; meld daarna opnieuw aan om de actuele rol te krijgen.

`POST /api/auth/login` zet de echte Identity-applicatiecookie; de response bevat geen cookie of wachtwoord. De cookie is `HttpOnly`, `Secure` en `SameSite=Strict`. Onbevoegde API-verzoeken krijgen 401/403 zonder loginredirect. De API controleert de security stamp bij **ieder** verzoek en verwerpt een cookie bij wijziging in plaats van de principal automatisch te vernieuwen.

### Cookie-, origin- en CSRF-inrichting

- De huidige API configureert geen CORS-origin. Gebruik bij voorkeur een same-origin SPA (bijvoorbeeld via dezelfde origin/reverse proxy); er is geen publieke beheerroute.
- De antiforgery-cookie is `HttpOnly`, `Secure`, `SameSite=Strict`. `GET /api/auth/csrf` retourneert een request token in de responsebody; een SPA bewaart dit token alleen in geheugen en stuurt het bij iedere ingelogde POST/PUT/DELETE mee als `X-CSRF-TOKEN`. Authenticated unsafe requests zonder geldig token geven 400. Anonieme register/login-verzoeken kunnen zonder token.
- De frontend gebruikt `fetch(..., { credentials: "include" })` of `XMLHttpRequest.withCredentials = true` wanneer cookies over een origin-grens nodig zijn. Alleen als een concrete andere origin echt nodig is, configureer die exacte HTTPS-origin met `AllowCredentials()` en beperkte methoden/headers; nooit `*`. Behoud het CSRF-token.
- Een SPA op een andere **site** krijgt met `SameSite=Strict` geen cookie. Kies dan same-origin hosting of ontwerp bewust een andere cookie/CSRF-configuratie, inclusief `Secure`, exacte CORS-origin en tokenvalidatie; zet `SameSite=None` niet zonder die maatregelen.

## API-routes

Project- en blogpostroutes blijven behouden. De vier GET-acties zijn publiek. Alle zes POST/PUT/DELETE-acties vereisen één policy `RequireRole("Editor", "Admin")`.

| Methode | Route | Anoniem | Gast | Editor/Admin |
|---|---|---:|---:|---:|
| GET | `/api/projects` | 200 | 200 | 200 |
| GET | `/api/projects/{id}` | 200 of 404 | 200 of 404 | 200 of 404 |
| POST | `/api/projects` | 401 | 403 | 201 |
| PUT | `/api/projects/{id}` | 401 | 403 | 204 of 404 |
| DELETE | `/api/projects/{id}` | 401 | 403 | 204 of 404 |
| GET | `/api/blogposts` | 200 | 200 | 200 |
| GET | `/api/blogposts/{id}` | 200 of 404 | 200 of 404 | 200 of 404 |
| POST | `/api/blogposts` | 401 | 403 | 201 |
| PUT | `/api/blogposts/{id}` | 401 | 403 | 204 of 404 |
| DELETE | `/api/blogposts/{id}` | 401 | 403 | 204 of 404 |
| POST | `/api/auth/register` | 201 of 400 | — | — |
| POST | `/api/auth/login` | 200 of 401 | — | — |
| GET | `/api/auth/csrf` | 200 | 200 | 200 |

Ongeldige DTO’s geven 400; onbekende IDs voor GET/PUT/DELETE geven 404. POST gebruikt de bestaande DTO’s en retourneert 201, een gegenereerd ID en `Location`.

## Geautomatiseerde tests en coverage

De integratietests gebruiken `WebApplicationFactory`, de echte middleware, Identity EF-stores en applicatiecookie. Iedere test maakt een eigen open SQLite in-memory-verbinding en schema; er is geen gedeelde database of parallelle testdata. De testfactory vervangt SQL Server voordat de API start, zodat de gewone connectionstring niet kan worden gemigreerd, geleegd of gevuld. Tests gebruiken echte registratie/login en een eigen cookiecontainer per identiteit, zonder test-authhandler of zelfgemaakte role claims.

SQLite is hier relationeel, maar niet de productieprovider. Het testschema wordt met `EnsureCreated` opgebouwd, niet met de SQL Server-migraties. SQL Server-specifieke migratie-/providersemantiek is daarmee geen onderdeel van de testdekking; de `AddIdentity`-migratie is afzonderlijk beoordeeld op alleen additieve Identity-tabellen.

```powershell
dotnet test .\backend\Portfolio.sln --logger "console;verbosity=normal"
dotnet test .\backend\Portfolio.sln --settings .\backend\Portfolio.Api.Tests\coverage.runsettings --collect:"XPlat Code Coverage" --logger "trx;LogFileName=auth-coverage.trx" --results-directory .\backend\docs\test-evidence\coverage
```

Herhaal de testopdracht om database-/parallelisatie-isolatie te controleren. Het runsettings-bestand filtert coverage tot `[Portfolio.Api]*`. Het coverageverslag, de echte testuitvoer, TRX-testscenario’s en Cobertura-XML staan in [test evidence](./docs/test-evidence/auth-coverage-report.md) en de onderliggende map [test-evidence](./docs/test-evidence/).

### Route/testmatrix

De methodenamen hieronder zijn de concrete xUnit-testnamen; de route, rol en data zijn zichtbaar als losse theory-cases in de testuitvoer/TRX.

| Route(s) | Testnaam | Verwachte/asserted status | Testbestand |
|---|---|---|---|
| `GET /api/projects`, `GET /api/projects/{id}` | `Public_get_routes_return_concrete_records_and_unknown_id_is_404` | lijst/detail 200, onbekend ID 404, concrete DTO-velden | [WritePolicyTests.cs](./Portfolio.Api.Tests/WritePolicyTests.cs) |
| `GET /api/blogposts`, `GET /api/blogposts/{id}` | `Public_get_routes_return_concrete_records_and_unknown_id_is_404` | lijst/detail 200, onbekend ID 404, concrete DTO-velden | [WritePolicyTests.cs](./Portfolio.Api.Tests/WritePolicyTests.cs) |
| Projects/Blogposts `POST`, `PUT`, `DELETE` | `Write_policy_matrix_has_expected_status_and_mutation` | per route: anoniem 401, Gast 403, Editor 201/204, Admin 201/204; verificatie via GET | [WritePolicyTests.cs](./Portfolio.Api.Tests/WritePolicyTests.cs) |
| Projects/Blogposts `POST`, `PUT` | `Invalid_write_body_returns_400_without_mutation` | 400, bestaande records ongewijzigd | [WritePolicyTests.cs](./Portfolio.Api.Tests/WritePolicyTests.cs) |
| Projects/Blogposts `PUT`, `DELETE` onbekend ID | `Write_for_unknown_id_returns_404_without_mutation` | 404, bestaande records ongewijzigd | [WritePolicyTests.cs](./Portfolio.Api.Tests/WritePolicyTests.cs) |
| `POST /api/auth/register` | `Registration_ignores_public_role_request_and_assigns_only_guest`, `Registration_duplicate_returns_400_and_preserves_original_account`, `Registration_weak_password_returns_400_without_creating_account`, `Registration_invalid_email_returns_400` | 201 uitsluitend Gast; rolmanipulatie genegeerd; duplicaat/zwak/ongeldig 400 | [AuthTests.cs](./Portfolio.Api.Tests/AuthTests.cs) |
| `POST /api/auth/login` | `Login_sets_httponly_cookie_and_cookie_is_reused_for_authorization`, `Failed_login_is_generic_and_does_not_create_a_usable_session` | 200 + HttpOnly-cookie echt hergebruikt; onjuist wachtwoord generiek 401 zonder sessie | [AuthTests.cs](./Portfolio.Api.Tests/AuthTests.cs) |
| `GET /api/auth/csrf` + cookie-writes | `Authenticated_write_without_csrf_token_returns_400_without_mutation` | token 200; ontbrekende CSRF-token 400 zonder mutatie | [AuthTests.cs](./Portfolio.Api.Tests/AuthTests.cs) |
| Lokale Identity-rolwijziging en CRUD-cookie | `Promotion_and_revocation_invalidate_old_cookies_and_require_fresh_login` | oude cookies na promotie/intrekking 401; verse Editor 201; verse Gast 403 | [SecurityStampTests.cs](./Portfolio.Api.Tests/SecurityStampTests.cs) |

De matrix omvat twaalf gevraagde businessroutes (de tien bestaande CRUD-routes plus register/login); `/api/auth/csrf` is een aanvullende CSRF-hulproute.

## Zeven lokale demonstratiestappen

Er zijn in deze werkronde geen screenshots opgeslagen. Voer de volgende opdracht **eenmaal** uit met de API en lokale database gestart; het script toont per actie methode, pad, rol en werkelijke status, maar nooit wachtwoorden, ruwe headers, cookies of CSRF-tokens. Bij login toont het uitsluitend cookienaam, `HttpOnly` en cookie-aanwezigheid. Bij iedere genummerde pauze kun je de bijbehorende screenshot maken:

```powershell
.\backend\scripts\Invoke-PortfolioAuthDemo.ps1 -PauseForScreenshots
```

| Screenshot | Exacte opdracht/actie | Inhoud |
|---|---|---|
| 1 | `.\backend\scripts\Invoke-PortfolioAuthDemo.ps1 -PauseForScreenshots`, pauze `1/7` | publieke GET en anonieme 401 |
| 2 | dezelfde lopende opdracht, pauze `2/7` | Gast-GET en Gast-403 |
| 3 | dezelfde lopende opdracht, pauze `3/7` | Editor POST 201 en GET |
| 4 | dezelfde lopende opdracht, pauze `4/7` | Admin POST/PUT en GET met gewijzigde data |
| 5 | dezelfde lopende opdracht, pauze `5/7` | DELETE 204 en GET 404 |
| 6 | dezelfde lopende opdracht, pauze `6/7` | onbekende PUT en DELETE 404 |
| 7 | dezelfde lopende opdracht, pauze `7/7` | rolpromotie/intrekking, oude cookie 401 en verse login met actuele rechten |

Druk bij iedere pauze Enter nadat je de screenshot hebt opgeslagen. De demo maakt uitsluitend eigen herkenbare tijdelijke portfolio-records aan en ruimt die op.

Persoonlijke uren, feedback en reflectie zijn bewust niet ingevuld.
