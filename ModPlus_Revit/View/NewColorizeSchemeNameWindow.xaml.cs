namespace ModPlus_Revit.View
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using ModPlusStyle.Controls;

    /// <summary>
    /// Логика взаимодействия для NewColorizeSchemeNameWindow.xaml
    /// </summary>
    public partial class NewColorizeSchemeNameWindow
    {
        private readonly ModPlusWindow _parentWindow;
        private readonly List<string> _existNames;
        private readonly List<char> _invalidSymbols =
            new List<char> { '\\', ':', '{', '}', '[', ']', '|', ';', '<', '>', '?', '`', '~' };

        /// <summary>
        /// Initializes a new instance of the <see cref="NewColorizeSchemeNameWindow"/> class.
        /// </summary>
        /// <param name="parentWindow">Parent window</param>
        /// <param name="existNames">Exist color scheme names</param>
        public NewColorizeSchemeNameWindow(ModPlusWindow parentWindow, List<string> existNames)
        {
            _parentWindow = parentWindow;
            _existNames = existNames;
            Owner = parentWindow;
            InitializeComponent();
            ModPlusAPI.Language.SetLanguageProviderForResourceDictionary(Resources, "LangApi");
            Loaded += OnLoaded;
            Closed += OnClosed;
            TbName.Focus();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _parentWindow.HideOverlay();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _parentWindow.ShowOverlay();
        }

        private void BtAccept_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtCancel_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TbName_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_existNames.Any(n => n.Equals(TbName.Text, StringComparison.InvariantCultureIgnoreCase)))
            {
                TbError.Visibility = Visibility.Visible;
                
                // Имя цветовой схемы "{0}" уже используется
                TbError.Text = string.Format(ModPlusAPI.Language.GetItem("RevitDlls", "c13"), TbName.Text);
                BtAccept.IsEnabled = false;
            }
            else if (string.IsNullOrWhiteSpace(TbName.Text))
            {
                TbError.Visibility = Visibility.Collapsed;
                BtAccept.IsEnabled = false;
            }
            else
            {
                TbError.Visibility = Visibility.Collapsed;
                BtAccept.IsEnabled = true;
            }
        }

        private void TbName_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (var s in e.Text)
            {
                if (!_invalidSymbols.Contains(s)) 
                    continue;
                e.Handled = true;
                break;
            }
        }
    }
}
