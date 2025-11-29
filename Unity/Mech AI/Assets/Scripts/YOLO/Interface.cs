using System;
using Unity.Barracuda;
using UnityEngine;

public class YoloInference : IDisposable
{
    private readonly IWorker worker;

    public YoloInference(Config config)
    {
        var model = ModelLoader.Load(config.OnnxModel);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, model);
    }

    public Tensor PerformInference(Texture texture)
    {
        var inputTensor = new Tensor(texture, channels: 3);
        worker.Execute(inputTensor);
        var output = worker.CopyOutput();
        inputTensor.Dispose();
        return output;
    }

    public void Dispose()
    {
        worker?.Dispose();
    }
}
