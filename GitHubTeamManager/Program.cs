using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GitHubTeamManager.Config;
using GitHubTeamManager.Services;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.json", optional: false)
              .AddJsonFile("appsettings.local.json", optional: true)
              .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Bind AppSettings to IOptions pattern
        services.Configure<GitHubOptions>(context.Configuration.GetSection(GitHubOptions.SectionName));

        // Register services
        services.AddTransient<SCIMService>();
        services.AddTransient<GitHubService>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

// Resolve services
var scimService = services.GetRequiredService<SCIMService>();
var githubService = services.GetRequiredService<GitHubService>();

Console.WriteLine("Fetching SCIM groups...");
var scimGroups = await scimService.GetGroupsAsync();

foreach (var group in scimGroups)
{
    Console.WriteLine($"Processing SCIM group: {group.DisplayName}");
    // Check if a corresponding GitHub team exists
    var existingTeam = await githubService.GetTeamByNameAsync(group.DisplayName);

    if (existingTeam == null)
    {
        Console.WriteLine($"Creating new GitHub team for group: {group.DisplayName}");
        var newTeam = await githubService.CreateTeamAsync(group.DisplayName, $"Team synced with IdP group {group.Id}");
        Console.WriteLine($"Created team: {newTeam.Name} (ID: {newTeam.Id})");

        existingTeam = newTeam;        
    }
    Console.WriteLine($"Linking team {existingTeam.Name} to IdP group {group.DisplayName}");
    await githubService.LinkTeamToGroupAsync(existingTeam.Id, group.Id);
    Console.WriteLine($"Successfully linked team {existingTeam.Name} to IdP group");
    
}

Console.WriteLine("Completed processing all SCIM groups.");