namespace ModPlus_Revit.App
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Enums;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Enums;
    using ModPlusAPI.LicenseServer;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.UserInfo;
    using ModPlusAPI.Windows;
    using ModPlusStyle;
    using ModPlusStyle.Controls.Dialogs;

    /// <summary>
    /// Модель представления окна настроек
    /// </summary>
    public class SettingsViewModel : VmBase
    {
        private const string LangApi = "ModPlusAPI";
        private readonly SettingsWindow _parentWindow;
        private Language.LangItem _selectedLanguage;
        private BitmapImage _languageImage;

        private string _languageOnWindowOpen;
        private bool _restartClientOnClose = true;

        private bool _canStopLocalLicenseServerConnection;
        private bool _canStopWebLicenseServerNotification;

        private ColorizeScheme _selectedColorizeScheme;
        private List<ColorizeScheme> _colorizeSchemes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="parentWindow">Parent window</param>
        public SettingsViewModel(SettingsWindow parentWindow)
        {
            _parentWindow = parentWindow;
            FillData();
        }

        /// <summary>
        /// Список доступных языков
        /// </summary>
        public List<Language.LangItem> Languages { get; private set; }

        /// <summary>
        /// Выбранный язык
        /// </summary>
        public Language.LangItem SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value)
                    return;
                _selectedLanguage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MessageAboutLanguageVisibility));
                Language.SetCurrentLanguage(value.Name);
                _parentWindow.SetLanguageProviderForModPlusWindow();
                _parentWindow.SetLanguageProviderForModPlusWindow("LangApi");
                SetLanguageImage();
            }
        }

        /// <summary>
        /// Изображение для языка (флаг)
        /// </summary>
        public BitmapImage LanguageImage
        {
            get => _languageImage;
            set
            {
                if (_languageImage == value)
                    return;
                _languageImage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Видимость сообщения о невозможности изменить какие-то настройки при переключении языка без перезагрузки
        /// </summary>
        public Visibility MessageAboutLanguageVisibility =>
            SelectedLanguage.Name != _languageOnWindowOpen ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Доступные темы
        /// </summary>
        public List<Theme> Themes { get; private set; }

        /// <summary>
        /// Выбранная тема оформления
        /// </summary>
        public Theme SelectedTheme
        {
            get => ThemeManager.GetTheme(Variables.PluginStyle);
            set
            {
                Variables.PluginStyle = value.Name;
                OnPropertyChanged();
                ThemeManager.ChangeTheme(_parentWindow, value);
            }
        }

        /// <summary>
        /// Разделители для чисел
        /// </summary>
        public string[] Separators => new[] { ".", "," };

        /// <summary>
        /// Выбранный разделитель целой и дробной части чисел
        /// </summary>
        public string SelectedSeparator
        {
            get => Variables.Separator;
            set => Variables.Separator = value;
        }

        /// <summary>
        /// Запускает окно настроек уведомлений
        /// </summary>
        public ICommand NotificationSettingsCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                UserInfoService.ShowUserNotificationSettings();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        });

        #region LAN License Server

        /// <summary>
        /// Включена работа с ЛВС Сервером Лицензий
        /// </summary>
        public bool IsLocalLicenseServerEnable
        {
            get => Variables.IsLocalLicenseServerEnable;
            set
            {
                Variables.IsLocalLicenseServerEnable = value;
                OnPropertyChanged();
                if (value)
                    IsWebLicenseServerEnable = false;
            }
        }

        /// <summary>
        /// Ip адрес локального сервера лицензий
        /// </summary>
        public string LocalLicenseServerIpAddress
        {
            get => Variables.LocalLicenseServerIpAddress;
            set => Variables.LocalLicenseServerIpAddress = value;
        }

        /// <summary>
        /// Порт локального сервера лицензий
        /// </summary>
        public int? LocalLicenseServerPort
        {
            get => Variables.LocalLicenseServerPort;
            set => Variables.LocalLicenseServerPort = value;
        }

        /// <summary>
        /// Можно ли выполнить остановку локального сервера лицензий. True - значит локальный сервер лицензий работает
        /// на момент открытия окна
        /// </summary>
        public bool CanStopLocalLicenseServerConnection
        {
            get => _canStopLocalLicenseServerConnection;
            set
            {
                if (_canStopLocalLicenseServerConnection == value)
                    return;
                _canStopLocalLicenseServerConnection = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Проверка соединения с локальным сервером лицензий
        /// </summary>
        public ICommand CheckLocalLicenseServerConnectionCommand => new RelayCommandWithoutParameter(async () =>
            {
                try
                {
                    await _parentWindow.ShowMessageAsync(
                        ClientStarter.IsLicenseServerAvailable()
                            ? Language.GetItem(LangApi, "h21")
                            : Language.GetItem(LangApi, "h20"),
                        $"{Language.GetItem(LangApi, "h22")} {LocalLicenseServerIpAddress}:{LocalLicenseServerPort}");
                }
                catch (Exception exception)
                {
                    ExceptionBox.Show(exception);
                }
            });

        /// <summary>
        /// Прервать работу локального сервера лицензий
        /// </summary>
        public ICommand StopLocalLicenseServerCommand => new RelayCommandWithoutParameter(
            () =>
        {
            try
            {
                ClientStarter.StopConnection();
                CanStopLocalLicenseServerConnection = false;
                _restartClientOnClose = false;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        },
            _ => CanStopLocalLicenseServerConnection);

        /// <summary>
        /// Восстановить работу локального сервера лицензий
        /// </summary>
        public ICommand RestoreLocalLicenseServerCommand => new RelayCommandWithoutParameter(
            () =>
        {
            try
            {
                ClientStarter.StartConnection(SupportedProduct.Revit);
                CanStopLocalLicenseServerConnection = true;
                _restartClientOnClose = true;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        },
            _ => !CanStopLocalLicenseServerConnection);

        #endregion

        #region Web License Server

        /// <summary>
        /// Включена работа с веб сервером лицензий
        /// </summary>
        public bool IsWebLicenseServerEnable
        {
            get => Variables.IsWebLicenseServerEnable;
            set
            {
                Variables.IsWebLicenseServerEnable = value;
                OnPropertyChanged();
                if (value)
                    IsLocalLicenseServerEnable = false;
            }
        }

        /// <summary>
        /// Уникальный идентификатор сервера лицензий
        /// </summary>
        public string WebLicenseServerGuid
        {
            get => Variables.WebLicenseServerGuid.ToString();
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    Variables.WebLicenseServerGuid = Guid.Empty;
                    WebLicenseServerClient.Instance.ReloadLicenseServerData();
                }
                else if (Guid.TryParse(value, out var guid))
                {
                    Variables.WebLicenseServerGuid = guid;
                    WebLicenseServerClient.Instance.ReloadLicenseServerData();
                }

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Адрес электронной почты пользователя, используемый для его идентификации в веб сервере лицензий
        /// </summary>
        public string WebLicenseServerUserEmail
        {
            get => Variables.WebLicenseServerUserEmail;
            set
            {
                Variables.WebLicenseServerUserEmail = value;
                WebLicenseServerClient.Instance.ReloadLicenseServerData();
            }
        }

        /// <summary>
        /// Можно ли остановить отправку уведомлений веб серверу лицензий
        /// </summary>
        public bool CanStopWebLicenseServerNotification
        {
            get => _canStopWebLicenseServerNotification;
            set
            {
                if (_canStopWebLicenseServerNotification == value)
                    return;
                _canStopWebLicenseServerNotification = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Проверка доступности веб сервера лицензий
        /// </summary>
        public ICommand CheckWebLicenseServerConnectionCommand => new RelayCommandWithoutParameter(async () =>
        {
            var result = false;
            await _parentWindow.InvokeWithIndeterminateProgress(Task.Run(async () =>
            {
                result = await WebLicenseServerClient.Instance.CheckConnection();
            }));
            if (result)
            {
                await _parentWindow.ShowMessageAsync(Language.GetItem(LangApi, "h21"), string.Empty);
            }
            else
            {
                ModPlusAPI.Windows.MessageBox.Show(
                    string.Format(Language.GetItem(LangApi, "h39"), WebLicenseServerGuid),
                    Language.GetItem(LangApi, "h20"),
                    MessageBoxIcon.Close);
            }
        });

        /// <summary>
        /// Проверка текущего пользователя на доступность работы с сервером лицензий
        /// </summary>
        public ICommand CheckIsUserAllowForWebLicenseServerCommand => new RelayCommandWithoutParameter(async () =>
        {
            var result = false;
            await _parentWindow.InvokeWithIndeterminateProgress(Task.Run(async () =>
            {
                result = await WebLicenseServerClient.Instance.CheckIsValidForUser();
            }));
            if (result)
            {
                // Для пользователя "{0}" имеется доступ к серверу лицензий "{1}"
                ModPlusAPI.Windows.MessageBox.Show(
                    string.Format(Language.GetItem(LangApi, "h41"), WebLicenseServerUserEmail, WebLicenseServerGuid),
                    MessageBoxIcon.Message);
            }
            else
            {
                // Для пользователя "{0}" отсутствует доступ к серверу лицензий "{1}"
                ModPlusAPI.Windows.MessageBox.Show(
                    string.Format(Language.GetItem(LangApi, "h42"), WebLicenseServerUserEmail, WebLicenseServerGuid) +
                    Environment.NewLine + Language.GetItem(LangApi, "h43"),
                    MessageBoxIcon.Close);
            }
        });

        /// <summary>
        /// Остановить отправку уведомлений веб серверу лицензий
        /// </summary>
        public ICommand StopWebLicenseServerNotificationsCommand => new RelayCommandWithoutParameter(
            () =>
        {
            WebLicenseServerClient.Instance.Stop();
            CanStopWebLicenseServerNotification = false;
        },
            _ => CanStopWebLicenseServerNotification);

        /// <summary>
        /// Восстановить отправку уведомлений веб серверу лицензий
        /// </summary>
        public ICommand RestoreWebLicenseServerNotificationsCommand => new RelayCommandWithoutParameter(
            () =>
        {
            WebLicenseServerClient.Instance.Start(SupportedProduct.Revit);
            CanStopWebLicenseServerNotification = true;
        },
            _ => !CanStopWebLicenseServerNotification);

        #endregion

        #region Tab colorizer

        /// <summary>
        /// Видимость настроек раскраски вкладок
        /// </summary>
        public bool IsVisibleTabColorizerSettings
        {
            get
            {
#if R2017 || R2018
                return false;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// Выполнять раскраску вкладок видов
        /// </summary>
        public bool ColorizeTabs
        {
#if R2017 || R2018
            get;
            set;
#else
            get => bool.TryParse(UserConfigFile.GetValue("Revit", nameof(ColorizeTabs)), out var b) && b;
            set
            {
                UserConfigFile.SetValue("Revit", nameof(ColorizeTabs), value.ToString(), true);
                OnPropertyChanged();
            }
#endif
        }

        /// <summary>
        /// Доступность сервиса раскраски по версии Revit
        /// </summary>
        public bool IsEnabledColorizeTabsForRevitVersion
        {
            get
            {
#if R2017 || R2018
                return false;
#elif R2019
                var xceedFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "Revit 2019", "Xceed.Wpf.AvalonDock.dll");
                if (File.Exists(xceedFile) &&
                    Version.TryParse(FileVersionInfo.GetVersionInfo(xceedFile).FileVersion, out var version))
                {
                    if (version.MinorRevision < 10)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                return true;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// Доступные цветовые схемы
        /// </summary>
        public List<ColorizeScheme> ColorizeSchemes
        {
            get
            {
#if R2017 || R2018
                return new List<ColorizeScheme>();
#else
                return _colorizeSchemes ?? (_colorizeSchemes = ModPlus.TabColorizer.GetColorSchemes());
#endif
            }
        }

        /// <summary>
        /// Выбранная цветовая схема
        /// </summary>
        public ColorizeScheme SelectedColorizeScheme
        {
#if R2017 || R2018
            get;
            set;
#else
            get => _selectedColorizeScheme;
            set
            {
                if (_selectedColorizeScheme == value)
                    return;
                _selectedColorizeScheme = value;
                OnPropertyChanged();
                if (value != null)
                    UserConfigFile.SetValue("Revit", "ColorizeTabsSchemeName", value.Name, true);
                RaiseColorizePreviewPropertiesChanged();
            }
#endif
        }

        /// <summary>
        /// Зона раскраски вкладок
        /// </summary>
        public TabColorizeZone ColorizeTabsZone
        {
#if R2017 || R2018
            get;
            set;
#else
            get => Enum.TryParse(UserConfigFile.GetValue("Revit", nameof(ColorizeTabsZone)), out TabColorizeZone zone)
                ? zone
                : TabColorizeZone.Background;
            set
            {
                UserConfigFile.SetValue("Revit", nameof(ColorizeTabsZone), value.ToString(), true);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEnabledColorizeTabsBorderThickness));
                RaiseColorizePreviewPropertiesChanged();
            }
#endif
        }

        /// <summary>
        /// Is enabled <see cref="ColorizeTabsBorderThickness"/>
        /// </summary>
        public bool IsEnabledColorizeTabsBorderThickness => ColorizeTabsZone != TabColorizeZone.Background;

        /// <summary>
        /// Толщина границы для зон раскраски Border...
        /// </summary>
        public int ColorizeTabsBorderThickness
        {
#if R2017 || R2018
            get;
            set;
#else
            get => int.TryParse(UserConfigFile.GetValue("Revit", nameof(ColorizeTabsBorderThickness)), out var i)
                ? i
                : 4;
            set
            {
                UserConfigFile.SetValue("Revit", nameof(ColorizeTabsBorderThickness), value.ToString(), true);
                OnPropertyChanged();
                RaiseColorizePreviewPropertiesChanged();
            }
#endif
        }

        /// <summary>
        /// Задний фон первой вкладки на предварительном просмотре
        /// </summary>
        public Brush ColorizePreviewFirstTabBackground
        {
#if R2017 || R2018
            get;
#else
            get
            {
                if (ColorizeTabsZone == TabColorizeZone.Background)
                    return SelectedColorizeScheme.Brushes[0];
                return Brushes.White;
            }
#endif
        }

        /// <summary>
        /// Задний фон второй вкладки на предварительном просмотре
        /// </summary>
        public Brush ColorizePreviewSecondTabBackground
        {
#if R2017 || R2018
            get;
#else
            get
            {
                if (ColorizeTabsZone == TabColorizeZone.Background)
                    return SelectedColorizeScheme.Brushes[1];
                return (SolidColorBrush)new ColorConverter().ConvertFrom("#cccccc");
            }
#endif
        }

        /// <summary>
        /// Задний фон третьей вкладки на предварительном просмотре
        /// </summary>
        public Brush ColorizePreviewThirdTabBackground
        {
#if R2017 || R2018
            get;
#else
            get
            {
                if (ColorizeTabsZone == TabColorizeZone.Background)
                    return SelectedColorizeScheme.Brushes[2];
                return (SolidColorBrush)new ColorConverter().ConvertFrom("#cccccc");
            }
#endif
        }

        /// <summary>
        /// Толщина рамок вкладок на предварительном просмотре
        /// </summary>
        public Thickness ColorizePreviewBorderThickness
        {
#if R2017 || R2018
            get;
#else
            get
            {
                switch (ColorizeTabsZone)
                {
                    case TabColorizeZone.BorderLeft:
                        return new Thickness(ColorizeTabsBorderThickness, 0, 0, 0);
                    case TabColorizeZone.BorderBottom:
                        return new Thickness(0, 0, 0, ColorizeTabsBorderThickness);
                    case TabColorizeZone.BorderTop:
                        return new Thickness(0, ColorizeTabsBorderThickness, 0, 0);
                    case TabColorizeZone.Background:
                        return new Thickness(0);
                    default:
                        return new Thickness(0);
                }
            }
#endif
        }

        /// <summary>
        /// Заливка рамок первой вкладки на предварительном просмотре
        /// </summary>
        public Brush ColorizePreviewFirstBorderBrush
        {
#if R2017 || R2018
            get;
#else
            get
            {
                if (ColorizeTabsZone == TabColorizeZone.Background)
                    return Brushes.Transparent;
                return SelectedColorizeScheme.Brushes[0];
            }
#endif
        }

        /// <summary>
        /// Заливка рамок второй вкладки на предварительном просмотре
        /// </summary>
        public Brush ColorizePreviewSecondBorderBrush
        {
#if R2017 || R2018
            get;
#else
            get
            {
                if (ColorizeTabsZone == TabColorizeZone.Background)
                    return Brushes.Transparent;
                return SelectedColorizeScheme.Brushes[1];
            }
#endif
        }

        /// <summary>
        /// Заливка рамок третьей вкладки на предварительном просмотре
        /// </summary>
        public Brush ColorizePreviewThirdBorderBrush
        {
#if R2017 || R2018
            get;
#else
            get
            {
                if (ColorizeTabsZone == TabColorizeZone.Background)
                    return Brushes.Transparent;
                return SelectedColorizeScheme.Brushes[2];
            }
#endif
        }

        #endregion

        /// <summary>
        /// Отключить работу сервера лицензий в Revit
        /// </summary>
        public bool DisableConnectionWithLicenseServerInRevit
        {
            get => Variables.DisableConnectionWithLicenseServerInRevit;
            set => Variables.DisableConnectionWithLicenseServerInRevit = value;
        }

        /// <summary>
        /// Заполнение данных
        /// </summary>
        public void FillData()
        {
            try
            {
                FillAndSetLanguages();
                FillThemesAndColors();

#if !R2017 && !R2018
                var savedColorizeSchemeName = UserConfigFile.GetValue("Revit", "ColorizeTabsSchemeName");
                var savedColorizeScheme = ColorizeSchemes.FirstOrDefault(s => s.Name == savedColorizeSchemeName);
                _selectedColorizeScheme = savedColorizeScheme ?? ColorizeSchemes.First();
#endif

                CanStopLocalLicenseServerConnection = ClientStarter.IsClientWorking();
                CanStopWebLicenseServerNotification =
                    Variables.IsWebLicenseServerEnable &&
                    !Variables.DisableConnectionWithLicenseServerInAutoCAD &&
                    WebLicenseServerClient.Instance.IsWorked;
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        }

        /// <summary>
        /// Применить настройки
        /// </summary>
        public void ApplySettings()
        {
            try
            {
#if !R2017 && !R2018
                ModPlus.TabColorizer.SetState(
                    ColorizeTabs,
                    SelectedColorizeScheme.Name,
                    ColorizeTabsZone,
                    ColorizeTabsBorderThickness);
                ModPlus.TabColorizer.Colorize();
#endif

                if (_restartClientOnClose)
                {
                    // reload server
                    ClientStarter.StopConnection();
                    ClientStarter.StartConnection(SupportedProduct.Revit);
                }

                if (!IsWebLicenseServerEnable && WebLicenseServerClient.Instance.IsWorked)
                    WebLicenseServerClient.Instance.Stop();
                if (IsWebLicenseServerEnable && !WebLicenseServerClient.Instance.IsWorked)
                    WebLicenseServerClient.Instance.Start(SupportedProduct.Revit);
            }
            catch (Exception ex)
            {
                ExceptionBox.Show(ex);
            }
        }

        private void FillAndSetLanguages()
        {
            Languages = Language.GetLanguagesByFiles();
            OnPropertyChanged(nameof(Languages));
            var selectedLanguage = Languages.First(li => li.Name == Language.CurrentLanguageName);
            _languageOnWindowOpen = selectedLanguage.Name;
            SelectedLanguage = selectedLanguage;
        }

        private void FillThemesAndColors()
        {
            Themes = new List<Theme>(ThemeManager.Themes);
            OnPropertyChanged(nameof(Themes));
            var pluginStyle = ThemeManager.Themes.First();
            var savedPluginStyleName = Variables.PluginStyle;
            if (!string.IsNullOrEmpty(savedPluginStyleName))
            {
                var theme = ThemeManager.Themes.Single(t => t.Name == savedPluginStyleName);
                if (theme != null)
                    pluginStyle = theme;
            }

            SelectedTheme = pluginStyle;
        }

        private void SetLanguageImage()
        {
            try
            {
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.UriSource = new Uri(
                    $"pack://application:,,,/ModPlus_Revit_{VersionData.CurrentRevitVersion};component/Resources/Flags/{SelectedLanguage.Name}.png");
                bi.EndInit();
                LanguageImage = bi;
            }
            catch
            {
                LanguageImage = null;
            }
        }

        private void RaiseColorizePreviewPropertiesChanged()
        {
            OnPropertyChanged(nameof(ColorizePreviewFirstTabBackground));
            OnPropertyChanged(nameof(ColorizePreviewSecondTabBackground));
            OnPropertyChanged(nameof(ColorizePreviewThirdTabBackground));
            OnPropertyChanged(nameof(ColorizePreviewBorderThickness));
            OnPropertyChanged(nameof(ColorizePreviewFirstBorderBrush));
            OnPropertyChanged(nameof(ColorizePreviewSecondBorderBrush));
            OnPropertyChanged(nameof(ColorizePreviewThirdBorderBrush));
        }
    }
}
