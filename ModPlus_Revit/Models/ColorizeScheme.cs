namespace ModPlus_Revit.Models
{
    using System.Collections.Generic;
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
        public ColorizeScheme(string name)
        {
            Name = name;
            Brushes = new List<Brush>();
        }
        
        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Кисти заливки сплошным цветом
        /// </summary>
        public List<Brush> Brushes { get; }
    }
}
