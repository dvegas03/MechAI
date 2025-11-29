using System;
using Whisper;
using System.Collections.Generic;

public class MockWhisperStream
{
    public event Action<WhisperResult> OnSegmentFinished;

    public void TriggerSegmentFinished(string resultText)
    {
        var segment = new WhisperSegment(0, resultText, 0, 100);
        var segments = new List<WhisperSegment> { segment };
        var mockResult = new WhisperResult(segments, 0);
        OnSegmentFinished?.Invoke(mockResult);
    }
}
