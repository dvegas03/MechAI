using System;
using UnityEngine;

public class MockMicrophoneRecord : MonoBehaviour
{
    public event Action<bool> OnVadChanged;

    public void TriggerVadChanged(bool isSpeech)
    {
        OnVadChanged?.Invoke(isSpeech);
    }
}