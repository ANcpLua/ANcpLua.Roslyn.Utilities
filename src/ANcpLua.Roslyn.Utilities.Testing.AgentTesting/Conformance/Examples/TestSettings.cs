// Licensed to the .NET Foundation under one or more agreements.

namespace ANcpLua.Roslyn.Utilities.Testing.AgentTesting.Conformance.Examples;

/// <summary>
/// Flat configuration key catalog shared by every conformance fixture in this folder.
/// Populate via <c>testsettings.development.json</c>, environment variables, or user secrets
/// and resolve through <see cref="Support.TestConfiguration"/>.
/// </summary>
public static class TestSettings
{
    // OpenAI
    public const string OpenAIApiKey = "OpenAI:ApiKey";
    public const string OpenAIChatModelName = "OpenAI:ChatModelName";
    public const string OpenAIReasoningModelName = "OpenAI:ReasoningModelName";

    // Azure OpenAI
    public const string AzureOpenAIEndpoint = "AzureOpenAI:Endpoint";
    public const string AzureOpenAIApiKey = "AzureOpenAI:ApiKey";
    public const string AzureOpenAIChatDeploymentName = "AzureOpenAI:ChatDeploymentName";

    // Anthropic
    public const string AnthropicApiKey = "Anthropic:ApiKey";
    public const string AnthropicChatModelName = "Anthropic:ChatModelName";
    public const string AnthropicReasoningModelName = "Anthropic:ReasoningModelName";

    // Ollama
    public const string OllamaEndpoint = "Ollama:Endpoint";
    public const string OllamaChatModelName = "Ollama:ChatModelName";

    // Google Gemini
    public const string GoogleGeminiApiKey = "GoogleGemini:ApiKey";
    public const string GoogleGeminiChatModelName = "GoogleGemini:ChatModelName";

    // OpenRouter
    public const string OpenRouterApiKey = "OpenRouter:ApiKey";
    public const string OpenRouterChatModelName = "OpenRouter:ChatModelName";
    public const string OpenRouterBaseUrl = "OpenRouter:BaseUrl";
}
