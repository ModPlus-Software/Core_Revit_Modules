namespace ModPlus_Revit.App
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Xml.Linq;
    using Enums;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Mvvm;
    using ModPlusAPI.Windows;
    using View;
    using MessageBox = ModPlusAPI.Windows.MessageBox;

    /// <summary>
    /// Контекст настройки раскраски вкладок
    /// </summary>
    public class TabColorizerSettingsContext : ObservableObject
    {
        private readonly SettingsWindow _parentWindow;
        private readonly SolidColorBrush _defGrayBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#cccccc");
        private ColorizeScheme _selectedColorizeScheme;
        private ObservableCollection<ColorizeScheme> _colorizeSchemes;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabColorizerSettingsContext"/> class.
        /// </summary>
        /// <param name="parentWindow">Parent window</param>
        public TabColorizerSettingsContext(SettingsWindow parentWindow)
        {
            _parentWindow = parentWindow;
#if !R2017 && !R2018
            var savedColorizeSchemeName = UserConfigFile.GetValue("Revit", "ColorizeTabsSchemeName");
            var savedColorizeScheme = ColorizeSchemes?.FirstOrDefault(s => s.Name == savedColorizeSchemeName);
            _selectedColorizeScheme = savedColorizeScheme ?? ColorizeSchemes?.FirstOrDefault();
#endif
        }

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
                var xceedFile = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Autodesk", "Revit 2019", "Xceed.Wpf.AvalonDock.dll");
                if (System.IO.File.Exists(xceedFile) &&
                    Version.TryParse(System.Diagnostics.FileVersionInfo.GetVersionInfo(xceedFile).FileVersion, out var version))
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
        public ObservableCollection<ColorizeScheme> ColorizeSchemes
        {
            get
            {
#if R2017 || R2018
                return new ObservableCollection<ColorizeScheme>();
#else
                if (_colorizeSchemes != null)
                {
                    return _colorizeSchemes;
                }

                _colorizeSchemes = new ObservableCollection<ColorizeScheme>();
                _colorizeSchemes.CollectionChanged += (sender, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        foreach (ColorizeScheme newItem in args.NewItems)
                        {
                            newItem.ColorChanged += (o, eventArgs) =>
                            {
                                RaiseColorizePreviewPropertiesChanged();
                            };
                        }
                    }
                };

                foreach (var colorizeScheme in ModPlus.TabColorizer.GetColorSchemes())
                {
                    _colorizeSchemes.Add(colorizeScheme);
                }

                return _colorizeSchemes;
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
        /// Толщина границ неактивных вкладок
        /// </summary>
        public int InactiveBorderThickness
        {
#if R2017 || R2018
            get;
            set;
#else
            get => int.TryParse(UserConfigFile.GetValue("Revit", nameof(InactiveBorderThickness)), out var i)
                ? i
                : 0;
            set
            {
                UserConfigFile.SetValue("Revit", nameof(InactiveBorderThickness), value.ToString(), true);
                OnPropertyChanged();
                RaiseColorizePreviewPropertiesChanged();
            }
#endif
        }

        /// <summary>
        /// Цвет границ неактивных вкладок
        /// </summary>
        public Color InactiveBorderColor
        {
#if R2017 || R2018
            get;
            set;
#else
            get
            {
                try
                {
                    if (ColorConverter.ConvertFromString(
                        UserConfigFile.GetValue("Revit", nameof(InactiveBorderColor))) is Color color)
                        return color;
                    return Colors.Black;
                }
                catch
                {
                    return Colors.Black;
                }
            }

            set
            {
                UserConfigFile.SetValue("Revit", nameof(InactiveBorderColor), new ColorConverter().ConvertToString(value), true);
                OnPropertyChanged();
                RaiseColorizePreviewPropertiesChanged();
            }
#endif
        }

        /// <summary>
        /// Толщина границ активных вкладок
        /// </summary>
        public int ActiveBorderThickness
        {
#if R2017 || R2018
            get;
            set;
#else
            get => int.TryParse(UserConfigFile.GetValue("Revit", nameof(ActiveBorderThickness)), out var i)
                ? i
                : 0;
            set
            {
                UserConfigFile.SetValue("Revit", nameof(ActiveBorderThickness), value.ToString(), true);
                OnPropertyChanged();
                RaiseColorizePreviewPropertiesChanged();
            }
#endif
        }

        /// <summary>
        /// Цвет границ активных вкладок
        /// </summary>
        public Color ActiveBorderColor
        {
#if R2017 || R2018
            get;
            set;
#else
            get
            {
                try
                {
                    if (ColorConverter.ConvertFromString(
                        UserConfigFile.GetValue("Revit", nameof(ActiveBorderColor))) is Color color)
                        return color;
                    return Colors.Black;
                }
                catch
                {
                    return Colors.Black;
                }
            }

            set
            {
                UserConfigFile.SetValue("Revit", nameof(ActiveBorderColor), new ColorConverter().ConvertToString(value), true);
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
                return ColorizeTabsZone == TabColorizeZone.Background
                    ? SelectedColorizeScheme.GetBrush(0) ?? Brushes.White
                    : Brushes.White;
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
                return ColorizeTabsZone == TabColorizeZone.Background
                    ? SelectedColorizeScheme.GetBrush(1) ?? _defGrayBrush
                    : _defGrayBrush;
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
                return ColorizeTabsZone == TabColorizeZone.Background
                    ? SelectedColorizeScheme.GetBrush(2) ?? _defGrayBrush
                    : _defGrayBrush;
            }
#endif
        }

        /// <summary>
        /// Толщина рамок неактивных вкладок на предварительном просмотре
        /// </summary>
        public Thickness ColorizePreviewInactiveBorderThickness
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
                        return new Thickness(InactiveBorderThickness);
                    default:
                        return new Thickness(0);
                }
            }
