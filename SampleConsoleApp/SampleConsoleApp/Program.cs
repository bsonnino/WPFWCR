using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Generative;

var readyState = LanguageModel.GetReadyState();
if (readyState is AIFeatureReadyState.NotSupportedOnCurrentSystem or AIFeatureReadyState.DisabledByUser)
{
    Console.WriteLine("Language model is not supported on this device");
    return;
}

if (readyState == AIFeatureReadyState.EnsureNeeded)
{
    await LanguageModel.EnsureReadyAsync();
}

using LanguageModel languageModel = await LanguageModel.CreateAsync();

string prompt = "What's the formula for glucose?";

var result = await languageModel.GenerateResponseAsync(prompt);

Console.WriteLine(result.Text);

Console.ReadLine();