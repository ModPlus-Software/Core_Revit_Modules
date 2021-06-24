namespace ModPlus_Revit.Utils
{
    using System;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Structure;
    using Enums;
    using JetBrains.Annotations;

    /// <summary>
    /// Утилиты армирования
    /// </summary>
    [PublicAPI]
    public static class RebarUtils
    {
        /// <summary>
        /// Возвращает модельный диаметр с учетом различий версий API
        /// </summary>
        /// <param name="rebarBarType"><see cref="RebarBarType"/></param>
        /// <param name="diameterType">Тип диаметра</param>
        public static double GetDiameter(this RebarBarType rebarBarType, DiameterType diameterType)
        {
#if R2017 || R2018 || R2019 || R2020 || R2021
            return rebarBarType.BarDiameter;
#else
            return diameterType == DiameterType.Model ? rebarBarType.BarModelDiameter : rebarBarType.BarNominalDiameter;
#endif
        }

        /// <summary>
        /// Возвращает номинальный диаметр арматуры, полученный из <see cref="RebarBarType"/>, в миллиметрах с
        /// округлением до одного знака после запятой в сторону четного числа
        /// </summary>
        /// <remarks>Данный метод подходит только для использования с номинальным диаметром!</remarks>
        /// <param name="rbt">Instance of <see cref="RebarBarType"/></param>
        public static double GetNominalDiameterInMm(this RebarBarType rbt)
        {
#if R2017 || R2018 || R2019 || R2020 || R2021
            var d = rbt.BarDiameter.FtToMm();
#else
            var d = rbt.GetDiameter(DiameterType.Nominal).FtToMm();
#endif
            return Math.Round(d, 1, MidpointRounding.ToEven);
        }

        /// <summary>
        /// Устанавливает диаметры с учетом различий версий API. Для Revit версии 2017-2021 используется значение
        /// переменной nominalDiameter
        /// </summary>
        /// <param name="rebarBarType"><see cref="RebarBarType"/></param>
        /// <param name="nominalDiameter">Номинальный диаметр, футы</param>
        /// <param name="modelDiameter">Модельный диаметр, футы</param>
        public static void SetDiameters(this RebarBarType rebarBarType, double nominalDiameter, double modelDiameter)
        {
#if R2017 || R2018 || R2019 || R2020 || R2021
            rebarBarType.BarDiameter = nominalDiameter;
#else
            rebarBarType.BarNominalDiameter = nominalDiameter;
            rebarBarType.BarModelDiameter = modelDiameter;
#endif
        }

        /// <summary>
        /// Задние типа компоновки арматурного стержня "Число с шагом"
        /// <para>Метод должен вызываться в транзакции</para>
        /// <para>Метод учитывает разницу в API</para>
        /// </summary>
        /// <param name="rebar">Арматурный стержень <see cref="Rebar"/></param>
        /// <param name="count">Число стержней в массиве</param>
        /// <param name="step">Шаг стержней</param>
        /// <param name="barsOnNormalSide">Определяет, находятся ли стержни набора арматуры на одной стороне плоскости
        /// арматуры, обозначенной нормалью</param>
        /// <param name="includeFirstBar">Определяет, показан ли первый прут в наборе арматуры</param>
        /// <param name="includeLastBar">Определяет, показан ли последний прут в наборе арматуры</param>
        public static void SetLayoutAsNumberWithSpacing(
            this Rebar rebar,
            int count,
            double step,
            bool barsOnNormalSide = true,
            bool includeFirstBar = true,
            bool includeLastBar = true)
        {
#if R2017
            rebar.SetLayoutAsNumberWithSpacing(count, step, barsOnNormalSide, includeFirstBar, includeLastBar);
#else
            rebar.GetShapeDrivenAccessor()
                .SetLayoutAsNumberWithSpacing(count, step, barsOnNormalSide, includeFirstBar, includeLastBar);
#endif
        }

        /// <summary>
        /// Задание типа компоновки арматурного "Единичный"
        /// <para>Метод должен вызываться в транзакции</para>
        /// <para>Метод учитывает разницу в API</para>
        /// </summary> 
        /// <param name="rebar">Арматурный стержень <see cref="Rebar"/></param>
        public static void SetLayoutAsSingle(this Rebar rebar)
        {
#if R2017
            rebar.SetLayoutAsSingle();
#else
            rebar.GetShapeDrivenAccessor().SetLayoutAsSingle();
#endif
        }

        /// <summary>
        /// Задание типа компоновки арматурного "Фиксированное число"
        /// <para>Метод должен вызываться в транзакции</para>
        /// <para>Метод учитывает разницу в API</para>
        /// </summary> 
        /// <param name="rebar">Арматурный стержень <see cref="Rebar"/></param>
        /// <param name="numberOfBarPositions">Количество позиций стержней в наборе арматуры</param>
        /// <param name="arrayLength">Длина распределения арматурного набора</param>
        /// <param name="barsOnNormalSide">Определяет, находятся ли стержни набора арматуры на одной стороне плоскости
        /// арматуры, обозначенной нормалью</param>
        /// <param name="includeFirstBar">Определяет, показан ли первый прут в наборе арматуры</param>
        /// <param name="includeLastBar">Определяет, показан ли последний прут в наборе арматуры</param>
        public static void SetLayoutAsFixedNumber(
            this Rebar rebar,
            int numberOfBarPositions,
            double arrayLength,
            bool barsOnNormalSide = true,
            bool includeFirstBar = true,
            bool includeLastBar = true)
        {
#if R2017
            rebar.SetLayoutAsFixedNumber(numberOfBarPositions, arrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
#else
            rebar.GetShapeDrivenAccessor()
                .SetLayoutAsFixedNumber(numberOfBarPositions, arrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
#endif
        }

        /// <summary>
        /// Задание типа компоновки арматурного "Максимальное расстояние"
        /// <para>Метод должен вызываться в транзакции</para>
        /// <para>Метод учитывает разницу в API</para>
        /// </summary>
        /// <param name="rebar">Арматурный стержень <see cref="Rebar"/></param>
        /// <param name="spacing">Максимальное расстояние между арматурой в арматурном наборе</param>
        /// <param name="arrayLength">Длина распределения арматурного набора</param>
        /// <param name="barsOnNormalSide">Определяет, находятся ли стержни набора арматуры на одной стороне плоскости
        /// арматуры, обозначенной нормалью</param>
        /// <param name="includeFirstBar">Определяет, показан ли первый прут в наборе арматуры</param>
        /// <param name="includeLastBar">Определяет, показан ли последний прут в наборе арматуры</param>
        public static void SetLayoutAsMaximumSpacing(
            this Rebar rebar,
            double spacing,
            double arrayLength,
            bool barsOnNormalSide = true,
            bool includeFirstBar = true,
            bool includeLastBar = true)
        {
#if R2017
            rebar.SetLayoutAsMaximumSpacing(spacing, arrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
#else
            rebar.GetShapeDrivenAccessor()
                .SetLayoutAsMaximumSpacing(spacing, arrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
#endif
        }

        /// <summary>
        /// Задание типа компоновки арматурного "Минимальное расстояние в свету"
        /// <para>Метод должен вызываться в транзакции</para>
        /// <para>Метод учитывает разницу в API</para>
        /// </summary>
        /// <param name="rebar">Арматурный стержень <see cref="Rebar"/></param>
        /// <param name="spacing">Максимальное расстояние между арматурой в арматурном наборе</param>
        /// <param name="arrayLength">Длина распределения арматурного набора</param>
        /// <param name="barsOnNormalSide">Определяет, находятся ли стержни набора арматуры на одной стороне плоскости
        /// арматуры, обозначенной нормалью</param>
        /// <param name="includeFirstBar">Определяет, показан ли первый прут в наборе арматуры</param>
        /// <param name="includeLastBar">Определяет, показан ли последний прут в наборе арматуры</param>
        public static void SetLayoutAsMinimumClearSpacing(
            this Rebar rebar,
            double spacing,
            double arrayLength,
            bool barsOnNormalSide = true,
            bool includeFirstBar = true,
            bool includeLastBar = true)
        {
#if R2017
            rebar.SetLayoutAsMinimumClearSpacing(spacing, arrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
#else
            rebar.GetShapeDrivenAccessor()
                .SetLayoutAsMinimumClearSpacing(spacing, arrayLength, barsOnNormalSide, includeFirstBar, includeLastBar);
#endif
        }

        /// <summary>
        /// Возвращает идентификатор формы арматурного стержня с учетом различий в Revit API
        /// <para>Метод должен вызываться в транзакции</para>
        /// </summary>
        /// <param name="rebar">Арматурный стержень <see cref="Rebar"/></param>
        public static ElementId GetShapeId(this Rebar rebar)
        {
#if R2017
            return rebar.RebarShapeId;
#else
            return rebar.GetShapeId();
#endif
        }
        
        /// <summary>
        /// Устанавливает форму арматурного стержня по идентификатору с учетом различий в Revit API.
        /// <para>Метод должен вызываться в транзакции</para>
        /// </summary>
        /// <param name="rebar">Арматурный стержень <see cref="Rebar"/></param>
        /// <param name="shapeId">Идентификатор формы арматурного стержня <see cref="ElementId"/></param>
        public static void SetShapeId(this Rebar rebar, ElementId shapeId)
        {
#if R2017
            rebar.RebarShapeId = shapeId;
#else
            rebar.GetShapeDrivenAccessor().SetRebarShapeId(shapeId);
#endif
        }
        
        /// <summary>
        /// Получение нормали арматурного стержня с учетом различий в API
        /// </summary>
        /// <param name="rebar">Арматурный стержень <see cref="Rebar"/></param>
        public static XYZ GetRebarNormal(this Rebar rebar)
        {
#if R2017
            return rebar.Normal;
#else
            return rebar.GetShapeDrivenAccessor().Normal;
#endif
        }
    }
}
