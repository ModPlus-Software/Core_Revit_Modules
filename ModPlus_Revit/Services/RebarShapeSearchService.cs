namespace ModPlus_Revit.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Structure;
    using Enums;
    using JetBrains.Annotations;
    using Utils;

    /// <summary>
    /// Сервис подбора форм по уникальным свойствам геометрии
    /// </summary>
    public class RebarShapeSearchService
    {
        private readonly List<RebarShape> _allRebarShapes;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RebarShapeSearchService"/> class.
        /// </summary>
        /// <param name="doc"><see cref="Document"/></param>
        public RebarShapeSearchService(Document doc)
        {
            _allRebarShapes = new FilteredElementCollector(doc)
                .OfClass(typeof(RebarShape))
                .Where(e => e.IsValidObject)
                .Cast<RebarShape>()
                .ToList();
        }

        /// <summary>
        /// Возвращает форму арматурного стержня по имени, если такая найдена
        /// </summary>
        /// <param name="shapeName">Имя формы для поиска</param>
        [CanBeNull]
        public RebarShape GetRebarShapeByName(string shapeName)
        {
            return string.IsNullOrEmpty(shapeName)
                ? null
                : _allRebarShapes.FirstOrDefault(s => s.Name == shapeName);
        }

        /// <summary>
        /// Возвращает список форм арматурных стержней по значению <see cref="RebarNamedShape"/>
        /// </summary>
        /// <param name="rebarNamedShape"><see cref="RebarNamedShape"/></param>
        public List<RebarShape> GetRebarShapes(RebarNamedShape rebarNamedShape)
        {
            switch (rebarNamedShape)
            {
                case RebarNamedShape.Straight:
                    return _allRebarShapes.Where(s => IsValidByCurvesCount(s, 1, 0)).ToList();
                case RebarNamedShape.Bottle:
                    return _allRebarShapes.Where(IsBottle).ToList();
                case RebarNamedShape.LShaped:
                    return _allRebarShapes.Where(IsLShaped).ToList();
                case RebarNamedShape.LShapedWithBigBend:
                    return _allRebarShapes.Where(IsLShapedWithBigBend).ToList();
                case RebarNamedShape.UShaped:
                    return _allRebarShapes.Where(IsUShaped).ToList();
                case RebarNamedShape.CShapedWithSketchHook:
                    return _allRebarShapes.Where(s => IsValidByCurvesCount(s, 3, 2)).ToList();
                case RebarNamedShape.CShapedWithRevithHook:
                    return _allRebarShapes.Where(s => IsValidByCurvesCount(s, 1, 0)).ToList();
                case RebarNamedShape.SShapedWithSketchHook:
                    return _allRebarShapes.Where(s => IsValidByCurvesCount(s, 5, 2)).ToList();
                case RebarNamedShape.SShapedWithRevitHook:
                    return _allRebarShapes.Where(s => IsValidByCurvesCount(s, 3, 0)).ToList();
                case RebarNamedShape.Girth:
                    return _allRebarShapes.Where(s => IsValidByCurvesCount(s, 4, 0)).ToList();
                case RebarNamedShape.Circle:
                    return _allRebarShapes.Where(s => IsValidByCurvesCount(s, 0, 2)).ToList();
                case RebarNamedShape.Spiral:
                    return _allRebarShapes
                        .Where(s => s.GetRebarShapeDefinition() is RebarShapeDefinitionByArc rebarShapeDefinition &&
                                    rebarShapeDefinition.Type == RebarShapeDefinitionByArcType.Spiral)
                        .ToList();
                case RebarNamedShape.Triangular:
                    return _allRebarShapes.Where(s => IsValidByCurvesCount(s, 2, 0)).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(rebarNamedShape), rebarNamedShape, null);
            }
        }

        /// <summary>
        /// Возвращает список форм арматурных стержней, содержащих указанное количество сегментов в эскизе
        /// </summary>
        /// <param name="linesCount">Количество отрезков</param>
        /// <param name="arcsCount">Количество дуг</param>
        public List<RebarShape> GetRebarShapesBySegmentsCount(int linesCount, int arcsCount)
        {
            return _allRebarShapes.Where(s => IsValidByCurvesCount(s, linesCount, arcsCount)).ToList();
        }

        /// <summary>
        /// Возвращает список имен форм арматурных стержней, имеющих указанное количество прямых и дуговых сегментов
        /// </summary>
        /// <param name="rebarNamedShape">Именованная форма арматурного стержня</param>
        /// <param name="insertFirstEmpty">True - добавить в начало списка пустую строку</param>
        public List<string> GetRebarShapeNames(RebarNamedShape rebarNamedShape, bool insertFirstEmpty)
        {
            var names = GetRebarShapes(rebarNamedShape).Select(s => s.Name).ToList();
            if (insertFirstEmpty)
                names.Insert(0, string.Empty);
            return names;
        }
        
        private bool IsValidByCurvesCount(RebarShape rebarShape, int lineCount, int arcCount)
        {
            var curvesForBrowser = rebarShape.GetCurvesForBrowser();

            return curvesForBrowser.Count(c => c is Line) == lineCount &&
                   curvesForBrowser.Count(c => c is Arc) == arcCount;
        }

        private bool IsBottle(RebarShape rebarShape)
        {
            var curvesForBrowser = rebarShape.GetCurvesForBrowser();
            var lines = new List<Line>();
            foreach (var curve in curvesForBrowser)
            {
                if (curve is Line line)
                    lines.Add(line);
            }

            if (lines.Count == curvesForBrowser.Count &&
                lines.Count == 3 &&
                Math.Abs(lines[0].Direction.DotProduct(lines[2].Direction) - 1.0) < 0.001)
            {
                if (lines[0].IsPerpendicularTo(lines[1]) &&
                    lines[1].IsPerpendicularTo(lines[2]))
                    return false;
                var furthestPoints = GeometryUtils.GetFurthestPoints(lines[0], lines[2]);
                if ((lines[0].Length + lines[2].Length) > furthestPoints.Item1.DistanceTo(furthestPoints.Item2))
                    return false;
                return true;
            }

            return false;
        }

        private bool IsLShaped(RebarShape rebarShape)
        {
            var curvesForBrowser = rebarShape.GetCurvesForBrowser();
            var lines = new List<Line>();
            foreach (var curve in curvesForBrowser)
            {
                if (curve is Line line)
                    lines.Add(line);
            }

            return lines.Count == curvesForBrowser.Count &&
                   lines.Count == 2 &&
                   lines[0].IsPerpendicularTo(lines[1]);
        }

        private bool IsLShapedWithBigBend(RebarShape rebarShape)
        {
            var curvesForBrowser = rebarShape.GetCurvesForBrowser();
            var lines = new List<Line>();
            Arc arc = null;
            foreach (var curve in curvesForBrowser)
            {
                if (curve is Line line)
                    lines.Add(line);
                else if (curve is Arc a)
                    arc = a;
            }

            return lines.Count == curvesForBrowser.Count &&
                   lines.Count == 2 &&
                   arc != null &&
                   lines[0].IsPerpendicularTo(lines[1]);
        }

        private bool IsUShaped(RebarShape rebarShape)
        {
            var curvesForBrowser = rebarShape.GetCurvesForBrowser();
            var lines = new List<Line>();
            foreach (var curve in curvesForBrowser)
            {
                if (curve is Line line)
                    lines.Add(line);
            }

            return lines.Count == curvesForBrowser.Count &&
                   lines.Count == 3 &&
                   lines[0].IsPerpendicularTo(lines[1]) &&
                   lines[1].IsPerpendicularTo(lines[2]) &&
                   Math.Abs(lines[0].Direction.DotProduct(lines[2].Direction) - 1.0) < 0.001;
        }
    }
}
