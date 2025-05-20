# GitHub Team Manager

This application synchronizes IdP groups with GitHub Teams using the SCIM API. It performs two main functions:
1. Fetches all groups from the IdP using the SCIM endpoint
2. Creates corresponding GitHub Teams and links them to the IdP groups

## Prerequisites

- .NET 8.0 SDK or later
- GitHub Organization with SCIM provisioning enabled
- GitHub Personal Access Token with appropriate permissions
- SCIM Token for your IdP

## Configuration

Create an `appsettings.json` file in the project directory with the following structure:

```json
{
  "GitHubToken": "your-github-token",
  "GitHubOrganization": "your-organization-name",
  "SCIMToken": "your-scim-token",
  "SCIMBaseUrl": "your-scim-base-url"
}
```

Replace the placeholder values with your actual configuration:

- `GitHubToken`: A GitHub Personal Access Token with `admin:org` scope
- `GitHubOrganization`: Your GitHub organization name
- `SCIMToken`: The SCIM token provided by your IdP
- `SCIMBaseUrl`: The base URL for SCIM API endpoints

## Building the Application

```bash
dotnet build
```

## Running the Application

```bash
dotnet run
```

The application will:
1. Connect to your IdP's SCIM endpoint
2. Fetch all available groups
3. For each group:
   - Check if a corresponding GitHub Team exists
   - Create a new team if it doesn't exist
   - Link the team with the IdP group

## Notes

- The application assumes that the SCIM group names should match the GitHub team names
- Teams are created with "Closed" privacy settings by default
- The application logs its progress to the console
