# GitHub Team Manager

A C# console application that automates the process of creating and linking GitHub Teams with Enterprise SCIM Groups. This tool helps streamline team management by automatically creating GitHub teams that correspond to your Identity Provider (IdP) groups and establishing the necessary links between them.

## Features

- Fetches all groups from your enterprise's SCIM endpoint
- Creates corresponding GitHub teams for each SCIM group
- Links GitHub teams with their corresponding IdP groups using GitHub's external groups API
- Handles both new team creation and existing team linking
- Supports enterprise-wide SCIM integration

## Prerequisites

- .NET 8.0 SDK or later
- GitHub Enterprise account with SCIM provisioning enabled
- GitHub Personal Access Token with appropriate permissions
- SCIM Token for your IdP
- Organization admin access

## Configuration

Create an `appsettings.json` file in the application directory with the following structure:

```json
{
  "GitHubToken": "your-github-token",
  "EnterpriseSlug": "your-enterprise-slug",
  "GitHubOrganization": "your-organization-name",
  "SCIMToken": "your-scim-token",
  "SCIMBaseUrl": "https://api.github.com"
}
```

### Required Settings

- `GitHubToken`: A GitHub Personal Access Token with `admin:org` scope
- `EnterpriseSlug`: Your GitHub Enterprise slug (URL-friendly name)
- `GitHubOrganization`: The name of your GitHub organization
- `SCIMToken`: The SCIM token provided by your IdP
- `SCIMBaseUrl`: The base URL for GitHub's API (usually https://api.github.com)

## Building and Running

To build the application:
```bash
dotnet build
```

To run the application:
```bash
dotnet run
```

## How It Works

1. The application connects to GitHub's SCIM API to fetch all available enterprise groups
2. For each SCIM group:
   - Checks if a corresponding GitHub team exists
   - If no team exists, creates a new team with matching name
   - Links the team (new or existing) to the SCIM group using GitHub's external groups API
3. Provides detailed logging of all operations

## Error Handling

- Validates all API responses
- Provides detailed error messages for troubleshooting
- Ensures proper authentication for both GitHub and SCIM operations

## Notes

- Teams are created with "Closed" privacy settings by default
- Team names match the corresponding SCIM group display names
- Links are established using GitHub's external groups API
