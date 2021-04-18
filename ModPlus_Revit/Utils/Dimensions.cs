namespace ModPlus_Revit.Utils
{
    using System;
    using Autodesk.Revit.DB;
    using JetBrains.Annotations;

    /// <summary>
    /// Утилиты для работы с размерами
    /// </summary>
    [PublicAPI]
    public static class Dimensions
    {
        /// <summary>
        /// Удаление нулей из размерной цепочки. В случае, если нулей не найдено, возвращает False. Иначе - True и
        /// массив <see cref="Reference"/> для пересоздания размерной цепочки без нулей
        /// </summary>
        /// <param name="dimension">Проверяемая размерная цепочка</param>
        /// <param name="referenceArray">Массив <see cref="Reference"/> для пересоздания размерной цепочки</param>
        /// <returns>True - размерная цепочка имела нули и требуется пересоздать её. Иначе false</returns>
        public static bool TryRemoveZeroes(Dimension dimension, out ReferenceArray referenceArray)
        {
            referenceArray = new ReferenceArray();
            var doc = dimension.Document;
        
            if (dimension.Segments.IsEmpty)
                return false;

            for (var i = 0; i < dimension.NumberOfSegments; ++i)
            {
                var segment = dimension.Segments.get_Item(i);

                var value = segment.Value;
                if (value.HasValue && Math.Abs(value.Value) < 0.0001)
                    continue;
                
                if (referenceArray.IsEmpty)
                    referenceArray.Append(dimension.References.get_Item(i).FixGridReference(doc));
 
                referenceArray.Append(dimension.References.get_Item(i + 1).FixGridReference(doc));
            }
 
            return referenceArray.Size < dimension.References.Size;
        }

        private static Reference FixGridReference(this Reference reference, Document doc)
        {
            return doc.GetElement(reference) is Grid grid ? new Reference(grid) : reference;
        }
    }
}
