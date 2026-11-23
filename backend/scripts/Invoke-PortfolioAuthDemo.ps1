[CmdletBinding()]
param(
    [string]$BaseUrl = "https://localhost:7099",
    [switch]$PauseForScreenshots
)

$ErrorActionPreference = "Stop"
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$apiProject = Join-Path $repositoryRoot "backend\Portfolio.Api\Portfolio.Api.csproj"
$marker = "AuthDemo-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
$emails = @{
    Gast = "$($marker.ToLowerInvariant())-gast@example.test"
    Editor = "$($marker.ToLowerInvariant())-editor@example.test"
    Admin = "$($marker.ToLowerInvariant())-admin@example.test"
}
$passwords = @{}
$sessions = @{}
$ownedProjects = [System.Collections.Generic.List[int]]::new()
$ownedBlogposts = [System.Collections.Generic.List[int]]::new()

function Read-HiddenPassword([string]$Prompt) {
    $securePassword = Read-Host -Prompt $Prompt -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
        $securePassword.Dispose()
    }
}

function New-WebSession {
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.CookieContainer = [System.Net.CookieContainer]::new()
    $handler.UseCookies = $true
    $handler.AllowAutoRedirect = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.BaseAddress = [Uri]$BaseUrl
    return [PSCustomObject]@{ Client = $client; Handler = $handler }
}

function Invoke-DemoRequest(
    [string]$Step,
    [string]$Role,
    [string]$Method,
    [string]$Path,
    [object]$Body,
    [string]$CsrfToken,
    [object]$Session
) {
    $request = [System.Net.Http.HttpRequestMessage]::new(
        [System.Net.Http.HttpMethod]::new($Method),
        $Path
    )
    if ($null -ne $Body) {
        $json = ConvertTo-Json -InputObject $Body -Depth 8 -Compress
        $request.Content = [System.Net.Http.StringContent]::new(
            $json,
            [System.Text.Encoding]::UTF8,
            "application/json"
        )
    }
    if (-not [string]::IsNullOrWhiteSpace($CsrfToken)) {
        [void]$request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", $CsrfToken)
    }

    $response = $Session.Client.SendAsync($request).GetAwaiter().GetResult()
    try {
        $status = [int]$response.StatusCode
        Write-Host "$Step | $Method $Path | rol=$Role | status=$status"
        if ($Path -eq "/api/auth/login") {
            $setCookieValues = @()
            $hasCookie = $response.Headers.Contains("Set-Cookie")
            if ($hasCookie) {
                $setCookieValues = @($response.Headers.GetValues("Set-Cookie"))
            }
            $cookieName = ""
            $httpOnly = $false
            if ($hasCookie -and $setCookieValues.Count -gt 0) {
                $cookieName = $setCookieValues[0].Split("=")[0]
                $httpOnly = $setCookieValues[0].Contains("httponly", [StringComparison]::OrdinalIgnoreCase)
            }
            Write-Host "  cookieName=$cookieName; HttpOnly=$httpOnly; Set-CookiePresent=$hasCookie"
        }
        elseif ($response.Content.Headers.ContentType.MediaType -eq "application/json") {
            $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if (-not [string]::IsNullOrWhiteSpace($text)) {
                $data = ConvertFrom-Json -InputObject $text
                if ($data -is [array]) {
                    Write-Host "  responseFields=count:$($data.Count)"
                }
                else {
                    $safeFields = @()
                    foreach ($field in @("id", "titel", "message", "errors")) {
                        if ($data.PSObject.Properties.Name -contains $field) {
                            $safeFields += "${field}:$($data.$field)"
                        }
                    }
                    if ($safeFields.Count -gt 0) {
                        Write-Host "  responseFields=$($safeFields -join '; ')"
                    }
                }
            }
        }

        $parsed = $null
        if ($response.Content.Headers.ContentType.MediaType -eq "application/json") {
            $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if (-not [string]::IsNullOrWhiteSpace($text)) {
                $parsed = ConvertFrom-Json -InputObject $text
            }
        }
        return [PSCustomObject]@{ Status = $status; Body = $parsed }
    }
    finally {
        $response.Dispose()
        $request.Dispose()
    }
}

function Get-DemoCsrfToken([string]$Role, [object]$Session) {
    $response = Invoke-DemoRequest "CSRF token" $Role "GET" "/api/auth/csrf" $null $null $Session
    if ($response.Status -ne 200 -or [string]::IsNullOrWhiteSpace($response.Body.requestToken)) {
        throw "Could not obtain an anti-forgery token."
    }
    return $response.Body.requestToken
}

function Register-And-Login([string]$Role, [object]$Session) {
    $registration = Invoke-DemoRequest "Account registration" $Role "POST" "/api/auth/register" @{
        email = $emails[$Role]
        password = $passwords[$Role]
    } $null $Session
    if ($registration.Status -ne 201) {
        throw "Registration failed with HTTP $($registration.Status)."
    }

    $login = Invoke-DemoRequest "Login" $Role "POST" "/api/auth/login" @{
        email = $emails[$Role]
        password = $passwords[$Role]
    } $null $Session
    if ($login.Status -ne 200) {
        throw "Login failed with HTTP $($login.Status)."
    }
}

