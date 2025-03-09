using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace VRCHub;

public partial class MainWindow
{
    private void DatapackCreator_Click(object sender, RoutedEventArgs e)
    {
        Page_Select("DatapackCreator");
    }

    private void Datapack_InputPath_DoubleClicked(object sender, MouseButtonEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog
        {
            Filter = "Asset Bundle (__data, *.vrcw)|__data;*.vrcw|All files (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
            Datapack_InputPath.Text = openFileDialog.FileName;
    }
    private void Datapack_OutputPath_DoubleClicked(object sender, MouseButtonEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog
        {
            Filter = "Asset Pack (*.dp)|*.dp",
            DefaultExt = ".dp"
        };
        if (saveFileDialog.ShowDialog() == true)
            Datapack_OutputFile.Text = saveFileDialog.FileName;
    }

    private void CreateDatapackButton_Clicked(object sender, RoutedEventArgs e)
    {
        ProcessStartInfo PackCreator = new("VRCHubNative.exe")
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
        };
        var InputFile = Datapack_InputPath.Text.Replace("\"", "");
        var OutputFile = Datapack_OutputFile.Text.Replace("\"", "");
        var WorldID = Datapack_WorldID.Text.Replace("\"", "");
        var WorldName = Datapack_WorldName.Text.Replace("\"", "");
        var WorldHash = Datapack_WorldHash.Text.Replace("\"", "");
        var Version = Datapack_Version_Major.Text + "." + Datapack_Version_Min.Text + "." + Datapack_Version_Patch.Text;
        var Author = Datapack_Author.Text.Replace("\"", "");
        CreateDatapack_Button.IsEnabled = false;
        Thread DatapackThread = new(() =>
        {
            PackCreator.Arguments += $"/create ";
            PackCreator.Arguments += $"\"{InputFile}\" ";
            PackCreator.Arguments += $"\"{OutputFile}\" ";
            PackCreator.Arguments += $"\"{WorldID}\" ";
            PackCreator.Arguments += $"\"{WorldName}\" ";
            PackCreator.Arguments += $"\"{WorldHash}\" ";
            PackCreator.Arguments += $"\"{Version}\" ";
            PackCreator.Arguments += $"\"{Author}\" ";
            Process.Start(PackCreator)?.WaitForExit();
            ProcessStartInfo openExplorer = new()
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{OutputFile}\"",
                UseShellExecute = true
            };
            Process.Start(openExplorer);
            Application.Current.Dispatcher.Invoke(() => CreateDatapack_Button.IsEnabled = true);
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };
        DatapackThread.Start();
    }
}