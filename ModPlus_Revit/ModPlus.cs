﻿namespace ModPlus_Revit
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Windows.Media;
    using System.Xml.Linq;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;
    using Enums;
    using Helpers;
    using Microsoft.Win32;
    using ModPlusAPI;
    using ModPlusAPI.Enums;
    using ModPlusAPI.LicenseServer;
    using ModPlusAPI.UserInfo;
    using ModPlusAPI.Windows;
    using Services;

    /// <inheritdoc />
    public class ModPlus : IExternalApplication
    {
        private UIControlledApplication _application;

        /// <inheritdoc />
        public Result OnStartup(UIControlledApplication application)
        {
            _application = application;
            try
            {
                // init lang
                if (!Language.Initialize())
                    return Result.Cancelled;

                // statistic
                Statistic.SendModuleLoaded("Revit", VersionData.CurrentRevitVersion);

                // Принудительная загрузка сборок
                LoadAssemblies();
                UserConfigFile.InitConfigFile();
                LoadPlugins();

                // check adaptation
                CheckAdaptation();

                // Load ribbon
                App.RibbonBuilder.CreateRibbon(application);

                // проверка загруженности модуля автообновления
                CheckAutoUpdaterLoaded();

                var disableConnectionWithLicenseServer = Variables.DisableConnectionWithLicenseServerInRevit;

                // license server
                if (Variables.IsLocalLicenseServerEnable && !disableConnectionWithLicenseServer)
                    ClientStarter.StartConnection(SupportedProduct.Revit);

                if (Variables.IsWebLicenseServerEnable && !disableConnectionWithLicenseServer)
                    WebLicenseServerClient.Instance.Start(SupportedProduct.Revit);

                // user info
                UserInfoService.ProductStartupAuthorization();

#if !R2017 && !R2018
                application.Idling += ApplicationOnIdling;
#endif

                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                // Тут отображение ошибки должно быть в обычном окне, т.к. сборки могли еще не загрузился
                TaskDialog.Show("ModPlus", exception.Message + Environment.NewLine + exception.StackTrace,
                    TaskDialogCommonButtons.Ok);
                return Result.Failed;
            }
        }

#if !R2017 && !R2018
        private void ApplicationOnIdling(object sender, IdlingEventArgs e)
        {
            try
            {
                if (sender is UIApplication uiApp)
                {
                    if (TabColorizer == null)
                    {
                        TabColorizer = new TabColorizer(uiApp, _application);
#if R2019
                        var xceedFile = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "Revit 2019", "Xceed.Wpf.AvalonDock.dll");
                        if (File.Exists(xceedFile) &&
                            Version.TryParse(FileVersionInfo.GetVersionInfo(xceedFile).FileVersion, out var version))
                        {
                            if (version.MinorRevision < 10)
                            {
                                _application.Idling -= ApplicationOnIdling;
                                return;
                            }
                        }
                        else
                        {
                            _application.Idling -= ApplicationOnIdling;
                            return;
                        }
#endif
                        var schemeName = UserConfigFile.GetValue("Revit", "ColorizeTabsSchemeName");
                        TabColorizer.SetState(
                            bool.TryParse(UserConfigFile.GetValue("Revit", "ColorizeTabs"), out var b) && b,
                            !string.IsNullOrEmpty(schemeName) ? schemeName : "Happy Like Pastels",
                            Enum.TryParse(UserConfigFile.GetValue("Revit", "ColorizeTabsZone"), out TabColorizeZone z) ? z : TabColorizeZone.Background,
                            int.TryParse(UserConfigFile.GetValue("Revit", "ColorizeTabsBorderThickness"), out var i) ? i : 4,
                            int.TryParse(UserConfigFile.GetValue("Revit", "ActiveBorderThickness"), out i) ? i : 0,
                            GetColorFromSettings("ActiveBorderColor"),
                            int.TryParse(UserConfigFile.GetValue("Revit", "InactiveBorderThickness"), out i) ? i : 0,
                            GetColorFromSettings("InactiveBorderColor"),
                            bool.TryParse(UserConfigFile.GetValue("Revit", "ColorizeByMask"), out b) && b);
                    }

                    _application.Idling -= ApplicationOnIdling;
                }
            }
            catch (FileNotFoundException)
            {
                _application.Idling -= ApplicationOnIdling;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
                _application.Idling -= ApplicationOnIdling;
            }
        }
#endif

        /// <inheritdoc />
        public Result OnShutdown(UIControlledApplication application)
        {
            ClientStarter.StopConnection();
            return Result.Succeeded;
        }

#if !R2017 && !R2018
        /// <summary>
        /// Статический экземпляр сервиса раскраски вкладок видов
        /// </summary>
        public static TabColorizer TabColorizer { get; private set; }
