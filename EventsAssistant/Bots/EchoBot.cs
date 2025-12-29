// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.22.0

using AdaptiveCards;
using AdaptiveCards.Templating;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EventsAssistant.Bots
{
    public class EchoBot : ActivityHandler
    {

        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EchoBot> _logger;


        // Updated constructor
        public EchoBot(IConfiguration configuration, ILogger<EchoBot> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;

        }


        private async Task SendTypingIndicatorAsync(ITurnContext turnContext)
        {
            var typingActivity = Activity.CreateTypingActivity();
            await turnContext.SendActivityAsync(typingActivity);
            return;
        }
        private async Task<(string intent, Dictionary<string, List<string>> entities)> GetCLUResultAsync(string userInput)
        {
            _logger.LogInformation("Calling CLU for input: '{UserInput}'", userInput);

            var languageEndpoint = _configuration["AzureLanguage:Endpoint"];
            var projectName = _configuration["AzureLanguage:ProjectName"];
            var deploymentName = _configuration["AzureLanguage:DeploymentName"];

            if (string.IsNullOrEmpty(languageEndpoint) || string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(deploymentName))
            {
                _logger.LogError("CLU configuration missing. Check appsettings.json");
                return ("None", new Dictionary<string, List<string>>());
            }

            // Get Azure AD token for Cognitive Services using Managed Identity
            var credential = new DefaultAzureCredential();
            var tokenRequestContext = new TokenRequestContext(
                new[] { "https://cognitiveservices.azure.com/.default" }
            );
            var accessToken = await credential.GetTokenAsync(tokenRequestContext);

            // Prepare HTTP client with Bearer token
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);

            var url = $"{languageEndpoint}/language/:analyze-conversations?api-version=2022-10-01-preview";

            var requestData = new
            {
                kind = "Conversation",
                analysisInput = new
                {
                    conversationItem = new
                    {
                        id = Guid.NewGuid().ToString(),
                        participantId = "user",
                        modality = "text",
                        language = "en",
                        text = userInput
                    }
                },
                parameters = new
                {
                    projectName = projectName,
                    deploymentName = deploymentName,
                    verbose = true
                }
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(url, content);
                _logger.LogDebug("CLU response status: {StatusCode}", response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogError("CLU request failed: {StatusCode}, Body: {Body}", response.StatusCode, body);
                    return ("None", new Dictionary<string, List<string>>());
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(responseBody);

                string intent = result?.result?.prediction?.topIntent ?? "None";
                var entities = new Dictionary<string, List<string>>();

                var entitiesArray = result?.result?.prediction?.entities;
                if (entitiesArray != null)
                {
                    foreach (var entity in entitiesArray)
                    {
                        string name = entity.category;
                        var values = new List<string>();

                        if (entity.extraInformation != null)
                        {
                            foreach (var info in entity.extraInformation)
                            {
                                if (info.extraInformationKind == "ListKey")
                                {
                                    values.Add((string)info.key);
                                }
                            }
                        }
                        if (values.Count == 0 && !string.IsNullOrEmpty(entity.text))
                        {
                            values.Add(entity.text);
                        }

                        if (!entities.ContainsKey(name))
                            entities[name] = new List<string>();

                        entities[name].AddRange(values);
                    }
                }

                _logger.LogInformation("CLU returned intent: '{Intent}'", intent);
                return (intent, entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CLU call for input: '{UserInput}'", userInput);
                return ("None", new Dictionary<string, List<string>>());
            }
        }
        private async Task<bool> HandleCardActionAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var action = turnContext.Activity.Value;
                if (action is not JObject actionData)
                {
                    _logger.LogDebug("Activity.Value is not a JObject. Ignoring as non-card action.");
                    return false;
                }

                var id = actionData["id"]?.ToString();
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogDebug("Card action ID is missing or empty.");
                    return false;
                }

                _logger.LogInformation("Handling card button action: '{ActionId}'", id);

                switch (id)
                {
                    case "topics":
                        await SendTypingIndicatorAsync(turnContext);
                        await turnContext.SendActivityAsync(
                            MessageFactory.Text("Thank you for your request! Here are the available security and compliance programs."),
                            cancellationToken);
                        await ListAvailableTopics(turnContext, cancellationToken);
                        _logger.LogInformation("Displayed available programs to user.");
                        return true;

                    case "FeedbackYes":
                        await SendTypingIndicatorAsync(turnContext);
                        await turnContext.SendActivityAsync(
                            MessageFactory.Text("Thank you! For further assistance, please contact ESC - Platform Security & Compliance Support <escpscs@microsoft.com>"),
                            cancellationToken);
                        _logger.LogInformation("User provided positive feedback.");
                        return true;

                    case "FeedbackNo":
                        await SendTypingIndicatorAsync(turnContext);
                        await turnContext.SendActivityAsync(
                            MessageFactory.Text("Thank you for your feedback!"),
                            cancellationToken);
                        _logger.LogInformation("User provided negative feedback.");
                        return true;

                    default:
                        _logger.LogWarning("Unknown card action ID received: '{ActionId}'", id);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while handling card action (e.g., program, feedback)");
                // Send generic fallback message only if not already responded
                try
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("Sorry, I couldn't process your request right now. Please try again."),
                        cancellationToken);
                }
                catch
                {

                }
                return true; // Prevent further processing
            }
        }

        private async Task<bool> HandleDropdownActionAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var valueObj = turnContext.Activity.Value as JObject;
                if (valueObj == null)
                {
                    _logger.LogDebug("Dropdown action: Activity.Value is not a JObject.");
                    return false;
                }

                if (!valueObj.TryGetValue("action", out var actionVal))
                {
                    _logger.LogDebug("Dropdown action: Missing 'action' field in payload.");
                    return false;
                }

                var action = actionVal.ToString();
                _logger.LogInformation("Handling dropdown action: '{Action}'", action);

                switch (action)
                {
                    case "selectName":
                        var selectedName = valueObj["selectedName"]?.ToString()?.Trim();
                        if (string.IsNullOrWhiteSpace(selectedName))
                        {
                            _logger.LogWarning("Dropdown 'selectName': No KPI should be selected.");
                            await SendTypingIndicatorAsync(turnContext);
                            await turnContext.SendActivityAsync(
                                MessageFactory.Text("No selection was made. Please choose an option from the dropdown and proceed."),
                                cancellationToken);
                            return true;
                        }

                        _logger.LogInformation("Dropdown 'selectName': Selected '{SelectedName}'", selectedName);
                        await HandleNameSelection(selectedName, turnContext, cancellationToken);
                        return true;

                    case "selectSubtopic":
                        var selectedSubtopic = valueObj["selectedSubtopic"]?.ToString()?.Trim();
                        if (string.IsNullOrWhiteSpace(selectedSubtopic))
                        {
                            _logger.LogWarning("Dropdown 'selectSubtopic': No waves should be selected.");
                            await SendTypingIndicatorAsync(turnContext);
                            await turnContext.SendActivityAsync(
                                MessageFactory.Text("No selection was made. Please choose an option from the dropdown and proceed."),
                                cancellationToken);
                            return true;
                        }

                        _logger.LogInformation("Dropdown 'selectSubtopic': Searching content for '{SelectedSubtopic}'", selectedSubtopic);
                        await SearchContent(selectedSubtopic, turnContext, cancellationToken);
                        return true;

                    case "default":
                        _logger.LogWarning("Dropdown action: Received 'default' fallback for user ID '{UserId}'", turnContext.Activity.From.Id);
                        await SendTypingIndicatorAsync(turnContext);
                        await turnContext.SendActivityAsync(
                            MessageFactory.Text("I'm sorry, I didn't understand that. Please try again."),
                            cancellationToken);
                        return true;

                    default:
                        _logger.LogWarning("Unknown dropdown action received: '{Action}'", action);
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while handling dropdown action (selectName/selectSubtopic)");
                try
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("Sorry, something went wrong while processing your selection. Please try again."),
                        cancellationToken);
                }
                catch
                {
                    // Best effort – ignore send failure
                }
                return true; // Prevent further processing
            }
        }

        private string ExtractUserInput(IMessageActivity activity)
        {
            var userInput = activity.Text?.Trim() ?? "";

            if (activity.Entities != null)
            {
                foreach (var entity in activity.Entities.Where(e => e.Type == "mention"))
                {
                    var mention = entity.GetAs<Mention>();
                    if (mention.Mentioned.Id == activity.Recipient.Id)
                    {
                        userInput = userInput.Replace(mention.Text, "").Trim().TrimStart(',').Trim();
                    }
                }
            }

            return userInput;
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            var userId = turnContext.Activity.From.Id;

            if (await HandleCardActionAsync(turnContext, cancellationToken)) return;

            if (await HandleDropdownActionAsync(turnContext, cancellationToken)) return;

            var userInput = ExtractUserInput(turnContext.Activity);

            if (string.IsNullOrWhiteSpace(userInput))
            {
                await SendTypingIndicatorAsync(turnContext);
                await turnContext.SendActivityAsync("Hi there! You mentioned me but didn’t include a message. Let me know how I can help.");
                return;
            }

            _logger.LogInformation("User input: '{UserInput}'", userInput);
            var topics = await GetTopicsAsync();
            var matchedTopic = topics.FirstOrDefault(topic =>
                topic.Equals(userInput, StringComparison.OrdinalIgnoreCase));

            if (matchedTopic != null)
            {
                _logger.LogInformation("Matched program: '{MatchedTopic}'", matchedTopic);
                await SendTypingIndicatorAsync(turnContext);
                await ListAvailableSubtopics(matchedTopic, turnContext, cancellationToken);
                return;
            }

            // Call CLU
            var (intent, entities) = await GetCLUResultAsync(userInput);

            _logger.LogInformation("CLU returned intent: '{Intent}'", intent);

            foreach (var entity in entities)
            {
                _logger.LogInformation("Entity: {EntityName} = [{Values}]",
                    entity.Key, string.Join(", ", entity.Value));
            }

            if (string.IsNullOrEmpty(intent))
            {
                await turnContext.SendActivityAsync("I'm not sure what you're asking for. Please rephrase or choose from the available topics.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                var searchSuccess = await SearchContent(userInput, turnContext, cancellationToken);

                if (searchSuccess)
                {
                    return;
                }
            }

            if (intent == "content" || intent == "greeting")
            {
                await HandleCommonEntitiesAsync(entities, turnContext, cancellationToken);
                return;
            }



        }

        private async Task HandleCommonEntitiesAsync(Dictionary<string, List<string>> entities, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Processing common entities from CLU result.");

            if (entities == null)
            {
                _logger.LogWarning("Entities dictionary is null.");
                await SendFallbackMessage(turnContext, cancellationToken);
                return;
            }

            try
            {
                // Handle SearchQuery
                if (entities.TryGetValue("SearchQuery", out var kpis) && kpis?.Count > 0)
                {
                    _logger.LogInformation("Found {KpiCount} KPI(s) in 'SearchQuery': [{KpiList}]",
                        kpis.Count, string.Join(", ", kpis));

                    if (kpis.Count == 1)
                    {
                        await SearchContent(kpis[0], turnContext, cancellationToken);
                    }
                    else
                    {
                        await SendTypingIndicatorAsync(turnContext);
                        var card = CreateNameDropdownAdaptiveCard("Please select a KPI", kpis);
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
                    }
                    return;
                }

                // Handle subtopics
                if (entities.TryGetValue("subtopic", out var subtopics) && subtopics?.Count > 0)
                {
                    var subtopic = subtopics[0];
                    _logger.LogInformation("Routing to subtopic: '{Subtopic}'", subtopic);
                    await SearchContent(subtopic, turnContext, cancellationToken);
                    return;
                }

                // Handle Topics
                if (entities.TryGetValue("Topic", out var topics) && topics?.Count > 0)
                {
                    var topic = topics[0];
                    _logger.LogInformation("User requested topic details: '{Topic}'", topic);
                    await ListAvailableSubtopics(topic, turnContext, cancellationToken);
                    return;
                }

                // Handle greeting
                if (entities.TryGetValue("Greeting text", out var greetings) && greetings?.Count > 0)
                {
                    _logger.LogInformation("Responding to greeting from user.");
                    await SendTypingIndicatorAsync(turnContext);

                    string welcomeCardJson = System.IO.File.ReadAllText(Path.Combine(".", "welcomeCard.json"));
                    var cardAttachment = new Attachment
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(welcomeCardJson)
                    };

                    if (welcomeCardJson != null)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken);
                    }
                    else
                    {
                        // Fallback text if card fails
                        await turnContext.SendActivityAsync(
                            MessageFactory.Text("Hello! How can I assist you with security and compliance programs today?"),
                            cancellationToken);
                        await ListAvailableTopics(turnContext, cancellationToken);
                    }
                    return;
                }

                // No known entity matched
                _logger.LogWarning("No recognized entities found in CLU response.");
                await SendFallbackMessage(turnContext, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while handling common entities.");
                try
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("Sorry, I encountered an issue processing your request. Please try again."),
                        cancellationToken);
                }
                catch
                {

                }
            }
        }

        private async Task SendFallbackMessage(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var fallbackText = "I couldn't find relevant guidance in my knowledge base. Could you please provide a more specific rephrasing with Security & compliance programs like SFI, Azt, or specific KPI names?";
            await turnContext.SendActivityAsync(MessageFactory.Text(fallbackText), cancellationToken);
            return;
        }

        private async Task<string> GetOpenAiResponseAsync(string name)
        {
            try
            {
                _logger.LogInformation("Starting OpenAI response generation for KPI: {KPI}", name);

                // Configuration values
                var searchEndpoint = _configuration["OpenAISearchData:Endpoint"];
                var searchIndex = _configuration["OpenAISearchData:IndexName"];
                var openAiEndpoint = _configuration["AzureOpenAI:Endpoint"];
                var deploymentName = _configuration["AzureOpenAI:DeploymentName"];

                // Initialize Azure OpenAI client
                var credential = new DefaultAzureCredential();
                var azureClient = new AzureOpenAIClient(new Uri(openAiEndpoint), credential);
                var chatClient = azureClient.GetChatClient(deploymentName);

                // Use the SDK's DataSource classes to define your search configuration
#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                AzureSearchChatDataSource azureSearchDataSource = new()
                {
                    Endpoint = new Uri(searchEndpoint),
                    IndexName = searchIndex,
                    Authentication = DataSourceAuthentication.FromSystemManagedIdentity(),
                    // The SDK handles mapping and other parameters for you
                };
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                // chat completion options and the system prompt. The "role_information" you had is the system prompt.

                string systemPrompt = @"
                 You are an expert SFI Remediation Assistant for a company's internal security team. Your sole purpose is to provide clear, actionable information to help users understand and fix security compliance issues related to Key Performance Indicators (KPIs). Your knowledge is strictly limited to the provided documentation.
                ### Instructions
                1.  **Strictly Adhere to Information:** Use only the content provided from the documents. Do not invent, speculate, or infer any information.
                2.  **Summary Generation:** When a user asks about a KPI, generate a concise, 2-3 sentence summary that explains what the KPI is and its importance. Ensure this summary is easy to understand.
                3.  **Remediation Steps:** Following the summary, provide the remediation steps that come from source document's 'answer' field. Use a numbered or bulleted list for clarity.
                4.  **Tone:** Maintain a professional, knowledgeable, and helpful tone throughout the entire response.
                5.  **Handling Ambiguity:** If the requested KPI is not found in the documents, state clearly that you could not find a match. Do not guess or offer general advice. Instead, politely ask the user to provide a different or more specific KPI name.
                6.  **Formatting:** Structure the response to be easily readable. Do not include any document citations or references (e.g., `[doc1]`).";

                ChatCompletionOptions completionOptions = new ChatCompletionOptions()
                {
                    Temperature = 0.7f,
                    MaxOutputTokenCount = 800,
                    TopP = 1.0f,
                    FrequencyPenalty = 0.0f,
                    PresencePenalty = 0.0f,

                };
#pragma warning disable AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                completionOptions.AddDataSource(azureSearchDataSource);
#pragma warning restore AOAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                // Create the chat message list with your system and user messages
                List<ChatMessage> messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemPrompt),
                    new UserChatMessage($"Tell me step by step remediation on kpi : {name}")
                };


                // Calling CompleteChat method and process the response
                _logger.LogInformation("Sending request to Azure OpenAI for KPI: {KPI}", name);
                ChatCompletion completion = chatClient.CompleteChat(messages, completionOptions);

                // This is how you access the content and potential citations
                _logger.LogInformation("Received response from Azure OpenAI for KPI: {KPI}", name);
                var responseContent = completion.Content[0].Text;
                return responseContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating OpenAI response for KPI: {KPI}", name);
                return $"An error occurred while processing your request: {ex.Message}";
            }

        }


        private async Task<bool> SearchContent(string userInput, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            _logger.LogInformation("Starting search for query: '{UserInput}'", userInput);

            if (string.IsNullOrWhiteSpace(userInput))
            {
                _logger.LogWarning("SearchContent called with empty or null input.");
                await SendFallbackMessage(turnContext, cancellationToken);
                return true;
            }

            try
            {
                // Load configuration
                var endpoint = _configuration["AzureSearch:Endpoint"];
                var indexName = _configuration["AzureSearch:IndexName"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(indexName))
                {
                    _logger.LogError("Azure Search configuration is missing. Check appsettings.json");
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("I can't search right now because the Azure Search service is misconfigured. Please contact the support team."),
                        cancellationToken);
                    return true;
                }

                // Create search client
                var searchClient = new SearchClient(new Uri(endpoint), indexName, new DefaultAzureCredential());
                var searchOptions = new SearchOptions
                {
                    QueryType = SearchQueryType.Full,
                    IncludeTotalCount = true,
                    SearchMode = SearchMode.All,
                    SearchFields = { "subtopic", "name", "description" },
                    Select = { "answer", "name", "subtopic" },
                    Size = 100,
                    ScoringProfile = "default"
                };

                _logger.LogDebug("Executing Azure Cognitive Search query: '{UserInput}'", userInput);

                var searchResults = await searchClient.SearchAsync<SearchDocument>(userInput, searchOptions);

                _logger.LogDebug("Search returned {TotalCount} results", searchResults.Value.TotalCount);

                if (searchResults.Value.TotalCount == 0)
                {
                    _logger.LogInformation("No search results found for query: '{UserInput}'", userInput);
                    return false;
                }

                var results = searchResults.Value.GetResults().ToList();
                var namesList = new List<string>();
                string subtopicAnswer = string.Empty;

                foreach (var result in results)
                {
                    if (result.Document.TryGetValue("name", out var name) && !string.IsNullOrEmpty(name?.ToString()))
                    {
                        namesList.Add(name.ToString());
                    }
                    if (result.Document.TryGetValue("answer", out var content))
                    {
                        subtopicAnswer = content.ToString();
                    }
                }

                // Remove duplicates
                namesList = namesList.Distinct().ToList();

                if (namesList.Count == 1)
                {
                    var selectedName = namesList.First();
                    _logger.LogInformation("Single match found: '{SelectedName}'", selectedName);
                    await HandleNameSelection(selectedName, turnContext, cancellationToken);
                    return true;
                }
                else if (namesList.Count > 0)
                {
                    _logger.LogInformation("Multiple matches found ({Count}) for query: '{UserInput}'", namesList.Count, userInput);
                    await SendTypingIndicatorAsync(turnContext);
                    var card = CreateNameDropdownAdaptiveCard(userInput, namesList);
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);
                    return true;
                }
                else
                {
                    // This is rare — we had results but no names
                    _logger.LogWarning("Search returned results but no valid 'name' field found for query: '{UserInput}'", userInput);
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("I found some information, but it's not available in a format I can display. Please try a different term."),
                        cancellationToken);
                    return true;
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 401)
            {
                _logger.LogError(ex, "Azure Search authentication failed. Check search key credentials.");
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I can't access the knowledge base right now due to a security issue. Please contact support."),
                    cancellationToken);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogError(ex, "Azure Search index not found. Check index name: '{IndexName}'", _configuration["AzureSearch:IndexName"]);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I can't find the knowledge base right now. Please try again later."),
                    cancellationToken);
                return true;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search request failed with status {StatusCode}", ex.Status);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I encountered an issue retrieving information from the knowledge base. Please try again in a few moments."),
                    cancellationToken);
                return true;
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger.LogError(timeoutEx, "Search request timed out for query: '{UserInput}'", userInput);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("The search took too long. Please try a simpler query or try again."),
                    cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during search for input: '{UserInput}'", userInput);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I encountered an unexpected issue. Please try again or contact support."),
                    cancellationToken);
                return true;
            }
        }

        private async Task<bool> HandleNameSelection(string selectedName, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            _logger.LogInformation("Starting detailed Kpi search for name: '{SelectedName}'", selectedName);

            if (string.IsNullOrWhiteSpace(selectedName))
            {
                _logger.LogWarning("HandleNameSelection called with null or empty name.");
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("No KPI was selected. Please choose a valid option."),
                    cancellationToken);
                return true;
            }

            try
            {
                // Load config
                var endpoint = _configuration["AzureSearch:Endpoint"];
                var indexName = _configuration["AzureSearch:IndexName"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(indexName))
                {
                    _logger.LogError("Azure Search configuration is missing. Check appsettings.json");
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("I can't retrieve details right now due to a configuration issue. Please contact support."),
                        cancellationToken);
                    return true;
                }

                var searchClient = new SearchClient(new Uri(endpoint), indexName, new DefaultAzureCredential());

                var searchOptions = new SearchOptions
                {
                    IncludeTotalCount = true,
                    QueryType = SearchQueryType.Full,
                    SearchMode = SearchMode.All,
                    SearchFields = { "name", "description" },
                    Select = { "name", "description", "subtopic", "control_scope", "answer" },
                    Size = 100,
                    ScoringProfile = "default"
                };

                _logger.LogDebug("Passing the KPI name to the OpenAI summary function: '{SelectedName}'", selectedName);
                var OpenAIResponse = await GetOpenAiResponseAsync(selectedName);


                _logger.LogDebug("Executing search for name: '{SelectedName}'", selectedName);
                var searchResults = await searchClient.SearchAsync<SearchDocument>(selectedName, searchOptions);

                if (searchResults.Value.TotalCount == 0)
                {
                    _logger.LogInformation("No results found for name: '{SelectedName}'", selectedName);
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("I couldn’t find any information for the selected KPI. Please try another or rephrase your query."),
                        cancellationToken);
                    return true;
                }

                var results = searchResults.Value.GetResults().ToList();
                _logger.LogInformation("Found {ResultCount} result(s) for name: '{SelectedName}'", results.Count, selectedName);

                var result = results.First();
                var doc = result.Document;

                // Extract fields safely
                string name = GetDocumentValue(doc, "name");
                string subtopic = GetDocumentValue(doc, "subtopic");
                string controlScope = GetDocumentValue(doc, "control_scope");
                string answer = GetDocumentValue(doc, "answer");

                // Parse answer into text and links
                var (answerText, linksList) = ParseAnswer(answer);

                // Show typing indicator before sending card
                await SendTypingIndicatorAsync(turnContext);

                // Create and send detail card
                var card = CreateDetailAdaptiveCard(name, OpenAIResponse, subtopic, controlScope, linksList, answerText);
                await turnContext.SendActivityAsync(MessageFactory.Attachment(card), cancellationToken);

                // Ask for feedback
                await AskForFeedback(turnContext, cancellationToken);

                _logger.LogInformation("Successfully displayed details for KPI: '{Name}'", name);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 401)
            {
                _logger.LogError(ex, "Authentication failed when retrieving details for: '{SelectedName}'", selectedName);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I can't access the data due to a security issue. Please contact support."),
                    cancellationToken);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogError(ex, "Search index not found for name: '{SelectedName}'", selectedName);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("The requested information is not available right now. Please try again later."),
                    cancellationToken);
                return true;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search request failed for KPI: '{SelectedName}'", selectedName);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I encountered an issue retrieving information. Please try again in a few moments."),
                    cancellationToken);
                return true;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving details for KPI: '{SelectedName}'", selectedName);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("An unexpected error occurred. Please try again or contact support."),
                    cancellationToken);
                return true;
            }
        }

        //helper method to avoid NullReferenceException
        private string GetDocumentValue(SearchDocument document, string key)
        {
            return document.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
        }

        private Attachment CreateNameDropdownAdaptiveCard(string query, List<string> namesList)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Query string is null or empty.");
                return null;
            }

            if (namesList == null || namesList.Count == 0)
            {
                _logger.LogWarning("Names list is null or empty.");
                return null;
            }

            try
            {
                string jsonPath = Path.Combine(".", "dropdown.json");
                if (!File.Exists(jsonPath))
                {
                    _logger.LogError("Adaptive card JSON file not found at path: {Path}", jsonPath);
                    return null;
                }

                string dropdownCardJson = File.ReadAllText(jsonPath);
                JObject cardContent = JObject.Parse(dropdownCardJson);

                // Replace text in TextBlock
                var textBlock = cardContent["body"]?
                    .FirstOrDefault(e => e["type"]?.ToString() == "TextBlock" &&
                                         e["text"]?.ToString()?.Contains("${query}") == true);

                if (textBlock != null)
                {
                    textBlock["text"] = textBlock["text"]?.ToString()?.Replace("${query}", query);
                    _logger.LogInformation("Text block updated with query: {Query}", query);
                }

                // Update dropdown choices
                var columnSet = cardContent["body"]?
                    .FirstOrDefault(e => e["type"]?.ToString() == "ColumnSet");

                var columns = columnSet?["columns"] as JArray;
                var itemsInFirstColumn = columns?[0]?["items"] as JArray;

                var choiceSet = itemsInFirstColumn?
                    .FirstOrDefault(i => i["id"]?.ToString() == "selectedName");

                if (choiceSet?["choices"] is JArray choicesArray)
                {
                    choicesArray.Clear();

                    foreach (var name in namesList.Distinct())
                    {
                        choicesArray.Add(new JObject
                        {
                            ["title"] = name,
                            ["value"] = name
                        });
                    }

                    _logger.LogInformation("Dropdown choices populated with {Count} names.", namesList.Count);
                }
                else
                {
                    _logger.LogWarning("Could not find 'KPI' choiceSet in the adaptive card JSON.");
                }

                return new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = cardContent
                };
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating dropdown card for query: '{Query}'", query);
                return MessageFactory.Text("An error occurred while loading KPIs. Please try again.").Attachments.FirstOrDefault();
            }
        }

        private Attachment CreateDetailAdaptiveCard(string name, string description, string subtopic, string controlScope, List<(string anchorText, string url)> linksList, string answerText = null)
        {
            try
            {
                // Load the card template
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "detailedCard.json");
                if (!File.Exists(filePath))
                {
                    _logger?.LogError("Adaptive card template not found: {FilePath}", filePath);
                    return MessageFactory.Text("Card template not found.").Attachments.FirstOrDefault();
                }

                var templateJson = File.ReadAllText(filePath);

                // Define the data to fill into the template
                var cardData = new
                {
                    name = name ?? "Not Found",
                    description = description ?? "No description available.",
                    subtopic = subtopic ?? "Unknown",
                    controlScope = controlScope ?? "Not specified",
                    answerText = answerText ?? ""
                };

                // Apply data to template
                var template = new AdaptiveCardTemplate(templateJson);
                var cardJson = template.Expand(cardData);

                // Parse final JSON into JObject
                var card = JsonConvert.DeserializeObject<JObject>(cardJson);

                // Add action buttons (References) dynamically
                var actions = new JArray();

                foreach (var (anchorText, url) in linksList.Distinct())
                {
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        actions.Add(new JObject
                        {
                            ["type"] = "Action.OpenUrl",
                            ["title"] = anchorText,
                            ["url"] = url
                        });
                    }
                }

                if (actions.Count > 0)
                {
                    card["actions"] = actions;
                }

                // Return as attachment
                return new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = card
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create detail adaptive card for KPI: '{Name}'", name);
                return MessageFactory.Text("An error occurred while displaying the KPI details. Please try again.").Attachments.FirstOrDefault();
            }
        }

        private (string answerText, List<(string anchorText, string url)> links) ParseAnswer(string answer)
        {
            // Handle null/empty input
            if (string.IsNullOrWhiteSpace(answer))
                return (string.Empty, new List<(string, string)>());

            // Fix smart quotes (e.g., “ or ”)
            answer = answer.Replace("“", "\"").Replace("”", "\"").Replace("’", "'");

            var links = new List<(string anchorText, string url)>();

            // Extract <a href="...">link text</a>
            var anchorRegex = new Regex(@"<a\s+href=""(https?://[^\s""]+)""[^>]*>(.*?)</a>", RegexOptions.IgnoreCase);
            var anchorMatches = anchorRegex.Matches(answer);

            foreach (Match match in anchorMatches)
            {
                string url = match.Groups[1].Value;
                string anchorText = WebUtility.HtmlDecode(match.Groups[2].Value.Trim());

                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    links.Add((anchorText, url));
                }
            }

            // Remove all anchor tags
            string textWithoutAnchors = anchorRegex.Replace(answer, "").Trim();

            //Extract plain URLs (https://...)
            var urlRegex = new Regex(@"https?://[^\s""<>{}[\]`|~^,\.\s]+[^\s""<>{}[\]`|~^,\s]", RegexOptions.IgnoreCase);
            var urlMatches = urlRegex.Matches(textWithoutAnchors);

            foreach (Match match in urlMatches)
            {
                string url = match.Value;

                // Clean trailing punctuation
                url = url.TrimEnd('.', ',', ';', ':', ')', ']', '}', '!', '?');

                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    links.Add((url, url)); // Use URL as both text and link
                }
            }

            // Remove plain URLs from text
            string textWithoutLinks = urlRegex.Replace(textWithoutAnchors, "").Trim();

            //Handle escaped newlines (\n → actual newline)
            textWithoutLinks = textWithoutLinks.Replace("\\n", "\n");

            //Clean up extra whitespace
            textWithoutLinks = Regex.Replace(textWithoutLinks, @"\s+", " ").Trim();

            return (textWithoutLinks, links);
        }

        private async Task ListAvailableTopics(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Displaying available programs to user");

            var topics = await GetTopicsAsync();

            if (!topics.Any())
            {
                _logger.LogWarning("No progarms found in knowledge base");
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("Sorry, no progarms are currently available."),
                    cancellationToken);
                return;
            }

            try
            {
                var filePath = Path.Combine(".", "topics.json");
                if (!File.Exists(filePath))
                {
                    _logger.LogError("Card template not found: {FilePath}", filePath);
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text("I couldn't load the progarm list. Please try again later."),
                        cancellationToken);
                    return;
                }

                var cardTemplate = await File.ReadAllTextAsync(filePath);
                var cardData = new
                {
                    topics = topics.Select(t => new { title = t, value = t }).ToList()
                };

                var cardJson = new AdaptiveCardTemplate(cardTemplate).Expand(cardData);
                var cardAttachment = new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(cardJson)
                };

                await turnContext.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken);
                _logger.LogInformation("Sent progarms card");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send progarms card");
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("An error occurred while showing progarms. Please try again."),
                    cancellationToken);
            }
        }

        private async Task AskForFeedback(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Asking user for feedback");

            try
            {
                var filePath = Path.Combine(".", "feedback.json");
                var cardTemplate = await File.ReadAllTextAsync(filePath);
                var cardJson = new AdaptiveCardTemplate(cardTemplate).Expand(new { });

                var cardAttachment = new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(cardJson)
                };

                await turnContext.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken);
                _logger.LogInformation("Feedback card sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send feedback card");
                // Optional: send fallback text
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("Was this information helpful? (Yes / No)"),
                    cancellationToken);
            }
        }

        private async Task<IEnumerable<string>> GetTopicsAsync()
        {

            _logger.LogInformation("Fetching list of available programs from Azure Search");

            try
            {
                var endpoint = _configuration["AzureSearch:Endpoint"];
                var indexName = _configuration["AzureSearch:IndexName"];

                var searchClient = new SearchClient(new Uri(endpoint), indexName, new DefaultAzureCredential());

                var searchOptions = new SearchOptions
                {
                    IncludeTotalCount = true,
                    QueryType = SearchQueryType.Full,
                    SearchMode = SearchMode.Any,
                    SearchFields = { "topic" },
                    Select = { "topic" },
                    Size = 100
                };

                var searchResults = await searchClient.SearchAsync<SearchDocument>("*", searchOptions);
                var topics = new HashSet<string>();

                foreach (var result in searchResults.Value.GetResults())
                {
                    if (result.Document.TryGetValue("topic", out var topic) && !string.IsNullOrEmpty(topic?.ToString()))
                    {
                        topics.Add(topic.ToString());
                    }
                }

                _logger.LogInformation("Found {TopicCount} unique programs", topics.Count);
                return topics;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search failed while fetching programs. Status: {Status}", ex.Status);
                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching programs");
                return Enumerable.Empty<string>();
            }
        }

        private async Task ListAvailableSubtopics(string userTopic, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var subtopics = await GetSubTopicsAsync(userTopic);

            if (!subtopics.Any())
            {
                _logger.LogWarning("No waves found for progarm: '{UserTopic}'", userTopic);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("Sorry, no waves are currently available for this progarm."),
                    cancellationToken);
                return;
            }

            try
            {
                var filePath = Path.Combine(".", "subtopics.json");
                var cardTemplate = await File.ReadAllTextAsync(filePath);

                var cardData = new
                {
                    topicName = userTopic,
                    subtopics = subtopics.Select(s => new { title = s, value = s }).ToList()
                };

                var cardJson = new AdaptiveCardTemplate(cardTemplate).Expand(cardData);
                var cardAttachment = new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(cardJson)
                };

                await turnContext.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken);
                _logger.LogInformation("Sent waves dropdown for progarm");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send  card for '{UserTopic}'", userTopic);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("An error occurred while showing waves for the progarm. Please try again."),
                    cancellationToken);
            }
        }

        private async Task<IEnumerable<string>> GetSubTopicsAsync(string userTopic)
        {

            _logger.LogInformation("Fetching waves for the program: '{UserTopic}'", userTopic);

            try
            {
                var endpoint = _configuration["AzureSearch:Endpoint"];
                var indexName = _configuration["AzureSearch:IndexName"];
                var searchClient = new SearchClient(new Uri(endpoint), indexName, new DefaultAzureCredential());

                var searchOptions = new SearchOptions
                {
                    IncludeTotalCount = true,
                    QueryType = SearchQueryType.Full,
                    SearchMode = SearchMode.All,
                    SearchFields = { "topic" },
                    Select = { "subtopic" },
                    Size = 100
                };

                var searchResults = await searchClient.SearchAsync<SearchDocument>(userTopic, searchOptions);
                var subtopics = new HashSet<string>();

                foreach (var result in searchResults.Value.GetResults())
                {
                    if (result.Document.TryGetValue("subtopic", out var subtopic) && !string.IsNullOrEmpty(subtopic?.ToString()))
                    {
                        subtopics.Add(subtopic.ToString());
                    }
                }

                _logger.LogInformation("Found {Count} waves for program '{UserTopic}'", subtopics.Count, userTopic);
                return subtopics;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search failed while fetching waves for '{UserTopic}'", userTopic);
                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching waves for '{UserTopic}'", userTopic);
                return Enumerable.Empty<string>();
            }
        }

        private async Task ListAvailableNames(string userTopic, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var names = await GetNamesAsync(userTopic);

            if (!names.Any())
            {
                _logger.LogWarning("No KPI names found under '{UserTopic}'", userTopic);
                return; // Silent return — could send message if needed
            }

            try
            {
                var filePath = Path.Combine(".", "names.json");
                var cardTemplate = await File.ReadAllTextAsync(filePath);

                var cardData = new
                {
                    cardTitle = userTopic,
                    names = names.Select(n => new { title = n, value = n }).ToList()
                };

                var cardJson = new AdaptiveCardTemplate(cardTemplate).Expand(cardData);
                var cardAttachment = new Attachment
                {
                    ContentType = "application/vnd.microsoft.card.adaptive",
                    Content = JsonConvert.DeserializeObject(cardJson)
                };

                await turnContext.SendActivityAsync(MessageFactory.Attachment(cardAttachment), cancellationToken);
                _logger.LogInformation("Sent KPI names card for respective waves");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render or send KPI names card for '{UserTopic}'", userTopic);
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("An error occurred while showing KPIs. Please try again."),
                    cancellationToken);
            }
        }

        private async Task<IEnumerable<string>> GetNamesAsync(string userTopic)
        {

            _logger.LogInformation("Fetching KPI names for subtopic: '{UserTopic}'", userTopic);

            try
            {
                var endpoint = _configuration["AzureSearch:Endpoint"];
                var indexName = _configuration["AzureSearch:IndexName"];
                var searchClient = new SearchClient(new Uri(endpoint), indexName, new DefaultAzureCredential());

                var searchOptions = new SearchOptions
                {
                    IncludeTotalCount = true,
                    QueryType = SearchQueryType.Full,
                    SearchMode = SearchMode.All,
                    SearchFields = { "subtopic" },
                    Select = { "name" },
                    Size = 100
                };

                var searchResults = await searchClient.SearchAsync<SearchDocument>(userTopic, searchOptions);
                var names = new HashSet<string>();

                foreach (var result in searchResults.Value.GetResults())
                {
                    if (result.Document.TryGetValue("name", out var name) && !string.IsNullOrEmpty(name?.ToString()))
                    {
                        names.Add(name.ToString());
                    }
                }

                _logger.LogInformation("Found {Count} KPI names under '{UserTopic}'", names.Count, userTopic);
                return names;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Azure Search failed while fetching KPI names for '{UserTopic}'", userTopic);
                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching KPI names for '{UserTopic}'", userTopic);
                return Enumerable.Empty<string>();
            }
        }
    }
}