#endif
        }

        /// <summary>
        /// Толщина рамок активных вкладок на предварительном просмотре
        /// </summary>
        public Thickness ColorizePreviewActiveBorderThickness
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
                        return new Thickness(ActiveBorderThickness);
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
                return ColorizeTabsZone == TabColorizeZone.Background
                    ? new SolidColorBrush(ActiveBorderColor)
                    : SelectedColorizeScheme.GetBrush(0) ?? Brushes.Transparent;
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
                return ColorizeTabsZone == TabColorizeZone.Background
                    ? new SolidColorBrush(InactiveBorderColor)
                    : SelectedColorizeScheme.GetBrush(1) ?? Brushes.Transparent;
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
                return ColorizeTabsZone == TabColorizeZone.Background
                    ? new SolidColorBrush(InactiveBorderColor)
                    : SelectedColorizeScheme.GetBrush(2) ?? Brushes.Transparent;
            }
#endif
        }

        /// <summary>
        /// Раскраску выполнять по маске имени документа
        /// </summary>
        public bool ColorizeByMask
        {
#if R2017 || R2018
            get;
            set;
#else
            get => bool.TryParse(UserConfigFile.GetValue("Revit", nameof(ColorizeByMask)), out var b) && b;
            set
            {
                UserConfigFile.SetValue("Revit", nameof(ColorizeByMask), value.ToString(), true);
                OnPropertyChanged();
            }
#endif
        }

        /// <summary>
        /// Добавить новую пользовательскую цветовую схему
        /// </summary>
        public ICommand AddNewColorizeSchemeCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                var win = new NewColorizeSchemeNameWindow(_parentWindow, ColorizeSchemes.Select(s => s.Name).ToList());
                if (win.ShowDialog() != true)
                    return;

                var newScheme = new ColorizeScheme(win.TbName.Text, true);
                newScheme.ColorRules.Add(new ColorRule(Colors.Red));
                ColorizeSchemes.Add(newScheme);
                SelectedColorizeScheme = newScheme;
                RaiseColorizePreviewPropertiesChanged();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        });

        /// <summary>
        /// Удалить выбранную цветовую схему
        /// </summary>
        public ICommand RemoveSelectedColorizeSchemeCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                ColorizeSchemes.Remove(SelectedColorizeScheme);
                SelectedColorizeScheme = ColorizeSchemes.Last();
                RaiseColorizePreviewPropertiesChanged();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        });

        /// <summary>
        /// Добавить новый цвет
        /// </summary>
        public ICommand AddNewColorCommand => new RelayCommandWithoutParameter(() =>
        {
            try
            {
                SelectedColorizeScheme.ColorRules.Add(new ColorRule(Colors.Red));
                RaiseColorizePreviewPropertiesChanged();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        });

        /// <summary>
        /// Удалить правило назначения цвета
        /// </summary>
        public ICommand RemoveColorRuleCommand => new RelayCommand<ColorRule>(colorRule =>
        {
            try
            {
                if (SelectedColorizeScheme.ColorRules.Count == 1)
                {
                    // You cannot delete the last color in the list
                    MessageBox.Show(Language.GetItem("RevitDlls", "c11"), MessageBoxIcon.Close);
                    return;
                }

                SelectedColorizeScheme.ColorRules.Remove(colorRule);
                RaiseColorizePreviewPropertiesChanged();
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }
        });

        /// <summary>
        /// Сохранить пользовательские цветовые схемы
        /// </summary>
        public void SaveCustomColorSchemes()
        {
            var colorsElement = UserConfigFile.ConfigFileXml.Element("CustomColorizeSchemes");
            colorsElement?.Remove();
            colorsElement = new XElement("CustomColorizeSchemes");
            UserConfigFile.ConfigFileXml.Add(colorsElement);
            
            var masksElement = UserConfigFile.ConfigFileXml.Element("CustomColorizeSchemeTitleMasks");
            masksElement?.Remove();
            masksElement = new XElement("CustomColorizeSchemeTitleMasks");
            UserConfigFile.ConfigFileXml.Add(masksElement);

            foreach (var colorizeScheme in ColorizeSchemes)
            {
                if (colorizeScheme.IsEnabledToModify)
                {
                    var colorXel = new XElement("ColorizeScheme");
                    colorXel.SetValue(
                        $"{colorizeScheme.Name},{string.Join(",", colorizeScheme.ColorRules.Select(c => c.Color))}");
                    colorsElement.Add(colorXel);
                }

                var maskXel = new XElement("ColorizeSchemeMask");
                maskXel.SetValue($"{colorizeScheme.Name}|{string.Join("|", colorizeScheme.ColorRules.Select(c => c.DocumentNameMask))}");
                masksElement.Add(maskXel);
            }

            UserConfigFile.SaveConfigFile();
        }

        /// <summary>
        /// Применить настройки
        /// </summary>
        public void ApplySettings()
        {
#if !R2017 && !R2018
            if (ModPlus.TabColorizer != null)
            {
                ModPlus.TabColorizer.SetState(
                    ColorizeTabs,
                    SelectedColorizeScheme?.Name,
                    ColorizeTabsZone,
                    ColorizeTabsBorderThickness,
                    ActiveBorderThickness,
                    ActiveBorderColor,
                    InactiveBorderThickness,
                    InactiveBorderColor,
                    ColorizeByMask);
                ModPlus.TabColorizer.Colorize();
            }
#endif
        }

        private void RaiseColorizePreviewPropertiesChanged()
        {
            OnPropertyChanged(nameof(ColorizePreviewFirstTabBackground));
            OnPropertyChanged(nameof(ColorizePreviewSecondTabBackground));
            OnPropertyChanged(nameof(ColorizePreviewThirdTabBackground));
            OnPropertyChanged(nameof(ColorizePreviewActiveBorderThickness));
            OnPropertyChanged(nameof(ColorizePreviewInactiveBorderThickness));
            OnPropertyChanged(nameof(ColorizePreviewFirstBorderBrush));
            OnPropertyChanged(nameof(ColorizePreviewSecondBorderBrush));
            OnPropertyChanged(nameof(ColorizePreviewThirdBorderBrush));
        }
    }
}
