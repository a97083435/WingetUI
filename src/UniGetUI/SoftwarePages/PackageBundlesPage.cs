using System.Diagnostics;
using System.Text.Json;
using System.Xml.Serialization;
using Windows.UI.Notifications;
using CommunityToolkit.WinUI;
using ExternalLibraries.Pickers;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.Interface.Widgets;
using UniGetUI.PackageEngine;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.Operations;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.SoftwarePages
{
    public class PackageBundlesPage : AbstractPackagesPage
    {
        BetterMenuItem? MenuInstallOptions;
        BetterMenuItem? MenuInstall;
        BetterMenuItem? MenuShare;
        BetterMenuItem? MenuDetails;
        BetterMenuItem? MenuAsAdmin;
        BetterMenuItem? MenuInteractive;
        BetterMenuItem? MenuSkipHash;

        private bool _hasUnsavedChanges = false;
        private bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set
            {
                MainApp.Instance.MainWindow.NavigationPage.BundlesBadge.Visibility =
                    value ? Visibility.Visible : Visibility.Collapsed;
                _hasUnsavedChanges = value;
            }
        }

        public PackageBundlesPage()
        : base(new PackagesPageData
        {
            DisableAutomaticPackageLoadOnStart = true,
            MegaQueryBlockEnabled = false,
            ShowLastLoadTime = false,
            PackagesAreCheckedByDefault = false,
            DisableSuggestedResultsRadio = true,
            PageName = "Bundles",

            Loader = PEInterface.PackageBundlesLoader,
            PageRole = OperationType.Install,

            NoPackages_BackgroundText = CoreTools.Translate("Add packages or open an existing package bundle"),
            NoPackages_SourcesText = CoreTools.Translate("Add packages to start"),
            NoPackages_SubtitleText_Base = CoreTools.Translate("The current bundle has no packages. Add some packages to get started"),
            MainSubtitle_StillLoading = CoreTools.Translate("Loading packages"),
            NoMatches_BackgroundText = CoreTools.Translate("No results were found matching the input criteria"),

            PageTitle = CoreTools.Translate("Package Bundles"),
            Glyph = "\uF133"
        })
        {
            Loader.PackagesChanged += (sender, args) =>
            {
                HasUnsavedChanges = true;
            };

            ReloadButton.Visibility = Visibility.Collapsed;
        }

        public override BetterMenu GenerateContextMenu()
        {
            BetterMenu menu = new();
            MenuInstall = new()
            {
                Text = "Install",
                IconName = IconType.Download,
                KeyboardAcceleratorTextOverride = "Ctrl+Enter"
            };
            MenuInstall.Click += MenuInstall_Invoked;
            menu.Items.Add(MenuInstall);

            menu.Items.Add(new MenuFlyoutSeparator { Height = 5 });

            MenuInstallOptions = new()
            {
                Text = "Installation options",
                IconName = IconType.Options,
                KeyboardAcceleratorTextOverride = "Alt+Enter"
            };
            MenuInstallOptions.Click += MenuInstallSettings_Invoked;
            menu.Items.Add(MenuInstallOptions);

            menu.Items.Add(new MenuFlyoutSeparator());

            MenuAsAdmin = new BetterMenuItem
            {
                Text = "Install as administrator",
                IconName = IconType.UAC
            };
            MenuAsAdmin.Click += MenuAsAdmin_Invoked;
            menu.Items.Add(MenuAsAdmin);

            MenuInteractive = new BetterMenuItem
            {
                Text = "Interactive installation",
                IconName = IconType.Interactive
            };
            MenuInteractive.Click += MenuInteractive_Invoked;
            menu.Items.Add(MenuInteractive);

            MenuSkipHash = new BetterMenuItem
            {
                Text = "Skip hash checks",
                IconName = IconType.Checksum
            };
            MenuSkipHash.Click += MenuSkipHash_Invoked;
            menu.Items.Add(MenuSkipHash);

            menu.Items.Add(new MenuFlyoutSeparator());

            BetterMenuItem menuRemoveFromList = new()
            {
                Text = "Remove from list",
                IconName = IconType.Delete
            };
            menuRemoveFromList.Click += MenuRemoveFromList_Invoked;
            menu.Items.Add(menuRemoveFromList);
            menu.Items.Add(new MenuFlyoutSeparator());

            MenuShare = new()
            {
                Text = "Share this package",
                IconName = IconType.Share
            };
            MenuShare.Click += MenuShare_Invoked;
            menu.Items.Add(MenuShare);

            MenuDetails = new()
            {
                Text = "Package details",
                IconName = IconType.Info_Round,
                KeyboardAcceleratorTextOverride = "Enter"
            };
            MenuDetails.Click += MenuDetails_Invoked;
            menu.Items.Add(MenuDetails);

            return menu;
        }

        public override void GenerateToolBar()
        {
            AppBarButton OpenBundle = new();
            AppBarButton NewBundle = new();
            AppBarButton InstallPackages = new();
            AppBarButton InstallAsAdmin = new();
            AppBarButton InstallInteractive = new();
            AppBarButton InstallSkipHash = new();
            AppBarButton RemoveSelected = new();
            AppBarButton SaveBundle = new();
            AppBarButton PackageDetails = new();
            AppBarButton SharePackage = new();
            AppBarButton HelpButton = new();

            ToolBar.PrimaryCommands.Add(NewBundle);
            ToolBar.PrimaryCommands.Add(OpenBundle);
            ToolBar.PrimaryCommands.Add(SaveBundle);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(InstallPackages);
            ToolBar.PrimaryCommands.Add(InstallAsAdmin);
            ToolBar.PrimaryCommands.Add(InstallInteractive);
            ToolBar.PrimaryCommands.Add(InstallSkipHash);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(RemoveSelected);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(PackageDetails);
            ToolBar.PrimaryCommands.Add(SharePackage);
            ToolBar.PrimaryCommands.Add(new AppBarSeparator());
            ToolBar.PrimaryCommands.Add(HelpButton);

            Dictionary<AppBarButton, string> Labels = new()
                { // Entries with a trailing space are collapsed
                  // Their texts will be used as the tooltip
                    { NewBundle,              CoreTools.Translate("New bundle") },
                    { InstallPackages,        CoreTools.Translate("Install selection") },
                    { InstallAsAdmin,     " " + CoreTools.Translate("Uninstall as administrator") },
                    { InstallInteractive, " " + CoreTools.Translate("Interactive uninstall") },
                    { InstallSkipHash, " " + CoreTools.Translate("Skip integrity checks") },
                    { OpenBundle,             CoreTools.Translate("Open existing bundle") },
                    { RemoveSelected,         CoreTools.Translate("Remove selection from bundle") },
                    { SaveBundle,           CoreTools.Translate("Save bundle as") },
                    { PackageDetails,         " " + CoreTools.Translate("Package details") },
                    { SharePackage,           " " + CoreTools.Translate("Share") },
                    { HelpButton,             CoreTools.Translate("Help") }
                };

            foreach (AppBarButton toolButton in Labels.Keys)
            {
                toolButton.IsCompact = Labels[toolButton][0] == ' ';
                if (toolButton.IsCompact)
                    toolButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                toolButton.Label = Labels[toolButton].Trim();
            }

            Dictionary<AppBarButton, IconType> Icons = new()
                {
                    { NewBundle,              IconType.AddTo },
                    { InstallPackages,        IconType.Download },
                    { InstallAsAdmin,         IconType.UAC },
                    { InstallInteractive,     IconType.Interactive },
                    { InstallSkipHash,        IconType.Checksum },
                    { OpenBundle,             IconType.OpenFolder },
                    { RemoveSelected,         IconType.Delete},
                    { SaveBundle,           IconType.SaveAs },
                    { PackageDetails,         IconType.Info_Round },
                    { SharePackage,           IconType.Share },
                    { HelpButton,             IconType.Help }
                };

            foreach (AppBarButton toolButton in Icons.Keys)
                toolButton.Icon = new LocalIcon(Icons[toolButton]);

            PackageDetails.Click += (s, e) =>
            {
                IPackage? package = SelectedItem as IPackage;
                if (package != null)
                    _ = MainApp.Instance.MainWindow.NavigationPage.ShowPackageDetails(package, OperationType.None);
            };

            HelpButton.Click += (s, e) => { MainApp.Instance.MainWindow.NavigationPage.ShowHelp(); };

            NewBundle.Click += (s, e) =>
            {
                _ = AskForNewBundle();
            };

            RemoveSelected.Click += (s, e) =>
            {
                HasUnsavedChanges = true;
                PEInterface.PackageBundlesLoader.RemoveRange(FilteredPackages.GetCheckedPackages());
            };

            InstallPackages.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages());
            InstallSkipHash.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), skiphash: true);
            InstallInteractive.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), interactive: true);
            InstallAsAdmin.Click += async (s, e) => await ImportAndInstallPackage(FilteredPackages.GetCheckedPackages(), elevated: true);


            OpenBundle.Click += async (s, e) =>
            {
                await OpenFromFile();
            };

            SaveBundle.Click += async (s, e) =>
            {
                await SaveFile();
            };

            SharePackage.Click += (s, e) =>
            {
                IPackage? package = SelectedItem;
                if (package != null) MainApp.Instance.MainWindow.SharePackage(package);
            };

        }

        public async Task<bool> AskForNewBundle()
        {
            if (!Loader.Packages.Any() || !HasUnsavedChanges)
            {
                // Need to call ClearPackages, this method also clears internal caches
                Loader.ClearPackages();
                HasUnsavedChanges = false;
                return true;
            }

            RichTextBlock rtb = new();
            var p = new Paragraph();
            rtb.Blocks.Add(p);
            p.Inlines.Add(new Run() {Text = CoreTools.Translate("Are you sure you want to create a new package bundle? ")});
            p.Inlines.Add(new LineBreak());
            p.Inlines.Add(new Run() {Text = CoreTools.Translate("Any unsaved changes will be lost"), FontWeight = FontWeights.Bold});

            ContentDialog dialog = new()
            {
                Title = CoreTools.Translate("Warning!"),
                Content = rtb,
                DefaultButton = ContentDialogButton.Secondary,
                PrimaryButtonText = CoreTools.Translate("Yes"),
                SecondaryButtonText = CoreTools.Translate("No"),
                XamlRoot = MainApp.Instance.MainWindow.Content.XamlRoot
            };

            ContentDialogResult result = await MainApp.Instance.MainWindow.ShowDialogAsync(dialog);
            if (result == ContentDialogResult.Primary)
            {
                Loader.ClearPackages();
                HasUnsavedChanges = false;
                return true;
            }

            return false;
        }

        public async Task ImportAndInstallPackage(IEnumerable<IPackage> packages, bool? elevated = null, bool? interactive = null, bool? skiphash = null)
        {
            MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Preparing packages, please wait..."));
            List<Package> packages_to_install = new();
            foreach (IPackage package in packages)
            {
                if(package is ImportedPackage imported)
                {
                    Logger.ImportantInfo($"Registering package {imported.Id} from manager {imported.Source.AsString}");
                    packages_to_install.Add(await imported.RegisterAndGetPackageAsync());
                }
                else
                {
                    Logger.Warn($"Attempted to install an invalid/incompatible package with Id={package.Id}");
                }
            }

            MainApp.Instance.MainWindow.HideLoadingDialog();

            foreach (Package package in packages_to_install)
            {
               MainApp.Instance.AddOperationToList(new InstallPackageOperation(package,
                    await InstallationOptions.FromPackageAsync(package, elevated, interactive, skiphash)));
            }
        }

        protected override void WhenPackageCountUpdated()
        {
            return;
        }

