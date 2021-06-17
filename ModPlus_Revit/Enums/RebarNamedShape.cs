namespace ModPlus_Revit.Enums
{
    /// <summary>
    /// Именованная форма арматурного стержня
    /// </summary>
    public enum RebarNamedShape
    {
        /// <summary>
        /// Прямой стержень
        /// </summary>
        Straight,
        
        /// <summary>
        /// Прямой продольно гнутый стержень ("Бутылочка")
        /// </summary>
        Bottle,
        
        /// <summary>
        /// L-Образный стержень
        /// </summary>
        LShaped,
        
        /// <summary>
        /// L-Образный стержень с большим загибом
        /// </summary>
        LShapedWithBigBend,
        
        /// <summary>
        /// П-Образный стержень
        /// </summary>
        UShaped,
        
        /// <summary>
        /// С-Образный стержень с крюками в эскизе
        /// </summary>
        CShapedWithSketchHook,
        
        /// <summary>
        /// С-Образный стержень с крюками Revit
        /// </summary>
        CShapedWithRevithHook,
        
        /// <summary>
        /// S-Образный стержень с крюками в эскизе
        /// </summary>
        SShapedWithSketchHook,
        
        /// <summary>
        /// S-Образный стержень с крюками Revit
        /// </summary>
        SShapedWithRevitHook,
        
        /// <summary>
        /// Обвязочный (прямоугольный) хомут
        /// </summary>
        Girth,
        
        /// <summary>
        /// Круглый стержень
        /// </summary>
        Circle,
        
        /// <summary>
        /// Спиралевидный стержень
        /// </summary>
        Spiral,
        
        /// <summary>
        /// Треугольные
        /// </summary>
        Triangular
    }
}
