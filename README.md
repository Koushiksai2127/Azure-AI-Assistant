# Azure AI Assistant

[![Azure](https://img.shields.io/badge/Azure-Cloud-0078D4?logo=microsoft-azure&logoColor=white)](https://azure.microsoft.com/)
[![Language](https://img.shields.io/badge/C%23-Code-239120?logo=c-sharp&logoColor=white)](https://dotnet.microsoft.com/)
[![Bot Framework](https://img.shields.io/badge/Bot%20Framework-SDK_v4-purple?logo=robot&logoColor=white)](https://dev.botframework.com/)
[![AI](https://img.shields.io/badge/Azure_OpenAI-GPT_Models-00A3E0?logo=openai&logoColor=white)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
[![Search](https://img.shields.io/badge/Azure_AI_Search-Knowledge_Base-0078D4?logo=microsoft-azure&logoColor=white)](https://azure.microsoft.com/en-us/products/ai-services/ai-search)
[![CLU](https://img.shields.io/badge/Azure_Language-CLU-0078D4?logo=microsoft-azure&logoColor=white)](https://azure.microsoft.com/en-us/products/ai-services/language-service)
[![Teams](https://img.shields.io/badge/Microsoft_Teams-Integration-6264A7?logo=microsoft-teams&logoColor=white)](https://www.microsoft.com/en-us/microsoft-teams/group-chat-software)

## 📋 Table of Contents
- [📖 Overview](#-overview)
- [🏗️ Architecture](#-architecture)
- [🚀 Key Features](#-key-features)
- [🛠️ Tech Stack](#-tech-stack)
- [⚙️ Prerequisites](#-prerequisites)
- [🔧 Configuration & Setup](#-configuration--setup)
- [🚢 Deployment to Azure](#-deployment-to-azure)
- [🧪 Testing & Troubleshooting](#-testing--troubleshooting)
- [🔒 Security & Best Practices](#-security--best-practices)
- [🤝 Contributing](#-contributing)
- [🧠 Key Takeaways & Learnings](#-key-takeaways--learnings)
- [💭 Final Thoughts](#-final-thoughts)

---

## 📖 Overview

The **AI Assistant** is an intelligent conversational bot integrated with **Microsoft Teams**. It is designed to assist users in retrieving specific information regarding security protocols, compliance guidelines, and event safety standards.

By leveraging **Azure AI Services**, specifically Conversational Language Understanding (CLU) and Azure AI Search, the bot allows users to ask questions in natural language and retrieves precise answers from indexed documents stored in Azure Blob Storage.

---

## 🏗️ Architecture

The solution is built on the Microsoft Azure Cloud platform using the **Bot Framework SDK for .NET**, ensuring enterprise-grade scalability and strong typing.

![Architecture](https://github.com/Koushiksai2127/Azure-AI-Assistant/blob/main/Screenshots/Architecture.png)

### Data Flow
1.  **User Interaction:** The user sends a query via the **Microsoft Teams** interface.
2.  **Bot Processing:** The request is routed through **Azure Bot Services** to the C# backend hosted on **Azure App Services**.
3.  **Intent Recognition:** The application sends the user's text to the **Language Understanding (CLU)** model to extract intents and entities.
4.  **Search Execution:** Based on the identified intent, a query is executed against **Azure AI Search**.
5.  **Data Retrieval:** Azure AI Search queries the index created from documents (PDFs, Docx) stored in **Azure Storage Accounts**.
6.  **Response:** The relevant information is retrieved, processed, and sent back to the user on Teams.

---

## 🚀 Key Features

* **MS Teams Integration:** Seamless access within the organization's primary communication tool.
* **Natural Language Processing:** Uses Azure CLU to understand context, synonyms, and user intent.
* **Intelligent Search:** Retrieves specific sections from large compliance documents using Azure AI Search.
* **Scalable Backend:** Hosted on Azure App Service for high availability.

---

## 🛠️ Tech Stack

* **Language:** C# (.NET 6.0 / .NET 8.0)
* **Framework:** Microsoft Bot Framework SDK v4 (Echo Bot Template)
* **Interface:** Microsoft Teams
* **Cloud Services:**
    * Azure Bot Service
    * Azure App Service
    * Azure Language Service (CLU)
    * Azure AI Search
    * Azure Blob Storage
    * Azure OpenAI Service

---

## ⚙️ Prerequisites

Before you begin, you must download and install the following tools.

1.  **Framework:** **.NET SDK 8.0**
    * **Why:** Required to build and run the C# code.
    * **Download:** [Official .NET 8.0 Download Link](https://dotnet.microsoft.com/download/dotnet/8.0)
    * *Action:* Download the "SDK" version for your operating system (Windows, macOS, or Linux).

2.  **Code Editor:** **Visual Studio 2022** (Recommended) or **VS Code**
    * **Why:** This is where you will write and edit the code.
    * **Download:** [Visual Studio 2022 Community (Free)](https://visualstudio.microsoft.com/downloads/)
    * *Action:* During installation, check the box for **"ASP.NET and web development"**.

3.  **Testing Tool:** **Bot Framework Emulator (V4)**
    * **Why:** Allows you to chat with your bot locally on your computer before deploying it to Teams.
    * **Download:** [GitHub Releases Page](https://github.com/microsoft/BotFramework-Emulator/releases)
    * *Action:* Scroll to the "Assets" section of the latest release and download the file ending in `.exe` (for Windows) or `.dmg` (for Mac).

4.  **Cloud Account:** **Azure Subscription**
    * **Why:** You need this to create the AI and Storage resources.
    * **Link:** [Create Free Azure Account](https://azure.microsoft.com/free/)

5.  **Deployment Target:** **Microsoft Teams Tenant**
    * **Why:** To upload and test the app in a real Teams environment.
    * **Link:** [Join Microsoft 365 Developer Program](https://developer.microsoft.com/en-us/microsoft-365/dev-program)
    * *Tip:* If you don't have admin access to your company's Teams, use the link above to get a free "Sandbox" environment for testing.

---

## 🔧 Configuration & Setup

Follow these steps exactly to get the project running on your local machine.

### 1. Clone the Repository
Open your terminal (Command Prompt, PowerShell, or Git Bash) and run the following commands one by one:

```bash
# 1. Download the code to your computer
git clone [https://github.com/Koushiksai2127/Azure-AI-Assistant.git](https://github.com/Koushiksai2127/Azure-AI-Assistant.git)

# 2. Enter the project folder
cd Azure-AI-Assistant
```
---
### 2. Azure Resource Provisioning

You need to set up the Azure Resource Environment.

#### Step A: Storage & Search (The Knowledge Base)
1.  **Create a Storage Account:**
    * Go to Azure Portal > Create **Storage Account**.
    * Create a container named `documents` and **upload your PDF/Word files** there.
2.  **Create Azure AI Search:**
    * Create an **Azure AI Search** service (Free tier is fine for testing).
    * Use the **"Import Data"** wizard in the Search service overview.
    * Select your **Storage Account** → Parse the files → **Create an Index** (name it `events-index`).

#### Step B: Language Service (The Brain)
1.  **Create Language Service:**
    * Create a **Language Service** resource. Check the box for **"Custom Question Answering & Conversational Language Understanding"**.
2.  **Configure in Language Studio:**
    * Go to [Language Studio](https://language.cognitive.azure.com/) and sign in.
    * Create a new **Conversational Language Understanding** project.
    * Define your **Intents** (e.g., `program`, `wave`) and add sample utterances.
    * **Train** the model and **Deploy** it (name the deployment `Test`).

![NLP_Language_Service](https://github.com/Koushiksai2127/Azure-AI-Assistant/blob/main/Screenshots/CLU_Project.png)

#### Step C: Bot Registration (The Identity)
1.  **Create Azure Bot:**
    * Create an **Azure Bot** resource.
    * For **Identity Type**, select **"User-Assigned Managed Identity"**.
    * Once created, go to **Configuration** and copy the **Microsoft App ID**.
    * *Note:* You do **not** need to generate a client secret (password) because the Managed Identity handles authentication securely.
2.  **Assign to App Service:**
    * Go to your **Azure App Service** resource.
    * Navigate to **Settings** > **Identity**.
    * Switch to the **User Assigned** tab and click **Add**.
    * Select the Bot identity you created in step 1.

#### Step D: Azure OpenAI (Summary Engine)
1.  **Create Azure OpenAI Resource:**
    * Create an **Azure OpenAI** resource in the portal.
    * Go to **Model Deployments** and deploy a model (e.g., `gpt-35-turbo` or `gpt-4`).
    * Name this deployment `summary-model` (or similar).
    * *Note:* You will need the **Endpoint** and **Key** from this resource (or assign the same Managed Identity to it for keyless access).
  
### 3. Application Settings
Open the `appsettings.json` file in your project root. Copy the code below and populate the values with your specific Azure resource keys and endpoints.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "AllowedHosts": "*",

  // Bot Identity Configuration
  "MicrosoftAppType": "SingleTenant",
  "MicrosoftAppId": "<YOUR_BOT_APP_ID>",         
  "MicrosoftAppClientId": "<YOUR_BOT_CLIENT_ID>", 
  "MicrosoftAppTenantId": "<YOUR_TENANT_ID>",     

  // Azure AI Search (Standard Knowledge Base)
  "AzureSearch": {
    "Endpoint": "<YOUR_SEARCH_SERVICE_ENDPOINT>",
    "IndexName": "<YOUR_INDEX_NAME>"
  },

  // Azure Language Service (CLU for Intent Recognition)
  "AzureLanguage": {
    "Endpoint": "<YOUR_LANGUAGE_ENDPOINT>",
    "ProjectName": "<YOUR_CLU_PROJECT_NAME>",
    "DeploymentName": "<YOUR_CLU_DEPLOYMENT_NAME>"
  },

  // Azure OpenAI (Summarization & Generative Responses)
  "AzureOpenAI": {
    "Endpoint": "<YOUR_OPENAI_ENDPOINT>",
    "DeploymentName": "<YOUR_GPT_MODEL_NAME>"
  },

  // OpenAI Search Extension (If using OpenAI On Your Data)
  "OpenAISearchData": {
    "Endpoint": "<YOUR_SEARCH_SERVICE_ENDPOINT>",
    "IndexName": "<YOUR_INDEX_NAME>"
  }
}
```
---
## 🚢 Deployment to Azure

### 1. Publish the Bot Code
You can deploy directly from the command line using the Azure CLI.

**Prerequisite:** Ensure you are logged in to Azure (`az login`).

```bash
# 1. Publish the project to a local folder
dotnet publish -c Release -o ./publish

# 2. Compress the publish folder (optional, if deploying via Zip)
cd publish
zip -r ../deploy.zip .
cd ..

# 3. Deploy to Azure App Service
az webapp deployment source config-zip --resource-group <YOUR_RG_NAME> --name <YOUR_WEBAPP_NAME> --src deploy.zip
```
Alternatively, you can use the **Publish** feature directly inside Visual Studio (Right-click Project > Publish > Azure).

### 2. Configure the Teams Channel
   * Go to your Azure Bot resource in the Azure Portal.
   * Select Channels from the left menu.
   * Click on the Microsoft Teams logo.
   * Select **Microsoft Teams Commercial** and click Apply.

### 3. Create & Install the Teams App Package

To make your bot visible and interactive in Microsoft Teams, you must package it as a **custom Teams app**.

### 📋 Step-by-Step Instructions

1. **Locate the `manifest.json` file**  
   Find the `manifest.json` file in your project’s `deployment/teams/manifest` directory (or wherever your manifest is stored).

2. **Update App Identifiers**  
   Open `manifest.json` and replace the following fields with your **Microsoft App ID** (found in Azure Bot resource):
   ```json
   {
     "id": "<Microsoft_APP_ID>",
     "bots": [
       {
         "botId": "<Microsoft_APP_ID>"
       }
     ]
   }
   ```
3. **Prepare the App Package ZIP** <br>
    Compress these three files into a single **.zip** archive:
    * manifest.json - App configuration
    * color.png - Full-color icon (recommended: 192×192 px)
    * outline.png - Transparent outline icon (recommended: 32×32 px) <br>
    
💡 **Icon Tips:** Use PNG format with transparent backgrounds. Validate sizing using Teams App Studio.

4. **Upload to Microsoft Teams Developer Portal**
    * Go to the Microsoft Teams Developer Portal
    * Click Apps → Import app
    * Upload your .zip file

5. **Review the app details**
    * Install & Test
   * Click Preview in Teams to install privately for testing Or click Publish to distribute across your organization’s tenant

✅ **Best Practice:** Always test with **Preview in Teams** first before publishing to ensure functionality and UI appearance are correct.


#### please find the [`Teams_bot_response`](Screenshots/Teams_bot_response.png) here.

---
## 🧪 Testing & Troubleshooting

If you encounter issues while running the bot, check the following common scenarios:

### Common Issues
* **`401 Unauthorized` / `403 Forbidden`:**
    * Confirm that `MicrosoftAppId` and `MicrosoftAppPassword` in your configuration match exactly with your **App Registration** in Azure Entra ID.
    * If using Managed Identity, ensure the identity has been assigned the correct roles.
* **`404 Not Found` on `/api/messages`:**
    * Confirm your route names in `BotController.cs`.
    * Check your application path and ensure the base URL in the Azure Bot Service configuration matches your App Service URL (e.g., `https://your-bot.azurewebsites.net/api/messages`).
    * Check Web App logs (Log Stream) in the Azure Portal for startup errors.
* **Bot Unreachable (Local/Remote):**
    * **Locally:** Check if **ngrok** is running and forwarding to the correct local port (if testing against Teams from localhost).
    * **Azure:** Check App Service TLS/SSL configuration. Ensure the endpoint is publicly accessible via HTTPS.
* **Azure OpenAI Errors:**
    * Verify the **Endpoint** and **API Key**.
    * Ensure the `DeploymentName` in `appsettings.json` matches exactly with the model deployment name in Azure OpenAI Studio (e.g., `gpt-35-turbo` vs `my-gpt-deployment`).

### Debugging Tools
* **App Logs:** Check Console output or Azure App Service **Log Stream**.
* **Bot Framework Emulator:** Use the "Live Chat" log to see JSON payloads and error traces.
* **Application Insights:** View end-to-end transaction traces to identify where the latency or failure occurs.

---

## 🔒 Security & Best Practices

To ensure the safety and reliability of your application:

* **Secret Management:** Use **Managed Identities** or **Azure Key Vault**.
* **Least Privilege:** Assign only necessary permissions to your Azure resources and Managed Identities (e.g., "Cognitive Services User" instead of "Contributor").
* **Input Sanitization:** Sanitize user inputs to prevent injection attacks before passing them to backend services.
* **Cost Control:** Implement rate-limiting and monitor usage for Azure OpenAI to prevent unexpected costs.
* **Updates:** Keep your NuGet packages and .NET runtime up-to-date to patch security vulnerabilities.

---

## 🤝 Contributing

Contributions are welcome! Please follow this workflow to contribute:

1.  **Fork** the repository.
2.  Create a **feature branch**:
    ```bash
    git checkout -b feat/your-change
    ```
3.  Make your changes and add unit tests where appropriate.
4.  **Commit** your changes following the repository's coding conventions.
5.  Open a **Pull Request** describing your changes.

---
## 🧠 Key Takeaways & Learnings

Building this project provided deep insights into designing scalable AI architectures on Azure. Here are the critical challenges I overcame and the concepts I mastered:

### 🔐 Mastering Managed Identity & Security
One of the most critical implementations was securing the communication between the Bot, App Service, and AI resources without using hardcoded keys.
* **Challenge:** Configuring the Bot Framework to authenticate using **User-Assigned Managed Identity** instead of the traditional App ID/Password method.
* **Solution:** I learned to assign specific RBAC roles (like `Cognitive Services User` and `Search Index Data Reader`) to the Managed Identity, ensuring a "Zero Trust" security model where the application code never handles sensitive secrets.

### 🔎 Hybrid Search & RAG Pattern
Integrating **Azure AI Search** with **Azure OpenAI** gave me a practical understanding of the **Retrieval-Augmented Generation (RAG)** pattern.
* **Insight:** I learned that LLMs (like GPT-4) are powerful but limited by their training data. By connecting them to a real-time Search Index, I could "ground" the AI's responses in our specific compliance documents.
* **Optimization:** I fine-tuned the search queries to retrieve only the top 3 most relevant document chunks, reducing token usage and cost while improving accuracy.

### 🤖 Intelligent Architecture Design
Moving beyond simple scripts to a full **Bot Framework** solution taught me the importance of state management and asynchronous processing.
* **Flow Control:** I learned how to orchestrate the flow from `Teams User` -> `Bot Service` -> `CLU (Intent)` -> `App Logic` -> `Response`, ensuring a seamless user experience even when backend services take time to process complex queries.

---

## 💭 Final Thoughts

Building this **Azure AI Assistant** has significantly improved my practical skills in **Azure AI Services** and **Cloud Security**. It reflects my curiosity, persistence, and passion for developing real-world solutions that solve tangible business problems—transforming static documents into interactive, intelligent conversations.

*More Azure AI and Cognitive Service projects will be added soon.*

### Let's Connect 🤝

If you have suggestions, feedback, or opportunities, feel free to reach out! I'm always excited to learn, collaborate, and work on meaningful AI projects.