function Set-DemoRole([string]$Email, [string]$Role) {
    & dotnet run --project $apiProject --launch-profile "Portfolio.Api" -- --role-email $Email --role-name $Role
    if ($LASTEXITCODE -ne 0) {
        throw "The local role command failed."
    }
}

function Assert-DemoStatus([object]$Response, [int]$Expected) {
    if ($Response.Status -ne $Expected) {
        throw "Expected HTTP $Expected but received HTTP $($Response.Status)."
    }
}

function Wait-DemoScreenshot([int]$Step) {
    if ($PauseForScreenshots) {
        [void](Read-Host "Screenshot $Step/7 gereed; maak de opname en druk Enter om door te gaan")
    }
}

try {
    $passwords.Gast = Read-HiddenPassword "Wachtwoord voor herkenbaar lokaal Gast-account"
    $passwords.Editor = Read-HiddenPassword "Wachtwoord voor herkenbaar lokaal Editor-account"
    $passwords.Admin = Read-HiddenPassword "Wachtwoord voor herkenbaar lokaal Admin-account"

    foreach ($role in @("Gast", "Editor", "Admin")) {
        $sessions[$role] = New-WebSession
        Write-Host "Lokaal demo-account: rol=$role; e-mail=$($emails[$role])"
    }

    Write-Host "Screenshot 1: openbare lijst en anonieme weigering"
    $result = Invoke-DemoRequest "1/7" "Anoniem" "GET" "/api/projects" $null $null $sessions.Gast
    Assert-DemoStatus $result 200
    $result = Invoke-DemoRequest "1/7" "Anoniem" "POST" "/api/projects" @{
        titel = "$marker anonymous"
        beschrijving = "Eigen AuthDemo-record."
        categorie = "AuthDemo"
        datum = "2026-11-23T12:00:00"
    } $null $sessions.Gast
    Assert-DemoStatus $result 401
    Wait-DemoScreenshot 1

    foreach ($role in @("Gast", "Editor", "Admin")) {
        Register-And-Login $role $sessions[$role]
    }
    Set-DemoRole $emails.Editor "Editor"
    Set-DemoRole $emails.Admin "Admin"

    Write-Host "Screenshot 2: Gast ziet GET maar kan niet schrijven"
    $guestToken = Get-DemoCsrfToken "Gast" $sessions.Gast
    $result = Invoke-DemoRequest "2/7" "Gast" "GET" "/api/projects" $null $null $sessions.Gast
    Assert-DemoStatus $result 200
    $result = Invoke-DemoRequest "2/7" "Gast" "POST" "/api/projects" @{
        titel = "$marker guest-denied"
        beschrijving = "Eigen AuthDemo-record."
        categorie = "AuthDemo"
        datum = "2026-11-23T12:00:00"
    } $guestToken $sessions.Gast
    Assert-DemoStatus $result 403
    Wait-DemoScreenshot 2

    Write-Host "Screenshot 3: Editor maakt eigen project aan"
    $editorToken = Get-DemoCsrfToken "Editor" $sessions.Editor
    $result = Invoke-DemoRequest "3/7" "Editor" "POST" "/api/projects" @{
        titel = "$marker project"
        beschrijving = "Herkenbaar tijdelijk AuthDemo-project."
        categorie = "AuthDemo"
        gitHubUrl = "https://example.test/auth-demo"
        datum = "2026-11-23T12:00:00"
    } $editorToken $sessions.Editor
    Assert-DemoStatus $result 201
    $projectId = [int]$result.Body.id
    $ownedProjects.Add($projectId)
    $result = Invoke-DemoRequest "3/7" "Editor" "GET" "/api/projects/$projectId" $null $null $sessions.Editor
    Assert-DemoStatus $result 200
    Wait-DemoScreenshot 3

    Write-Host "Screenshot 4: Admin maakt en wijzigt eigen blogpost"
    $adminToken = Get-DemoCsrfToken "Admin" $sessions.Admin
    $result = Invoke-DemoRequest "4/7" "Admin" "POST" "/api/blogposts" @{
        titel = "$marker blogpost"
        inhoud = "Herkenbare tijdelijke AuthDemo-blogpost."
        publicatiedatum = "2026-11-23T12:00:00"
    } $adminToken $sessions.Admin
    Assert-DemoStatus $result 201
    $blogpostId = [int]$result.Body.id
    $ownedBlogposts.Add($blogpostId)
    $result = Invoke-DemoRequest "4/7" "Admin" "PUT" "/api/blogposts/$blogpostId" @{
        titel = "$marker gewijzigd"
        inhoud = "Gewijzigde tijdelijke AuthDemo-blogpost."
        publicatiedatum = "2026-11-23T12:30:00"
    } $adminToken $sessions.Admin
    Assert-DemoStatus $result 204
    $result = Invoke-DemoRequest "4/7" "Admin" "GET" "/api/blogposts/$blogpostId" $null $null $sessions.Admin
    Assert-DemoStatus $result 200
    Wait-DemoScreenshot 4

    Write-Host "Screenshot 5: Admin verwijdert testblogpost; GET geeft 404"
    $result = Invoke-DemoRequest "5/7" "Admin" "DELETE" "/api/blogposts/$blogpostId" $null $adminToken $sessions.Admin
    Assert-DemoStatus $result 204
    [void]$ownedBlogposts.Remove($blogpostId)
    $result = Invoke-DemoRequest "5/7" "Admin" "GET" "/api/blogposts/$blogpostId" $null $null $sessions.Admin
    Assert-DemoStatus $result 404
    Wait-DemoScreenshot 5

    Write-Host "Screenshot 6: onbekende PUT en DELETE geven 404"
    $result = Invoke-DemoRequest "6/7" "Admin" "PUT" "/api/projects/2147483647" @{
        titel = "Onbekend"
        beschrijving = "Geldige body."
        categorie = "AuthDemo"
        datum = "2026-11-23T12:00:00"
    } $adminToken $sessions.Admin
    Assert-DemoStatus $result 404
    $result = Invoke-DemoRequest "6/7" "Admin" "DELETE" "/api/projects/2147483647" $null $adminToken $sessions.Admin
    Assert-DemoStatus $result 404
    Wait-DemoScreenshot 6

    Write-Host "Screenshot 7: role promotion/revocation invalidates existing cookies"
    Set-DemoRole $emails.Gast "Editor"
    $result = Invoke-DemoRequest "7/7" "Oude Gast-cookie" "POST" "/api/projects" @{
        titel = "$marker stale-guest"
        beschrijving = "Oude cookie moet ongeldig zijn."
        categorie = "AuthDemo"
        datum = "2026-11-23T12:00:00"
    } $guestToken $sessions.Gast
    Assert-DemoStatus $result 401
    $result = Invoke-DemoRequest "Fresh login" "Editor" "POST" "/api/auth/login" @{
        email = $emails.Gast
        password = $passwords.Gast
    } $null $sessions.Gast
    Assert-DemoStatus $result 200
    $guestEditorToken = Get-DemoCsrfToken "Editor" $sessions.Gast
    $result = Invoke-DemoRequest "7/7" "Editor na verse login" "POST" "/api/projects" @{
        titel = "$marker promoted"
        beschrijving = "Eigen AuthDemo-project."
        categorie = "AuthDemo"
        datum = "2026-11-23T12:00:00"
    } $guestEditorToken $sessions.Gast
    Assert-DemoStatus $result 201
    $ownedProjects.Add([int]$result.Body.id)

    Set-DemoRole $emails.Gast "Gast"
    $result = Invoke-DemoRequest "7/7" "Oude Editor-cookie" "POST" "/api/projects" @{
        titel = "$marker stale-editor"
        beschrijving = "Oude cookie moet ongeldig zijn."
        categorie = "AuthDemo"
        datum = "2026-11-23T12:00:00"
    } $guestEditorToken $sessions.Gast
    Assert-DemoStatus $result 401
    $result = Invoke-DemoRequest "Fresh login" "Gast" "POST" "/api/auth/login" @{
        email = $emails.Gast
        password = $passwords.Gast
    } $null $sessions.Gast
    Assert-DemoStatus $result 200
    $freshGuestToken = Get-DemoCsrfToken "Gast" $sessions.Gast
    $result = Invoke-DemoRequest "7/7" "Gast na verse login" "POST" "/api/projects" @{
        titel = "$marker demoted"
        beschrijving = "Eigen AuthDemo-project."
        categorie = "AuthDemo"
        datum = "2026-11-23T12:00:00"
    } $freshGuestToken $sessions.Gast
    Assert-DemoStatus $result 403
    Wait-DemoScreenshot 7
}
finally {
    if ($null -ne $sessions.Admin) {
        try {
            $cleanupToken = Get-DemoCsrfToken "Admin cleanup" $sessions.Admin
            foreach ($id in $ownedBlogposts) {
                [void](Invoke-DemoRequest "Cleanup" "Admin" "DELETE" "/api/blogposts/$id" $null $cleanupToken $sessions.Admin)
            }
            foreach ($id in $ownedProjects) {
                [void](Invoke-DemoRequest "Cleanup" "Admin" "DELETE" "/api/projects/$id" $null $cleanupToken $sessions.Admin)
            }
        }
        catch {
            Write-Warning "Cleanup was incomplete; remove records with the AuthDemo marker after reviewing them."
        }
    }

    foreach ($session in $sessions.Values) {
        $session.Client.Dispose()
        $session.Handler.Dispose()
    }
    $passwords.Clear()
}
