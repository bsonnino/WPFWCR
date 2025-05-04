using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Generative;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WPFSampleApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => InitializePhiSilica();
    }

    private LanguageModel languageModel;

    private async void InitializePhiSilica()
    {
        try
        {
            var readyState = LanguageModel.GetReadyState();
            if (readyState is AIFeatureReadyState.NotSupportedOnCurrentSystem or AIFeatureReadyState.DisabledByUser)
            {
                LoadingGrid.Visibility = Visibility.Collapsed;
                MessageBox.Show("Phi Silica not available on this system", "Error initializing language model", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }
            if (readyState == AIFeatureReadyState.EnsureNeeded)
            {
                StatusText.Text = "Installing Phi Silica";
                var installTask = LanguageModel.EnsureReadyAsync();

                installTask.Progress = (installResult, progress) => Dispatcher.BeginInvoke(() =>
                                {
                                    StatusText.Text = $"Progress: {progress * 100:F1}";
                                });

                var result = await installTask;
                Dispatcher.BeginInvoke(() => StatusText.Text = "Done: " + result.Status.ToString());
            }
            languageModel = await LanguageModel.CreateAsync();
            LoadingGrid.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            LoadingGrid.Visibility = Visibility.Collapsed;
            MessageBox.Show(ex.Message, "Error initializing language model", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AskButton_Click(object sender, RoutedEventArgs e)
    {
        DoRespond();
    }

    CancellationTokenSource? cts = null;

    private async void DoRespond()
    {
        if (languageModel == null || InputTextBox.Text.Length == 0)
        {
            return;
        }
        try
        {
            AskButton.Visibility = Visibility.Collapsed;
            StopBtn.Visibility = Visibility.Visible;
            var text = InputTextBox.Text;
            ResponseText.Text = string.Empty;

            cts = new CancellationTokenSource();
            var asyncOp = languageModel.GenerateResponseAsync(text);
            if (asyncOp != null)
            {
                asyncOp.Progress = (asyncInfo, delta) =>
                {
                    Dispatcher.BeginInvoke(() => ResponseText.Text += delta);
                    if (cts.IsCancellationRequested)
                    {
                        asyncOp?.Cancel();
                    }
                };
                var result = await asyncOp;
            }
        }
        catch (Exception ex)
        {
            ResponseText.Text +=
                Environment.NewLine + Environment.NewLine + (ex is TaskCanceledException ? 
                    "Request canceled" : $"Error in processing: {ex.ToString()}");
        }


        AskButton.Visibility = Visibility.Visible;
        StopBtn.Visibility = Visibility.Collapsed;

        cts?.Dispose();
        cts = null;
    }

    private void StopBtn_Click(object sender, RoutedEventArgs e)
    {
        cts?.Cancel();
    }

    private void InputTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && sender is TextBox textBox)
        {
            DoRespond();
        }
    }
}