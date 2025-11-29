using System.Collections.Generic;
using System.Threading.Tasks;

public class MockLLMService : LLMService
{
    public string LastInstructions { get; private set; }
    public object LastInput { get; private set; }
    public OpenAIResponse MockResponse;

    public MockLLMService() : base() { }

    public override async Task<OpenAIResponse> SendTextRequestAsync(string instructions, string input)
    {
        LastInstructions = instructions;
        LastInput = input;
        await Task.Yield();
        return MockResponse;
    }

    public override async Task<OpenAIResponse> SendTextAndImageRequestAsync(string instructions, string userMessage, byte[] image)
    {
        LastInstructions = instructions;
        LastInput = new { userMessage, image };
        await Task.Yield();
        return MockResponse;
    }
}