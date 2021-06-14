namespace ModPlus_Revit.Services
{
#if !R2017 && !R2018
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Events;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Events;
    using Enums;
    using Models;
    using ModPlusAPI;
    using ModPlusAPI.Windows;
    using UIFramework;
    using Xceed.Wpf.AvalonDock.Controls;
    using Xceed.Wpf.AvalonDock.Layout;
    using Color = System.Windows.Media.Color;

    /// <summary>
    /// Сервис раскрашивания вкладок открытых видов
    /// </summary>
    public class TabColorizer
    {
        private readonly UIApplication _uiApplication;
        private readonly UIControlledApplication _uiControlledApplication;
        private readonly Dictionary<long, string> _documentColors;
        private readonly SolidColorBrush _defBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#cccccc");
        private List<ColorizeScheme> _colorSchemes;
        private bool _colorize;
        private string _schemeName;
        private TabColorizeZone _colorizeZone;
        private int _borderThickness;
        private bool _wasSetToTrue;
        private int _activeBorderThickness;
        private Color _activeBorderColor;
        private int _inactiveBorderThickness;
        private Color _inactiveBorderColor;
        private bool _colorizeByMask;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabColorizer"/> class.
        /// </summary>
        /// <param name="uiApplication"><see cref="UIApplication"/></param>
        /// <param name="uiControlledApplication"><see cref="UIControlledApplication"/></param>
        public TabColorizer(UIApplication uiApplication, UIControlledApplication uiControlledApplication)
        {
            _uiApplication = uiApplication;
            _uiControlledApplication = uiControlledApplication;
            _documentColors = new Dictionary<long, string>();
        }

        /// <summary>
        /// Возвращает цветовые схемы. Ключ - целое число, являющееся  
        /// </summary>
        public List<ColorizeScheme> GetColorSchemes()
        {
            var schemes = new List<ColorizeScheme>();
            schemes.AddRange(GetDefaultColorSchemes());
            
            try
            {
                schemes.AddRange(GetCustomColorSchemes());
                LoadColorSchemeDocumentTitleMasks(schemes);
            }
            catch (Exception exception)
            {
                ExceptionBox.Show(exception);
            }

            return schemes;
        }

        /// <summary>
        /// Установить состояние работы сервиса
        /// </summary>
        /// <param name="colorize">Выполнять ли раскраску вкладок</param>
        /// <param name="schemeName">Имя цветовой схемы</param>
        /// <param name="colorizeZone">Зона раскраски</param>
        /// <param name="borderThickness">Толщина границы для зон раскраски Border...</param>
        /// <param name="activeBorderThickness">Толщина границ активной вкладки при сплошной заливке фона</param>
        /// <param name="activeBorderColor">Цвет границ активной вкладки при сплошной заливке фона</param>
        /// <param name="inactiveBorderThickness">Толщина границ неактивной вкладки при сплошной заливке фона</param>
        /// <param name="inactiveBorderColor">Цвет границ неактивной вкладки при сплошной заливке фона</param>
        /// <param name="colorizeByMask">Выполнить раскраску по маске имени документа</param>
        public void SetState(
            bool colorize,
            string schemeName,
            TabColorizeZone colorizeZone,
            int borderThickness,
            int activeBorderThickness,
            Color activeBorderColor,
            int inactiveBorderThickness,
            Color inactiveBorderColor,
            bool colorizeByMask)
        {
            _colorizeByMask = colorizeByMask;
            _inactiveBorderColor = inactiveBorderColor;
            _inactiveBorderThickness = inactiveBorderThickness;
            _activeBorderColor = activeBorderColor;
            _activeBorderThickness = activeBorderThickness;
            _documentColors.Clear();
            _colorSchemes = GetColorSchemes();
            _borderThickness = borderThickness;
            _colorizeZone = colorizeZone;
            _schemeName = !string.IsNullOrEmpty(schemeName) ? schemeName : "Happy Like Pastels";
            _colorize = colorize;
            if (colorize && !_wasSetToTrue)
            {
                _uiApplication.ViewActivated += UiApplicationOnViewActivated;
                _uiApplication.Application.DocumentOpened += (sender, args) => Colorize();
                _uiApplication.Application.DocumentCreated += (sender, args) => Colorize();
                _uiApplication.Application.DocumentSaving += ApplicationOnDocumentSaving;
                _uiApplication.Application.DocumentSavingAs += ApplicationOnDocumentSavingAs;
                _uiApplication.Application.DocumentSavedAs += ApplicationOnDocumentSavedAs;
                _uiApplication.Application.DocumentSaved += ApplicationOnDocumentSaved;
                _uiApplication.Application.DocumentClosing += ApplicationOnDocumentClosing;
            }

            if (colorize)
            {
                _wasSetToTrue = true;
            }
        }

        /// <summary>
        /// Выполнить раскраску согласно установленному состоянию
        /// </summary>
        public void Colorize()
        {
            if (!_wasSetToTrue)
                return;

            var documentPaneGroupControl = GetDocumentPaneGroupControl(_uiApplication);
            var documentPanes = GetDocumentPanes(documentPaneGroupControl);

            // При тестировании выяснилось, что в documentPanes количество элементов всегда 1
            foreach (var documentPaneControl in documentPanes)
            {
                var documentTabs = GetDocumentTabs(documentPaneControl).ToList();

                foreach (Document document in _uiApplication.Application.Documents)
                {
                    if (document.IsLinked)
                        continue;

                    var documentPointer = GetDocumentPointer(document);
                    SolidColorBrush brush = null;

                    if (_documentColors.ContainsKey(documentPointer))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(_documentColors[documentPointer]));
                    }
                    else if (_colorize)
                    {
                        var colorScheme = _colorSchemes.FirstOrDefault(s => s.Name == _schemeName);
                        if (colorScheme != null)
                        {
                            if (_colorizeByMask)
                            {
                                if (colorScheme.IsValidDocumentTitle(document.Title, out var color))
                                {
                                    brush = new SolidColorBrush(color);

                                    if (_documentColors.ContainsKey(documentPointer))
                                        _documentColors[documentPointer] = color.ToString();
                                    else
                                        _documentColors.Add(documentPointer, color.ToString());
                                }
                            }
                            else
                            {
                                foreach (var colorRule in colorScheme.ColorRules
                                    .Where(colorRule => !_documentColors.ContainsValue(colorRule.Color.ToString())))
                                {
                                    brush = new SolidColorBrush(colorRule.Color);

                                    if (_documentColors.ContainsKey(documentPointer))
                                        _documentColors[documentPointer] = colorRule.Color.ToString();
                                    else
                                        _documentColors.Add(documentPointer, colorRule.Color.ToString());

                                    break;
                                }
                            }
                        }
                    }

                    foreach (var tabItem in documentTabs.Where(tabItem => GetTabDocumentId(tabItem) == documentPointer))
                    {
                        if (_colorize && brush != null)
                        {
                            ColorizeTab(tabItem, brush);
                        }
                        else
                        {
                            if (tabItem.IsSelected)
                            {
                                tabItem.BorderBrush = Brushes.White;
                                tabItem.Background = Brushes.White;
                                tabItem.OpacityMask = Brushes.White;
                            }
                            else
                            {
                                tabItem.BorderBrush = _defBrush;
                                tabItem.Background = _defBrush;
                                tabItem.OpacityMask = _defBrush;
                            }
                        }
                    }
                }
            }
        }

        private void ColorizeTab(TabItem tabItem, Brush brush)
        {
            if (_colorizeZone == TabColorizeZone.Background)
            {
                tabItem.BorderBrush = brush;
                tabItem.Background = brush;
                tabItem.OpacityMask = brush;

                if (tabItem.IsSelected)
                {
                    tabItem.BorderThickness = new Thickness(_activeBorderThickness);
                    tabItem.BorderBrush = new SolidColorBrush(_activeBorderColor);
                }
                else
                {
                    tabItem.BorderThickness = new Thickness(_inactiveBorderThickness);
                    tabItem.BorderBrush = new SolidColorBrush(_inactiveBorderColor);
                }
            }
            else
            {
                if (tabItem.IsSelected)
                {
                    tabItem.Background = Brushes.White;
                    tabItem.OpacityMask = Brushes.White;
                }
                else
                {
                    tabItem.Background = _defBrush;
                    tabItem.OpacityMask = _defBrush;
                }

                if (_colorizeZone == TabColorizeZone.BorderLeft)
                    tabItem.BorderThickness = new Thickness(_borderThickness, 0, 0, 0);
                else if (_colorizeZone == TabColorizeZone.BorderBottom)
                    tabItem.BorderThickness = new Thickness(0, 0, 0, _borderThickness);
                else if (_colorizeZone == TabColorizeZone.BorderTop)
                    tabItem.BorderThickness = new Thickness(0, _borderThickness, 0, 0);

                tabItem.BorderBrush = brush;
            }
        }

        private void ApplicationOnDocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            var documentPointer = GetDocumentPointer(e.Document);
            if (_documentColors.ContainsKey(documentPointer))
                _documentColors.Remove(documentPointer);
        }
        
        private void ApplicationOnDocumentSavingAs(object sender, DocumentSavingAsEventArgs e)
        {
            _uiControlledApplication.Idling += UiControlledApplicationOnIdling;
        }

        private void ApplicationOnDocumentSaving(object sender, DocumentSavingEventArgs e)
        {
            _uiControlledApplication.Idling += UiControlledApplicationOnIdling;
        }
        
        private void ApplicationOnDocumentSaved(object sender, DocumentSavedEventArgs e)
        {
            _uiControlledApplication.Idling += UiControlledApplicationOnIdling;
        }
        
        private void ApplicationOnDocumentSavedAs(object sender, DocumentSavedAsEventArgs e)
        {
            _uiControlledApplication.Idling += UiControlledApplicationOnIdling;
        }

        private void UiApplicationOnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            _uiControlledApplication.Idling += UiControlledApplicationOnIdling;
        }

        private void UiControlledApplicationOnIdling(object sender, IdlingEventArgs e)
        {
            try
            {
                Colorize();
            }
            finally
            {
                _uiControlledApplication.Idling -= UiControlledApplicationOnIdling;
            }
        }

        private LayoutDocumentPaneGroupControl GetDocumentPaneGroupControl(UIApplication uiApplication)
        {
            var windowRoot = GetWindowRoot(uiApplication);
            return windowRoot != null
                ? MainWindow.FindFirstChild<LayoutDocumentPaneGroupControl>((MainWindow)windowRoot)
                : null;
        }

        private IEnumerable<LayoutDocumentPaneControl> GetDocumentPanes(LayoutDocumentPaneGroupControl docTabGroup)
        {
            return docTabGroup != null
                ? docTabGroup.FindVisualChildren<LayoutDocumentPaneControl>()
                : new List<LayoutDocumentPaneControl>();
        }

        private long GetDocumentPointer(Document doc)
        {
            var m = doc.GetType().GetMethod("getMFCDoc", BindingFlags.Instance | BindingFlags.NonPublic);
            var obj = m?.Invoke(doc, null);
            m = obj?.GetType().GetMethod("GetPointerValue", BindingFlags.Instance | BindingFlags.NonPublic);

            // ReSharper disable once PossibleNullReferenceException
            return ((IntPtr)m.Invoke(obj, null)).ToInt64();
        }

        private IEnumerable<TabItem> GetDocumentTabs(LayoutDocumentPaneControl docPane)
        {
            return docPane != null
                ? docPane.FindVisualChildren<TabItem>()
                : new List<TabItem>();
        }

        private long GetTabDocumentId(TabItem tab)
        {
            return ((MFCMDIFrameHost)((MFCMDIChildFrameControl)((LayoutDocument)tab.Content).Content).Content).document.ToInt64();
        }

        private Visual GetWindowRoot(UIApplication uiApplication)
        {
            try
            {
                return HwndSource.FromHwnd(uiApplication.MainWindowHandle)?.RootVisual;
            }
            catch
            {
                return null;
            }
        }

        private IEnumerable<ColorizeScheme> GetDefaultColorSchemes()
        {
            foreach (var s in new Dictionary<string, string[]>
            {
                //// https://www.schemecolor.com/rainbow-pastels-color-scheme.php
                { "Rainbow Pastels", new[] { "#FFB7B2", "#E2F0CB", "#C7CEEA", "#B5EAD7", "#FFDAC1", "#FF9AA2" } },
                //// https://www.schemecolor.com/blissful-june.php
                { "Blissful June", new[] { "#9BC6D3", "#A6D6C3", "#CBDDC3", "#E8E3CC", "#EAE8DF", "#F5D3D6" } },
                //// https://www.schemecolor.com/endless-color.php
                { "Endless Color", new[] { "#0A39E7", "#00C313", "#FFDD00", "#FF3100", "#7E0095", "#FF00A2" } },
                //// https://www.schemecolor.com/pink-blue-pastels-set.php
                { "Pink & Blue Pastels Set", new[] { "#FEE9F0", "#FDD5E8", "#B9CBED", "#C8E4F4" } },
                //// https://www.schemecolor.com/bright-advice.php
                { "Bright Advice", new[] { "#FFD83B", "#FF6B66", "#611ACA", "#E53271", "#3299FF" } },
                //// https://www.schemecolor.com/dullest-pastels.php
                { "Dullest Pastels", new[] { "#7DB08D", "#897AAB", "#B98AAA", "#CAAEA4", "#D5CEBD", "#9DB6C6" } },
                //// https://www.schemecolor.com/happy-like-pastels.php
                { "Happy Like Pastels", new[] { "#F9DEDF", "#9ED2F6", "#F9DFC7", "#F6F5F0", "#9DDCE0", "#B8AFEF" } },
                //// https://www.schemecolor.com/indian-festive-colors.php
                { "Indian Festive Colors", new[] { "#DC26D2", "#FFCA05", "#2600DC", "#EE2D26" } }
            })
            {
                var colorizeScheme = new ColorizeScheme(s.Key, false);
                colorizeScheme.AddColors(s.Value);
                yield return colorizeScheme;
            }
        }

        private IEnumerable<ColorizeScheme> GetCustomColorSchemes()
        {
            var xElement = UserConfigFile.ConfigFileXml.Element("CustomColorizeSchemes");
            if (xElement == null)
                yield break;
            
            foreach (var element in xElement.Elements("ColorizeScheme"))
            {
                var split = element.Value.Split(',');
                if (split.Length <= 1)
                    continue;
                
                var name = split[0];
                var colors = new List<Color>();

                foreach (var s in split.Skip(1))
                {
                    try
                    {
                        if (ColorConverter.ConvertFromString(s) is Color color)
                        {
                            colors.Add(color);
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }

                if (colors.Any())
                {
                    var colorScheme = new ColorizeScheme(name, true);
                    foreach (var color in colors)
                    {
                        colorScheme.ColorRules.Add(new ColorRule(color));
                    }

                    yield return colorScheme;
                }
            }
        }

        private void LoadColorSchemeDocumentTitleMasks(IReadOnlyCollection<ColorizeScheme> schemes)
        {
            var xElement = UserConfigFile.ConfigFileXml.Element("CustomColorizeSchemeTitleMasks");
            if (xElement == null)
                return;
            
            foreach (var element in xElement.Elements("ColorizeSchemeMask"))
            {
                var split = element.Value.Split('|');
                if (split.Length <= 1)
                    continue;
                var name = split[0];
                var scheme = schemes.FirstOrDefault(s => s.Name == name);
                if (scheme == null)
                    continue;
                
                var masks = split.Skip(1).ToList();
                for (var i = 0; i < masks.Count; i++)
                {
                    if (scheme.ColorRules.Count > i)
                        scheme.ColorRules[i].DocumentNameMask = masks[i];
                }
            }
        }
    }
#endif
}