#pragma warning disable
        protected override void WhenPackagesLoaded(ReloadReason reason)
        {
            return;
        }
#pragma warning restore

        protected override void WhenShowingContextMenu(IPackage package)
        {
            if (MenuAsAdmin is null
                || MenuInteractive is null
                || MenuSkipHash is null
                || MenuDetails is null
                || MenuShare is null
                || MenuInstall is null
                || MenuInstallOptions is null)
            {
                Logger.Error("Menu items are null on InstalledPackagesTab");
                return;
            }

            bool IS_VALID = package as InvalidImportedPackage is null;

            MenuAsAdmin.IsEnabled = IS_VALID && package.Manager.Capabilities.CanRunAsAdmin;
            MenuInteractive.IsEnabled = IS_VALID && package.Manager.Capabilities.CanRunInteractively;
            MenuSkipHash.IsEnabled = IS_VALID && package.Manager.Capabilities.CanSkipIntegrityChecks;
            MenuDetails.IsEnabled = IS_VALID;
            MenuShare.IsEnabled = IS_VALID;
            MenuInstall.IsEnabled = IS_VALID;
            MenuInstallOptions.IsEnabled = IS_VALID;
        }

        private async void MenuInstall_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            await ImportAndInstallPackage([package]);
        }

        private async void MenuAsAdmin_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;
            await ImportAndInstallPackage([package], elevated: true);
        }

        private async void MenuInteractive_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            await ImportAndInstallPackage([package], interactive: true);

        }
        private async void MenuSkipHash_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            await ImportAndInstallPackage([package], skiphash: true);
        }

        private void MenuShare_Invoked(object sender, RoutedEventArgs args)
        {
            if (PackageList.SelectedItem == null) return;
            MainApp.Instance.MainWindow.SharePackage(SelectedItem);
        }

        private void MenuDetails_Invoked(object sender, RoutedEventArgs args)
        {
            ShowDetailsForPackage(SelectedItem);
        }

        private async void MenuInstallSettings_Invoked(object sender, RoutedEventArgs e)
        {
            IPackage? package = SelectedItem;
            if (package is ImportedPackage imported)
            {
                HasUnsavedChanges = true;
                await MainApp.Instance.MainWindow.NavigationPage.ShowInstallOptionsDialog_ImportedPackage(imported);
            }
        }

        private void MenuRemoveFromList_Invoked(object sender, RoutedEventArgs args)
        {
            IPackage? package = SelectedItem;
            if (package == null) return;

            HasUnsavedChanges = true;
            Loader.Remove(package);
        }


        public async Task OpenFromFile(string? file = null)
        {
            try
            {
                if (await AskForNewBundle() == false)
                    return;

                if (file == null)
                {
                    // Select file
                    FileOpenPicker picker = new(MainApp.Instance.MainWindow.GetWindowHandle());
                    file = picker.Show(new List<string> { "*.ubundle", "*.json", "*.yaml", "*.xml" });
                    if (file == String.Empty)
                        return;
                }

                MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Loading packages, please wait..."));

                // Read file
                BundleFormatType formatType;
                string EXT = file.Split('.')[^1].ToLower();
                if (EXT == "yaml")
                    formatType = BundleFormatType.YAML;
                else if (EXT == "xml")
                    formatType = BundleFormatType.XML;
                else if (EXT == "json" || EXT == "ubundle")
                    formatType = BundleFormatType.JSON;
                else
                    formatType = BundleFormatType.JSON;

                string fileContent = await File.ReadAllTextAsync(file);

                await AddFromBundle(fileContent, formatType);
                HasUnsavedChanges = false;

                MainApp.Instance.MainWindow.HideLoadingDialog();

            }
            catch (Exception ex)
            {

                Logger.Error("An error occurred while attempting to open a bundle");
                Logger.Error(ex);
                var warningDialog = new ContentDialog
                {
                    Title = CoreTools.Translate("The package bundle is not valid"),
                    Content = CoreTools.Translate("The bundle you are trying to load appears to be invalid. Please check the file and try again.") + "\n\n" + ex.Message,
                    CloseButtonText = CoreTools.Translate("Ok"),
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = MainApp.Instance.MainWindow.Content.XamlRoot // Ensure the dialog is shown in the correct context
                };

                MainApp.Instance.MainWindow.HideLoadingDialog();
                await MainApp.Instance.MainWindow.ShowDialogAsync(warningDialog);
            }
        }

        public async Task SaveFile()
        {
            try
            {
                // Get file
                string defaultName = CoreTools.Translate("Package bundle") + ".ubundle";
                string file = (new FileSavePicker(MainApp.Instance.MainWindow.GetWindowHandle())).Show(new List<string> { "*.ubundle", "*.json", "*.yaml", "*.xml" }, defaultName);
                if (file != String.Empty)
                {
                    // Loading dialog
                    MainApp.Instance.MainWindow.ShowLoadingDialog(CoreTools.Translate("Saving packages, please wait..."));

                    // Select appropriate format
                    BundleFormatType formatType;
                    string EXT = file.Split('.')[^1].ToLower();
                    if (EXT == "yaml")
                        formatType = BundleFormatType.YAML;
                    else if (EXT == "xml")
                        formatType = BundleFormatType.XML;
                    else if (EXT == "json" || EXT == "ubundle")
                        formatType = BundleFormatType.JSON;
                    else
                        formatType = BundleFormatType.JSON;

                    // Save serialized data
                    await File.WriteAllTextAsync(file, await CreateBundle(Loader.Packages, formatType));

                    MainApp.Instance.MainWindow.HideLoadingDialog();

                    // Launch file
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = @$"/select, ""{file}"""
                    });

                    HasUnsavedChanges = false;

                }
            }
            catch (Exception ex)
            {
                Logger.Error("An error occurred when saving packages to a file");
                Logger.Error(ex);

                var warningDialog = new ContentDialog
                {
                    Title = CoreTools.Translate("Could not create bundle"),
                    Content = CoreTools.Translate("The package bundle could not be created due to an error.") + "\n\n" + ex.Message,
                    CloseButtonText = CoreTools.Translate("Ok"),
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = MainApp.Instance.MainWindow.Content.XamlRoot // Ensure the dialog is shown in the correct context
                };

                MainApp.Instance.MainWindow.HideLoadingDialog();
                await MainApp.Instance.MainWindow.ShowDialogAsync(warningDialog);


            }
        }

        public static async Task<string> CreateBundle(IEnumerable<IPackage> packages, BundleFormatType formatType = BundleFormatType.JSON)
        {
            SerializableBundle_v1 exportable = new();
            exportable.export_version = 2.0;
            foreach (IPackage package in packages)
                if (package is Package && !package.Source.IsVirtualManager)
                    exportable.packages.Add(await package.AsSerializable());
                else
                    exportable.incompatible_packages.Add(package.AsSerializable_Incompatible());

            Logger.Debug("Finished loading serializable objects. Serializing with format " + formatType);
            string ExportableData;

            if (formatType == BundleFormatType.JSON)
                ExportableData = JsonSerializer.Serialize(exportable, new JsonSerializerOptions { WriteIndented = true });
            else if (formatType == BundleFormatType.YAML)
            {
                YamlDotNet.Serialization.ISerializer serializer = new YamlDotNet.Serialization.SerializerBuilder()
                    .Build();
                ExportableData = serializer.Serialize(exportable);
            }
            else
            {
                string tempfile = Path.GetTempFileName();
                StreamWriter writer = new(tempfile);
                XmlSerializer serializer = new(typeof(SerializableBundle_v1));
                serializer.Serialize(writer, exportable);
                writer.Close();
                ExportableData = await File.ReadAllTextAsync(tempfile);
                File.Delete(tempfile);
            }

            Logger.Debug("Serialization finished successfully");

            return ExportableData;
        }

        public async Task AddFromBundle(string content, BundleFormatType format)
        {

            // Deserialize data
            SerializableBundle_v1? DeserializedData;
            if (format is BundleFormatType.JSON)
            {
                DeserializedData = await Task.Run(() => JsonSerializer.Deserialize<SerializableBundle_v1>(content));
            }
            else if (format is BundleFormatType.YAML)
            {
                YamlDotNet.Serialization.IDeserializer deserializer =
                    new YamlDotNet.Serialization.DeserializerBuilder()
                        .Build();
                DeserializedData = await Task.Run(() => deserializer.Deserialize<SerializableBundle_v1>(content));
            }
            else
            {
                string tempfile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempfile, content);
                StreamReader reader = new(tempfile);
                XmlSerializer serializer = new(typeof(SerializableBundle_v1));
                DeserializedData = await Task.Run(() => serializer.Deserialize(reader) as SerializableBundle_v1);
                reader.Close();
                File.Delete(tempfile);
            }

            if (DeserializedData is null || DeserializedData.export_version is -1)
            {
                throw new ArgumentException("DeserializedData was null");
            }

            List<IPackage> packages = new List<IPackage>();

            foreach (SerializablePackage_v1 DeserializedPackage in DeserializedData.packages)
            {
                packages.Add(PackageFromSerializable(DeserializedPackage));
            }

            foreach (SerializableIncompatiblePackage_v1 DeserializedPackage in DeserializedData
                         .incompatible_packages)
            {
                packages.Add(InvalidPackageFromSerializable(DeserializedPackage, NullSource.Instance));
            }

            await PEInterface.PackageBundlesLoader.AddPackagesAsync(packages);

        }

        public static IPackage PackageFromSerializable(SerializablePackage_v1 raw_package)
        {
            IPackageManager? manager = null;
            IManagerSource? source;

            foreach (var possible_manager in PEInterface.Managers)
            {
                if (possible_manager.Name == raw_package.ManagerName)
                {
                    manager = possible_manager;
                    break;
                }
            }

            if (manager?.Capabilities.SupportsCustomSources == true)
            {
                source = manager?.SourceProvider?.SourceFactory.GetSourceIfExists(raw_package.Source);
            }
            else
                source = manager?.DefaultSource;

            if (manager is null || source is null)
            {
                return InvalidPackageFromSerializable(raw_package.GetInvalidEquivalent(), NullSource.Instance);
            }

            return new ImportedPackage(raw_package, manager, source);
        }

        public static IPackage InvalidPackageFromSerializable(SerializableIncompatiblePackage_v1 raw_package, IManagerSource source)
        {
            return new InvalidImportedPackage(raw_package, source);
        }
    }
}

