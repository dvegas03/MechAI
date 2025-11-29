using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class Test_LLMResponseParsing
{
    [Test]
    public void ExtractTextFromResponse_ParsesCorrectly()
    {
        var go = new GameObject();
        var controller = go.AddComponent<LLMController>();
        var methodInfo = typeof(LLMController).GetMethod("ExtractTextFromResponse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var response = new OpenAIResponse
        {
            output = new List<ResponseItem>
            {
                new ResponseItem
                {
                    type = "message",
                    content = new List<ResponseContent>
                    {
                        new ResponseContent { type = "output_text", text = "  This is the answer.  " }
                    }
                }
            }
        };

        var result = (string)methodInfo.Invoke(controller, new object[] { response });
        Assert.AreEqual("This is the answer.", result);
        Object.DestroyImmediate(go);
    }
}