#endif

        // Принудительная загрузка сборок
        // необходимых для работы
        private static void LoadAssemblies()
        {
            try
            {
                foreach (var fileName in Constants.ExtensionsLibraries)
                {
                    var extDll = Path.Combine(Constants.ExtensionsDirectory, fileName);
                    if (File.Exists(extDll))
                    {
                        Assembly.LoadFrom(extDll);
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        // Загрузка плагинов
        private static void LoadPlugins()
        {
            try
            {
                var functionsKey = Registry.CurrentUser.OpenSubKey("ModPlus\\Functions");
                if (functionsKey == null)
                    return;
                using (functionsKey)
                {
                    foreach (var functionKeyName in functionsKey.GetSubKeyNames())
                    {
                        var functionKey = functionsKey.OpenSubKey(functionKeyName);
                        if (functionKey == null)
                            continue;
                        foreach (var availProductVersionKeyName in functionKey.GetSubKeyNames())
                        {
                            // Если версия продукта не совпадает, то пропускаю
                            if (!availProductVersionKeyName.Equals(VersionData.CurrentRevitVersion))
                                continue;
                            var availProductVersionKey = functionKey.OpenSubKey(availProductVersionKeyName);
                            if (availProductVersionKey == null)
                                continue;

                            // беру свойства функции из реестра
                            var file = availProductVersionKey.GetValue("File") as string;
                            var onOff = availProductVersionKey.GetValue("OnOff") as string;
                            var productFor = availProductVersionKey.GetValue("ProductFor") as string;
                            if (string.IsNullOrEmpty(onOff) || string.IsNullOrEmpty(productFor))
                                continue;
                            if (!productFor.Equals("Revit"))
                                continue;
                            var isOn = !bool.TryParse(onOff, out var b) || b; // default - true

                            // Если "Продукт для" подходит, файл существует и функция включена - гружу
                            if (isOn)
                            {
                                if (!string.IsNullOrEmpty(file) && File.Exists(file))
                                {
                                    // load
                                    var localFuncAssembly = Assembly.LoadFrom(file);
                                    LoadPluginsUtils.LoadDataFromPluginInterface(localFuncAssembly, file);
                                }
                                else
                                {
                                    var foundedFile = LoadPluginsUtils.FindFile(functionKeyName);
                                    if (!string.IsNullOrEmpty(foundedFile) && File.Exists(foundedFile))
                                    {
                                        var localFuncAssembly = Assembly.LoadFrom(foundedFile);
                                        LoadPluginsUtils.LoadDataFromPluginInterface(localFuncAssembly, foundedFile);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Проверка загруженности модуля автообновления
        /// </summary>
        private static void CheckAutoUpdaterLoaded()
        {
            try
            {
                var loadWithWindows = !bool.TryParse(RegistryUtils.GetValue("AutoUpdater", "LoadWithWindows"), out bool b) || b;
                if (loadWithWindows)
                {
                    // Если "грузить с виндой", то проверяем, что модуль запущен
                    // если не запущен - запускаем
                    var isOpen = Process.GetProcesses().Any(t => t.ProcessName == "mpAutoUpdater");
                    if (!isOpen)
                    {
                        var fileToStart = Path.Combine(Constants.CurrentDirectory, "mpAutoUpdater.exe");
                        if (File.Exists(fileToStart))
                        {
                            Process.Start(fileToStart);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Statistic.SendException(exception);
            }
        }

        private static async void CheckAdaptation()
        {
            var confCuiXel = ModPlusAPI.RegistryData.Adaptation.GetCuiAsXElement("Revit");

            // Проходим по группам
            if (confCuiXel == null || confCuiXel.IsEmpty)
            {
                if (await ModPlusAPI.Web.Connection.HasAllConnectionAsync(3))
                {
                    // Грузим файл
                    try
                    {
                        const string url = "http://www.modplus.org/Downloads/StandardCUIRevit.xml";
                        if (string.IsNullOrEmpty(url))
                            return;
                        string xmlStr;
                        using (var wc = new WebClientWithTimeout { Proxy = ModPlusAPI.Web.Proxy.GetWebProxy() })
                            xmlStr = wc.DownloadString(url);
                        var xmlDocument = XElement.Parse(xmlStr);

                        ModPlusAPI.RegistryData.Adaptation.SaveCuiFromXElement("Revit", xmlDocument);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
        
        private Color GetColorFromSettings(string key)
        {
            try
            {
                if (ColorConverter.ConvertFromString(UserConfigFile.GetValue("Revit", key)) is Color color)
                    return color;
                return Colors.Black;
            }
            catch
            {
                return Colors.Black;
            }
        }

        /// <summary>
        /// <see cref="WebClient"/> wrapper
        /// </summary>
        internal class WebClientWithTimeout : WebClient
        {
            /// <inheritdoc/>
            protected override WebRequest GetWebRequest(Uri uri)
            {
                var w = base.GetWebRequest(uri);

                // ReSharper disable once PossibleNullReferenceException
                w.Timeout = 3000;
                return w;
            }
        }
    }
}
