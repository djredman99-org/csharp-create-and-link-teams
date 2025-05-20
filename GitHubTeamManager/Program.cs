using Microsoft.Extensions.Configuration;
using GitHubTeamManager.Config;
using GitHubTeamManager.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var settings = configuration.Get<AppSettings>()
    ?? throw new InvalidOperationException("Failed to load application settings");

var scimService = new SCIMService(settings.SCIMBaseUrl, settings.SCIMToken, settings.EnterpriseSlug);
var githubService = new GitHubService(settings.GitHubToken, settings.EnterpriseSlug, settings.SCIMToken);

Console.WriteLine("Fetching SCIM groups...");
var scimGroups = await scimService.GetGroupsAsync();

foreach (var group in scimGroups)
{
    Console.WriteLine($"Processing SCIM group: {group.DisplayName}");
      // Check if a corresponding GitHub team exists
    var existingTeam = await githubService.GetTeamByNameAsync(settings.GitHubOrganization, group.DisplayName);
    
    if (existingTeam == null)
    {
        Console.WriteLine($"Creating new GitHub team for group: {group.DisplayName}");
        var newTeam = await githubService.CreateTeamAsync(settings.GitHubOrganization, group.DisplayName, $"Team synced with IdP group {group.Id}");
        Console.WriteLine($"Created team: {newTeam.Name} (ID: {newTeam.Id})");
        
        // Link the team to the IdP group
        Console.WriteLine($"Linking team {newTeam.Name} to IdP group {group.DisplayName}");
        await githubService.LinkTeamToGroupAsync(newTeam.Id, group.Id);
        Console.WriteLine($"Successfully linked team {newTeam.Name} to IdP group");
    }
    else
    {
        Console.WriteLine($"Team already exists: {existingTeam.Name} (ID: {existingTeam.Id})");
        Console.WriteLine($"Linking existing team {existingTeam.Name} to IdP group {group.DisplayName}");
        await githubService.LinkTeamToGroupAsync(existingTeam.Id, group.Id);
        Console.WriteLine($"Successfully linked team {existingTeam.Name} to IdP group");
    }
}

Console.WriteLine("Completed processing all SCIM groups.");
