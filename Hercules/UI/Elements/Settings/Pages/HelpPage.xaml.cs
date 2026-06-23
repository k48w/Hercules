namespace Hercules.UI.Elements.Settings.Pages;

public partial class HelpPage
{
    public HelpPage()
    {
        InitializeComponent();
    }

    private void Expander_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
    {

    }

    private void ExportDiagnosticReport_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        string timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"Hercules-diagnostic-{timestamp}.json",
            Filter = "JSON report (*.json)|*.json"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            Integrations.DiagnosticReportService.WriteReport(dialog.FileName);
            DiagnosticStatusText.Text = $"Diagnostic report exported: {dialog.FileName}";

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add($"/select,{dialog.FileName}");
            System.Diagnostics.Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            App.Logger.WriteException("HelpPage::ExportDiagnosticReport", ex);
            DiagnosticStatusText.Text = $"Export failed: {ex.Message}";
        }
    }
}
