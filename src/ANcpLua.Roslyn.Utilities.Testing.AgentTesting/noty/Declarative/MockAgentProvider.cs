// Copyright (c) Microsoft. All rights reserved.
// Source: Microsoft.Agents.AI.Workflows.Declarative.UnitTests/MockAgentProvider.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Moq;

namespace Noty.Workflows.Tests;

// Moq<ResponseAgentProvider> with pre-canned messages and capture. Use it
// when the code under test talks to the full conversation stack and rolling a
// hand-fake would be heavier than reading from Moq.Verify.
internal sealed class MockAgentProvider : Mock<ResponseAgentProvider>
{
    public IList<string> ExistingConversationIds { get; } = [];

    public List<ChatMessage> TestMessages { get; set; } = [];

    public MockAgentProvider()
    {
        this.Setup(p => p.CreateConversationAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(this.CreateConversationId()));

        var testMessages = this.CreateMessages();

        this.Setup(p => p.GetMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(testMessages.First()));

        this.Setup(p => p.GetMessagesAsync(
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(testMessages));

        this.Setup(p => p.CreateMessageAsync(
                It.IsAny<string>(),
                It.IsAny<ChatMessage>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, ChatMessage, CancellationToken>((_, message, _) => Task.FromResult(this.Capture(message)));
    }

    private string CreateConversationId()
    {
        string id = Guid.NewGuid().ToString("N");
        this.ExistingConversationIds.Add(id);
        return id;
    }

    private ChatMessage Capture(ChatMessage message)
    {
        this.TestMessages.Add(message);
        return message;
    }

    private List<ChatMessage> CreateMessages()
    {
        const int MessageCount = 5;
        this.TestMessages = Enumerable.Range(1, MessageCount)
            .Select(i => new ChatMessage(ChatRole.User, $"Test message {i}") { MessageId = Guid.NewGuid().ToString("N") })
            .ToList();
        return this.TestMessages;
    }

    private static async IAsyncEnumerable<ChatMessage> ToAsyncEnumerable(IEnumerable<ChatMessage> messages)
    {
        foreach (var message in messages)
        {
            yield return message;
        }
        await Task.CompletedTask;
    }
}
