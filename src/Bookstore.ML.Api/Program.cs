using System.Diagnostics;
using Bookstore.ML.Api;
using Bookstore.ML.Model;
using Microsoft.Extensions.ML;
using Microsoft.ML;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string modelPath = builder.Configuration["Model:Path"] ?? throw new InvalidOperationException("Model:Path is not set");

// One pool per model; the file watcher reloads the model when the zip changes
builder.Services.AddPredictionEnginePool<DeliveryInput, DeliveryPrediction>()
    .FromFile(modelName: "delivery", filePath: modelPath, watchForChanges: true);
builder.Services.AddSingleton(new ModelCardCache(Path.ChangeExtension(modelPath, ".json")));
builder.Services.AddSingleton<PredictionMetrics>();

WebApplication app = builder.Build();

app.MapPost("/predict/delivery-time", (DeliveryRequest request, PredictionEnginePool<DeliveryInput, DeliveryPrediction> pool,
    ModelCardCache cards, PredictionMetrics metrics) =>
{
    long start = Stopwatch.GetTimestamp();
    DeliveryInput input = request.ToInput();
    float days = pool.Predict(modelName: "delivery", example: input).PredictedDays;
    ModelCard card = cards.Current;
    string? warning = card.OutOfRange(input);
    metrics.Record(Stopwatch.GetElapsedTime(start), card.Version, warning is not null);
    return new DeliveryResponse(MathF.Round(days, 2), card.Version, warning);
});

app.MapGet("/model", (ModelCardCache cards) => cards.Current);

if (app.Environment.IsDevelopment())
{
    // Do not do this: one PredictionEngine shared by every request (post 04). Kept only to measure the damage.
    MLContext ml = new();
    PredictionEngine<DeliveryInput, DeliveryPrediction> shared =
        ml.Model.CreatePredictionEngine<DeliveryInput, DeliveryPrediction>(ml.Model.Load(modelPath, out _));
    app.MapPost("/demo/shared-engine", (DeliveryRequest request, ModelCardCache cards) =>
        new DeliveryResponse(MathF.Round(shared.Predict(request.ToInput()).PredictedDays, 2), cards.Current.Version, null));
}

app.Run();
