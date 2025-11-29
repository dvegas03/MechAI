using Unity.Barracuda;
using System;
using System.Collections;
using System.Collections.Generic;

public class MockBarracudaWorker : IWorker
{
    private Tensor _mockOutput;

    public MockBarracudaWorker(Tensor mockOutput)
    {
        _mockOutput = mockOutput;
    }

    public void Dispose() { _mockOutput?.Dispose(); }
    
    public IWorker Execute(Tensor input) { return this; }
    
    public IWorker Execute(IDictionary<string, Tensor> inputs) { return this; }
    
    public IWorker Execute() { return this; }
    
    public Tensor PeekOutput(string name) => _mockOutput;
    
    public Tensor PeekOutput() => _mockOutput;
    
    public Tensor[] PeekConstants(string name) => new Tensor[0];

    #region IWorker Members
    
    public Model model => null;
    
    public IEnumerator StartManualSchedule() { yield break; }
    
    public IEnumerator StartManualSchedule(IDictionary<string, Tensor> inputs) { yield break; }
    
    public IEnumerator StartManualSchedule(Tensor input) { yield break; }

    public void SetInput(Tensor t) { }

    public void SetInput(string name, Tensor t) { }

    public void FlushSchedule(bool blocking = true) { }
    
    public string[] GetOutputNames() => new[] { "output" };
    
    public void PrepareForInput(IDictionary<string, TensorShape> inputShapes) { }
    
    public void PrepareForInput(IDictionary<string, TensorShape> inputShapes, DataType dataType) { }
    
    public float scheduleProgress => 1.0f;
    
    public string Summary() => "MockBarracudaWorker";

    #endregion
}
