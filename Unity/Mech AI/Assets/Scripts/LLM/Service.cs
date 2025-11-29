using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class LLMService
{
    private readonly string apiKey;
    private const string ApiUrl = "https://api.openai.com/v1/responses";
    private const string DefaultModel = "gpt-4o-mini";
    private const float DefaultTemperature = 0.3f;
    private const int DefaultMaxOutputTokens = 250;
    private const string FallbackResponse = "I’m unable to respond right now.";

    public LLMService()
    {
        apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogWarning("OPENAI_API_KEY environment variable not set. LLMService will not function.");
        }
    }

    public virtual async Task<OpenAIResponse> SendTextRequestAsync(string instructions, string input)
    {
        var requestData = new OpenAIRequest
        {
            model = DefaultModel,
            temperature = DefaultTemperature,
            max_output_tokens = DefaultMaxOutputTokens,
            instructions = instructions,
            input = input
        };
        return await SendRequestAsync(requestData);
    }

    public virtual async Task<OpenAIResponse> SendTextAndImageRequestAsync(string instructions, string userMessage, byte[] image)
    {
        var base64Image = Convert.ToBase64String(image);
        var imageUrl = $"data:image/png;base64,{base64Image}";

        var requestData = new OpenAIRequest
        {
            model = DefaultModel,
            temperature = DefaultTemperature,
            max_output_tokens = DefaultMaxOutputTokens,
            instructions = instructions,
            input = new List<ContentPart>
            {
                new ContentPart { type = "input_text", text = userMessage },
                new ContentPart { type = "image_url", image_url = new ImageUrl { url = imageUrl } }
            }
        };
        return await SendRequestAsync(requestData);
    }

    private async Task<OpenAIResponse> SendRequestAsync(OpenAIRequest requestData)
    {
        var json = JsonConvert.SerializeObject(requestData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        using var request = new UnityWebRequest(ApiUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

        var operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"LLM Request Error: {request.error}");
            return null;
        }

        var responseJson = request.downloadHandler.text;
        return JsonConvert.DeserializeObject<OpenAIResponse>(responseJson);
    }
}

[Serializable]
public class OpenAIRequest
{
    public string model;
    public string instructions;
    public object input;
    public float temperature;
    public int max_output_tokens;
}

[Serializable]
public class OpenAIResponse
{
    public List<ResponseItem> output;
}

[Serializable]
public class ResponseItem
{
    public string type;
    public string id;
    public string role;
    public List<ResponseContent> content;
}

[Serializable]
public class ResponseContent
{
    public string type;
    public string text;
}

[Serializable]
public class ContentPart
{
    public string type;
    public string text;
    public ImageUrl image_url;
}

[Serializable]
public class ImageUrl
{
    public string url;
}