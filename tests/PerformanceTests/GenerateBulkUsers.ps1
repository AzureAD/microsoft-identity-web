# To run this script make sure you execute the following first:
# Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
# Install-Module AzureAD -Scope CurrentUser

param(
    [Parameter(Mandatory=$true)] 
    $TenantId,
    [Parameter(Mandatory=$true, HelpMessage="Ex: domain.microsoft.com")] 
    $TenantDomain,
    [Parameter(Mandatory=$true)] 
    $Password,
    [Parameter(Mandatory=$true, HelpMessage="First user created will start with this suffix")] 
    $StartAtUserSuffix =1,
    [Parameter(Mandatory=$true, HelpMessage="Number of users to create")] 
    $UsersToCreate = 100,
    [Parameter(HelpMessage="Prefix for each username")] 
    $UsernamePrefix = "MIWTestUser"
    )

Write-Host "Creating ${UsersToCreate} users starting from user ${UsernamePrefix}${StartAtUserSuffix}"

Connect-AzureAD -TenantId $TenantId
Get-AzureADTenantDetail
$PasswordProfile = New-Object -TypeName Microsoft.Open.AzureAD.Model.PasswordProfile
$PasswordProfile.Password = $password
$PasswordProfile.ForceChangePasswordNextLogin = $false

$lastSuffix  = ($StartAtUserSuffix -as [int]) + ($UsersToCreate -as [int]) -as [int]
for ($i = $StartAtUserSuffix -as [int]; $i -lt ($lastSuffix -as [int]); $i++)
{
    $username = "${UsernamePrefix}${i}"
    $displayName = "${username}FName ${username}LName"
    $upn = "${Username}@${TenantDomain}"

    New-AzureADUser -UserPrincipalName $upn -MailNickName $username -DisplayName $displayName -PasswordProfile $PasswordProfile  -AccountEnabled $true
}

Get-AzureADUser -SearchString "MIWTestUser" | measure
