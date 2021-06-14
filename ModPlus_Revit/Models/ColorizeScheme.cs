namespace ModPlus_Revit.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows.Media;

    /// <summary>
    /// Цветовая схема раскраски
    /// </summary>
    public class ColorizeScheme
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorizeScheme"/> class.
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="isEnabledToModify">Доступность редактирования</param>
        public ColorizeScheme(string name, bool isEnabledToModify)
        {
            Name = name;
            IsEnabledToModify = isEnabledToModify;
            ColorRules = new ObservableCollection<ColorRule>();
            ColorRules.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (ColorRule newItem in args.NewItems)
                    {
                        newItem.PropertyChanged += (o, eventArgs) =>
                        {
                            if (eventArgs.PropertyName == nameof(ColorRule.Color))
                                ColorChanged?.Invoke(this, EventArgs.Empty);
                        };
                    }
                }
            };
        }

        /// <summary>
        /// Изменился цвет в списке цветов
        /// </summary>
        public event EventHandler ColorChanged;

        /// <summary>
        /// Доступность редактирования
        /// </summary>
        public bool IsEnabledToModify { get; }

        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Правила назначения цвета
        /// </summary>
        public ObservableCollection<ColorRule> ColorRules { get; }

        /// <summary>
        /// Добавляет в коллекцию <see cref="ColorRules"/> цвета из списка HEX кодов без маски имени документа
        /// </summary>
        /// <param name="hexCodes">Список HEX кодов</param>
        public void AddColors(string[] hexCodes)
        {
            foreach (var hex in hexCodes)
            {
                if (ColorConverter.ConvertFromString(hex) is Color color)
                    ColorRules.Add(new ColorRule(color));
            }
        }

        /// <summary>
        /// Кисти заливки сплошным цветом
        /// </summary>
        public List<SolidColorBrush> GetBrushes()
        {
            return ColorRules.Select(r => new SolidColorBrush(r.Color)).ToList();
        }

        /// <summary>
        /// Возвращает кисть по индексу или null, если количество кистей меньше
        /// </summary>
        /// <param name="index">Индекс</param>
        public SolidColorBrush GetBrush(int index)
        {
            var brushes = GetBrushes();
            if (brushes != null && brushes.Count > index)
                return brushes[index];
            return null;
        }

        /// <summary>
        /// Подходит ли заголовок документа для раскраски по маске
        /// </summary>
        /// <param name="title">Заголовок</param>
        /// <param name="color">Цвет</param>
        public bool IsValidDocumentTitle(string title, out Color color)
        {
            title = title.ToUpper();
            foreach (var colorRule in ColorRules)
            {
                if (string.IsNullOrWhiteSpace(colorRule.DocumentNameMask))
                    continue;
                if (title.Contains(colorRule.DocumentNameMask.ToUpper()))
                {
                    color = colorRule.Color;
                    return true;
                }
            }

            color = default;
            return false;
        }
    }
}
