using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using GitHubTeamManager.Config;
using GitHubTeamManager.Services;
using Octokit;

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
        services.AddTransient<GitHubService>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var services = scope.ServiceProvider;

// Resolve services
var githubService = services.GetRequiredService<GitHubService>();

Console.WriteLine("Fetching SCIM groups...");
var scimGroups = await githubService.GetGroupsAsync();

foreach (var group in scimGroups)
{
    Console.WriteLine($"Processing SCIM group: {group.group_name}");
    // Check if a corresponding GitHub team exists
    var existingTeam = await githubService.GetTeamByNameAsync(group.group_name);


    if (existingTeam == null)
    {
        Console.WriteLine($"Creating new GitHub team for group: {group.group_name}");
        var newTeam = await githubService.CreateTeamAsync(group.group_name, $"Team synced with IdP group {group.group_id}");
        Console.WriteLine($"Created team: {newTeam.Name} (ID: {newTeam.Id})");        
        Console.WriteLine($"Linking team {newTeam.Name} to IdP group {group.group_name}");
        await githubService.LinkTeamToGroupAsync(newTeam.Slug, group.group_id);
        Console.WriteLine($"Successfully linked team {newTeam.Name} to IdP group");        
    }    
}


Console.WriteLine("Completed processing all SCIM groups.");