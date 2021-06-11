namespace ModPlus_Revit.Services
{
#if !R2017 && !R2018
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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
    using UIFramework;
    using Xceed.Wpf.AvalonDock.Controls;
    using Xceed.Wpf.AvalonDock.Layout;

    /// <summary>
    /// Сервис раскрашивания вкладок открытых видов
    /// </summary>
    public class TabColorizer
    {
        private readonly UIApplication _uiApplication;
        private readonly Dictionary<long, Brush> _documentBrushes;
        private readonly List<ColorizeScheme> _colorSchemes;
        private readonly SolidColorBrush _defBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#cccccc");
        private bool _colorize;
        private string _schemeName;
        private TabColorizeZone _colorizeZone;
        private int _borderThickness;
        private bool _wasSetToTrue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TabColorizer"/> class.
        /// </summary>
        /// <param name="uiApplication"><see cref="UIApplication"/></param>
        public TabColorizer(UIApplication uiApplication)
        {
            _uiApplication = uiApplication;
            _documentBrushes = new Dictionary<long, Brush>();
            _colorSchemes = GetColorSchemes();
        }

        /// <summary>
        /// Возвращает цветовые схемы. Ключ - целое число, являющееся  
        /// </summary>
        public List<ColorizeScheme> GetColorSchemes()
        {
            var brushConverter = new BrushConverter();
            return new List<ColorizeScheme>
            {
                new ColorizeScheme("Rainbow Pastels")
                {
                    // https://www.schemecolor.com/rainbow-pastels-color-scheme.php
                    Brushes =
                    {
                        (SolidColorBrush)brushConverter.ConvertFrom("#FFB7B2"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#E2F0CB"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#C7CEEA"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#B5EAD7"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#FFDAC1"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#FF9AA2"),
                    }
                },
                new ColorizeScheme("Blissful June")
                {
                    // https://www.schemecolor.com/blissful-june.php
                    Brushes =
                    {
                        (SolidColorBrush)brushConverter.ConvertFrom("#9BC6D3"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#A6D6C3"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#CBDDC3"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#E8E3CC"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#EAE8DF"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#F5D3D6"),
                    }
                },
                new ColorizeScheme("Endless Color")
                {
                    // https://www.schemecolor.com/endless-color.php
                    Brushes =
                    {
                        (SolidColorBrush)brushConverter.ConvertFrom("#0A39E7"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#00C313"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#FFDD00"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#FF3100"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#7E0095"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#FF00A2"),
                    }
                },
                new ColorizeScheme("Pink & Blue Pastels Set")
                {
                    // https://www.schemecolor.com/pink-blue-pastels-set.php
                    Brushes =
                    {
                        (SolidColorBrush)brushConverter.ConvertFrom("#FEE9F0"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#FDD5E8"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#B9CBED"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#C8E4F4"),
                    }
                },
                new ColorizeScheme("Bright Advice")
                {
                    // https://www.schemecolor.com/bright-advice.php
                    Brushes =
                    {
                        (SolidColorBrush)brushConverter.ConvertFrom("#FFD83B"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#FF6B66"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#611ACA"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#E53271"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#3299FF"),
                    }
                },
                new ColorizeScheme("Dullest Pastels")
                {
                    // https://www.schemecolor.com/dullest-pastels.php
                    Brushes =
                    {
                        (SolidColorBrush)brushConverter.ConvertFrom("#7DB08D"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#897AAB"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#B98AAA"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#CAAEA4"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#D5CEBD"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#9DB6C6"),
                    }
                },
                new ColorizeScheme("Happy Like Pastels")
                {
                    // https://www.schemecolor.com/happy-like-pastels.php
                    Brushes =
                    {
                        (SolidColorBrush)brushConverter.ConvertFrom("#F9DEDF"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#9ED2F6"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#F9DFC7"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#F6F5F0"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#9DDCE0"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#B8AFEF"),
                    }
                },
                new ColorizeScheme("Indian Festive Colors")
                {
                    // https://www.schemecolor.com/indian-festive-colors.php
                    Brushes =
                    {
                        (SolidColorBrush)brushConverter.ConvertFrom("#DC26D2"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#FFCA05"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#2600DC"),
                        (SolidColorBrush)brushConverter.ConvertFrom("#EE2D26"),
                    }
                },
            };
        }

        /// <summary>
        /// Установить состояние работы сервиса
        /// </summary>
        /// <param name="colorize">Выполнять ли раскраску вкладок</param>
        /// <param name="schemeName">Имя цветовой схемы</param>
        /// <param name="colorizeZone">Зона раскраски</param>
        /// <param name="borderThickness">Толщина границы для зон раскраски Border...</param>
        public void SetState(bool colorize, string schemeName, TabColorizeZone colorizeZone, int borderThickness)
        {
            _documentBrushes.Clear();
            _borderThickness = borderThickness;
            _colorizeZone = colorizeZone;
            _schemeName = !string.IsNullOrEmpty(schemeName) ? schemeName : "Happy Like Pastels";
            _colorize = colorize;
            if (colorize && !_wasSetToTrue)
            {
                _uiApplication.ViewActivated += UIApplicationOnViewActivated;
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
                    Brush brush = null;

                    if (_documentBrushes.ContainsKey(documentPointer))
                    {
                        brush = _documentBrushes[documentPointer];
                    }
                    else if (_colorize)
                    {
                        var colorScheme = _colorSchemes.FirstOrDefault(s => s.Name == _schemeName);
                        if (colorScheme != null)
                        {
                            foreach (var schemeBrush in colorScheme.Brushes
                                .Where(schemeBrush => !_documentBrushes.ContainsValue(schemeBrush)))
                            {
                                brush = schemeBrush;

                                if (_documentBrushes.ContainsKey(documentPointer))
                                    _documentBrushes[documentPointer] = brush;
                                else
                                    _documentBrushes.Add(documentPointer, brush);

                                break;
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

        private void UIApplicationOnViewActivated(object sender, ViewActivatedEventArgs e)
        {
            Colorize();
        }

        private void ApplicationOnDocumentClosing(object sender, DocumentClosingEventArgs e)
        {
            var documentPointer = GetDocumentPointer(e.Document);
            if (_documentBrushes.ContainsKey(documentPointer))
                _documentBrushes.Remove(documentPointer);
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
    }
#endif
}
