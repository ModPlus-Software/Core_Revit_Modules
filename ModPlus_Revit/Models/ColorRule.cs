namespace ModPlus_Revit.Models
{
    using System.Windows.Media;
    using ModPlusAPI.Mvvm;

    /// <summary>
    /// Правило назначения цвета
    /// </summary>
    public class ColorRule : ObservableObject
    {
        private Color _color;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRule"/> class.
        /// </summary>
        /// <param name="color">Цвет</param>
        public ColorRule(Color color)
        {
            Color = color;
            DocumentNameMask = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorRule"/> class.
        /// </summary>
        /// <param name="color">Цвет</param>
        /// <param name="documentNameMask">Маска имени документа</param>
        public ColorRule(Color color, string documentNameMask)
        {
            Color = color;
            DocumentNameMask = documentNameMask;
        }

        /// <summary>
        /// Цвет
        /// </summary>
        public Color Color
        {
            get => _color;
            set
            {
                if (_color == value)
                    return;
                _color = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Маска имени документа
        /// </summary>
        public string DocumentNameMask { get; set; }
    }
}
