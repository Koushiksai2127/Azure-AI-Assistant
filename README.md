# Azure AI Assistant

[![license](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![build status](https://img.shields.io/badge/build-passing-brightgreen)]()

Professional, extensible AI Assistant built with the Bot Framework SDK (C#) that integrates with Azure AI services and is packaged for Microsoft Teams.

Table of Contents
- About
- Features
- Repository layout
- Prerequisites
- Local development
- Configuration
- Run locally (Bot Framework Emulator + ngrok)
- Configure Azure resources
- Deploy to Azure App Service
- Teams packaging & installation
- Testing & troubleshooting
- Security & best practices
- Contributing
- License
- Maintainer

About

This repository contains a C# Bot Framework-based AI Assistant designed to work in Microsoft Teams and to leverage Azure AI services such as Azure OpenAI and Azure Cognitive Services for conversational and retrieval-augmented experiences.

Features
- Conversational bot built with the Bot Framework SDK (C#)
- Integration with Azure OpenAI or Cognitive Services and optional Cognitive Search for RAG
- Teams-ready manifest and packaging guidance
- Local development with Bot Framework Emulator and ngrok
- Guidelines for Azure deployment and secure configuration

Repository layout
- EventsAssistant/                - (example project folder; replace with actual project name if different)
- src/                           - application source projects (C#)
- docs/                          - documentation and manifests
- .github/                       - GitHub Actions workflows (CI/CD)
- README.md                      - this file

Prerequisites
- .NET SDK (check project target; commonly .NET 7 or .NET 8): https://dotnet.microsoft.com/
- Azure subscription (for production and Azure AI services)
- Azure OpenAI access or Azure Cognitive Services account
- Azure CLI (optional but recommended): https://learn.microsoft.com/cli/azure/install-azure-cli
- ngrok (for local HTTPS tunneling): https://ngrok.com/
- Bot Framework Emulator (for local testing): https://github.com/microsoft/BotFramework-Emulator
- Git, and a code editor such as Visual Studio or VS Code

Local development
1. Clone the repository:
   git clone https://github.com/Koushiksai2127/Azure-AI-Assistant.git
   cd Azure-AI-Assistant

2. Open the solution or project in your IDE (Visual Studio / VS Code).

3. Restore and build:
   dotnet restore
   dotnet build

Configuration
The project uses configuration values for Azure AD, Bot credentials, and Azure AI services. Keep secrets out of source control.

Common configuration keys (appsettings.json or environment variables):
- AzureAd:TenantId
- AzureAd:ClientId (MicrosoftAppId)
- AzureAd:ClientSecret (MicrosoftAppPassword)
- Bot:MessagingEndpoint (https://<your-host>/api/messages)
- AzureOpenAI:Endpoint
- AzureOpenAI:Key
- AzureOpenAI:DeploymentName
- CognitiveSearch:Endpoint
- CognitiveSearch:Key

Example appsettings.json (skeleton)
{
  "AzureAd": {
    "TenantId": "<tenant-id>",
    "ClientId": "<app-id>",
    "ClientSecret": "<client-secret>"
  },
  "Bot": {
    "MessagingEndpoint": "https://<your-host>/api/messages"
  },
  "AzureOpenAI": {
    "Endpoint": "https://<your-resource>.openai.azure.com/",
    "Key": "<your-key>",
    "DeploymentName": "<deployment-name>"
  },
  "AllowedHosts": "*"
}

Tip: Use environment variables or Azure Key Vault for secrets. Example environment variables (Linux/macOS):
export AzureOpenAI__Endpoint="https://<your-resource>.openai.azure.com/"
export AzureOpenAI__Key="<your-key>"
export AzureOpenAI__DeploymentName="gpt-deployment"
export AzureAd__ClientId="<app-id>"
export AzureAd__ClientSecret="<app-secret>"
export AzureAd__TenantId="<tenant-id>"

Run locally (Bot Framework Emulator + ngrok)
1. Start ngrok to expose local port 3978 (or the port your app uses):
   ngrok http 3978

2. Copy the HTTPS forwarding URL from ngrok (e.g. https://abcd1234.ngrok.io) and set your bot messaging endpoint to:
   https://abcd1234.ngrok.io/api/messages

3. Start the bot locally from your IDE or via CLI:
   dotnet run --project <path-to-bot-csproj>

4. Open Bot Framework Emulator and connect using the endpoint above, with Microsoft App ID and Password (if configured).

Configure Azure resources (recommended)
- Create a resource group:
  az group create --name rg-azure-ai-assistant --location eastus

- App Service (host the bot):
  az appservice plan create --name ai-bot-plan --resource-group rg-azure-ai-assistant --sku S1
  az webapp create --resource-group rg-azure-ai-assistant --plan ai-bot-plan --name <your-webapp-name> --runtime "DOTNET|7.0"

- Azure OpenAI / Cognitive Services: create a resource from the Azure Portal. Note endpoint, key, and (for OpenAI) deployment name.

- (Optional) Azure Key Vault to store secrets:
  az keyvault create -n kv-azure-ai-assistant -g rg-azure-ai-assistant -l eastus

- Bot Channels Registration: configure messaging endpoint to https://<your-webapp>.azurewebsites.net/api/messages and set Microsoft App ID and Password from your Azure AD app registration.

Deploy to Azure App Service
1. Publish the bot:
   dotnet publish -c Release -o ./publish

2. Deploy using Azure CLI (zip deploy):
   az webapp deploy --resource-group rg-azure-ai-assistant --name <your-webapp-name> --src-path ./publish --type zip

3. Configure App Settings in the Azure Web App to set the same configuration values as your local appsettings (use Key Vault references for secrets when possible).

Teams packaging & installation
1. Create a Teams manifest (manifest.json) that references your botId (MicrosoftAppId) and your webapp domain.
2. Include two icons (outline and color). Recommended sizes: 32x32 (outline) and 192x192 (color).
3. Zip the manifest and icons into a package and upload via Teams Developer Portal or your tenant app catalog.

Sample manifest snippet
{
  "manifestVersion": "1.13",
  "version": "1.0.0",
  "id": "<GUID>",
  "packageName": "com.yourorg.azureaiassistant",
  "developer": { "name": "Your Org", "websiteUrl": "https://yourorg.example", "privacyUrl": "https://yourorg.example/privacy", "termsOfUseUrl": "https://yourorg.example/terms" },
  "name": { "short": "Azure AI Assistant", "full": "Azure AI Assistant for Teams" },
  "description": { "short": "AI assistant", "full": "AI assistant powered by Azure OpenAI and Bot Framework" },
  "icons": { "outline": "outline.png", "color": "color.png" },
  "bots": [
    {
      "botId": "<MicrosoftAppId>",
      "scopes": [ "personal", "team" ],
      "supportsFiles": false,
      "isNotificationOnly": false
    }
  ],
  "permissions": [ "identity", "messageTeamMembers" ],
  "validDomains": [ "<your-webapp-hostname>.azurewebsites.net" ]
}

Testing & troubleshooting
- 401/403: verify MicrosoftAppId and MicrosoftAppPassword and app registration consent.
- 404 on /api/messages: confirm correct route and that the app is running.
- Bot unreachable: check ngrok or App Service TLS and ensure HTTPS publicly accessible.
- Azure OpenAI errors: validate endpoint, key, and deployment name. Review quota and permissions.

Security & best practices
- Never commit secrets. Use Azure Key Vault or GitHub Actions secrets.
- Apply least privilege for resources and service principals.
- Monitor usage and costs for LLM calls.
- Validate and sanitize user inputs where applicable.

Contributing
Contributions welcome:
1. Fork the repository
2. Create a branch: git checkout -b feat/your-change
3. Make changes, add tests
4. Open a Pull Request describing your changes

License
This project is licensed under the MIT License. See LICENSE for details.

Maintainer
- GitHub: Koushiksai2127

If you want, I can also:
- Add a CI workflow for building and deploying to Azure
- Create a Teams manifest and example icons in the repo
- Inspect the repository files to tailor this README with exact project names and startup commands
