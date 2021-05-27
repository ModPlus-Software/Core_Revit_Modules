namespace ModPlus_Revit.Utils
{
    using System;
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
    }
}
