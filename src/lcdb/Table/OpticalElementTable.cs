using System;
using System.Collections.Generic;
using System.Linq;
using LitMath;
using lcdb;
using lcdb.Annotation;
using OtoCAD.OpticEntity;

namespace OtoCAD.Table
{
    /// <summary>
    /// 光学元件数据表
    /// 提供光学元件的专用数据库操作
    /// </summary>
    public class OpticalElementTable : DBTable
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="database">数据库实例</param>
        /// <param name="tableId">表ID</param>
        internal OpticalElementTable(Database database, ObjectId tableId) : base(database, tableId)
        {
        }

        #region 专用查询方法

        /// <summary>
        /// 根据光学ID获取元件
        /// </summary>
        /// <param name="opticalId">光学ID</param>
        /// <returns>光学元件</returns>
        public Element GetByOpticalId(string opticalId)
        {
            return _items.OfType<Element>().FirstOrDefault(e => e.id.ToString() == opticalId);
        }

        /// <summary>
        /// 根据玻璃类型获取元件
        /// </summary>
        /// <param name="glassType">玻璃类型</param>
        /// <returns>光学元件列表</returns>
        public IList<Element> GetByGlassType(string glassType)
        {
            return _items.OfType<Element>().Where(e => e.GlassType == glassType).ToList();
        }

        /// <summary>
        /// 根据镀膜类型获取元件
        /// </summary>
        /// <param name="coatingType">镀膜类型</param>
        /// <returns>光学元件列表</returns>
        public IList<Element> GetByCoatingType(CoatingType coatingType)
        {
            return _items.OfType<Element>().Where(e => 
                e.CoatingMarks.Any(cm => cm.CoatingType == coatingType)).ToList();
        }

        /// <summary>
        /// 根据折射率范围获取元件
        /// </summary>
        /// <param name="minIndex">最小折射率</param>
        /// <param name="maxIndex">最大折射率</param>
        /// <returns>光学元件列表</returns>
        public IList<Element> GetByRefractiveIndexRange(double minIndex, double maxIndex)
        {
            return _items.OfType<Element>().Where(e => 
                e.RefractiveIndex >= minIndex && e.RefractiveIndex <= maxIndex).ToList();
        }

        /// <summary>
        /// 根据阿贝数范围获取元件
        /// </summary>
        /// <param name="minAbbe">最小阿贝数</param>
        /// <param name="maxAbbe">最大阿贝数</param>
        /// <returns>光学元件列表</returns>
        public IList<Element> GetByAbbeNumberRange(double minAbbe, double maxAbbe)
        {
            return _items.OfType<Element>().Where(e => 
                e.AbbeNumber >= minAbbe && e.AbbeNumber <= maxAbbe).ToList();
        }

        /// <summary>
        /// 根据焦距范围获取元件
        /// </summary>
        /// <param name="minFocalLength">最小焦距</param>
        /// <param name="maxFocalLength">最大焦距</param>
        /// <returns>光学元件列表</returns>
        public IList<Element> GetByFocalLengthRange(double minFocalLength, double maxFocalLength)
        {
            return _items.OfType<Element>().Where(e => 
            {
                var fl = e.GetFocalLength();
                return fl >= minFocalLength && fl <= maxFocalLength;
            }).ToList();
        }

        /// <summary>
        /// 获取所有透镜元件
        /// </summary>
        /// <returns>透镜元件列表</returns>
        public IList<Element> GetAllLenses()
        {
            return _items.OfType<Element>().Where(e => e is EnhancedElement).ToList();
        }

        /// <summary>
        /// 获取所有透镜系统
        /// </summary>
        /// <returns>透镜系统列表</returns>
        public IList<Element> GetAllLensSystems()
        {
            return _items.OfType<Element>().Where(e => e is Lens).ToList();
        }

        /// <summary>
        /// 获取所有框架
        /// </summary>
        /// <returns>框架列表</returns>
        public IList<Element> GetAllFrames()
        {
            return _items.OfType<Element>().Where(e => e is Frame).ToList();
        }

        #endregion

        #region 光学计算方法

        /// <summary>
        /// 计算系统总焦距
        /// </summary>
        /// <returns>系统总焦距</returns>
        public double CalculateSystemFocalLength()
        {
            var lenses = GetAllLenses();
            if (lenses.Count == 0)
                return double.PositiveInfinity;

            if (lenses.Count == 1)
                return lenses[0].GetFocalLength();

            // 多透镜系统焦距计算
            double totalPower = 0.0;
            foreach (var lens in lenses)
            {
                var fl = lens.GetFocalLength();
                if (Math.Abs(fl) > 1e-6)
                {
                    totalPower += 1.0 / fl;
                }
            }

            return Math.Abs(totalPower) > 1e-6 ? 1.0 / totalPower : double.PositiveInfinity;
        }

        /// <summary>
        /// 计算系统光学中心
        /// </summary>
        /// <returns>系统光学中心</returns>
        public Vector2 CalculateOpticalCenter()
        {
            var elements = GetAllLenses();
            if (elements.Count == 0)
                return new Vector2(0, 0);

            double totalX = 0.0;
            double totalY = 0.0;
            double totalWeight = 0.0;

            foreach (var element in elements)
            {
                var center = element.GetOpticalCenter();
                double weight = 1.0 / Math.Max(Math.Abs(element.GetFocalLength()), 1e-6);
                
                totalX += center.X * weight;
                totalY += center.Y * weight;
                totalWeight += weight;
            }

            return totalWeight > 1e-6 ? new Vector2(totalX / totalWeight, totalY / totalWeight) : new Vector2(0, 0);
        }

        /// <summary>
        /// 计算系统数值孔径
        /// </summary>
        /// <returns>系统数值孔径</returns>
        public double CalculateSystemNumericalAperture()
        {
            var elements = GetAllLenses();
            if (elements.Count == 0)
                return 0.0;

            // 找到限制孔径的元件
            double minAperture = double.MaxValue;
            foreach (var element in elements)
            {
                double aperture = element.SemiDiameter;
                if (aperture < minAperture)
                    minAperture = aperture;
            }

            // 计算系统数值孔径
            double systemFocalLength = CalculateSystemFocalLength();
            return Math.Abs(systemFocalLength) > 1e-6 ? minAperture / (2 * Math.Abs(systemFocalLength)) : 0.0;
        }

        /// <summary>
        /// 计算系统F数
        /// </summary>
        /// <returns>系统F数</returns>
        public double CalculateSystemFNumber()
        {
            var systemNA = CalculateSystemNumericalAperture();
            return systemNA > 1e-6 ? 1.0 / (2 * systemNA) : double.PositiveInfinity;
        }

        /// <summary>
        /// 计算系统总长度
        /// </summary>
        /// <returns>系统总长度</returns>
        public double CalculateSystemTotalLength()
        {
            var elements = GetAllLenses().Cast<Element>().ToList();
            if (elements.Count == 0)
                return 0.0;

            var sortedElements = elements.OrderBy(e => e.OriginalX).ToList();
            
            double firstElementX = sortedElements[0].OriginalX;
            double lastElementX = sortedElements[sortedElements.Count - 1].OriginalX;
            double lastElementThickness = sortedElements[sortedElements.Count - 1].Thickness;

            return lastElementX + lastElementThickness - firstElementX;
        }

        #endregion

        #region 统计方法

        /// <summary>
        /// 获取玻璃类型统计
        /// </summary>
        /// <returns>玻璃类型统计字典</returns>
        public Dictionary<string, int> GetGlassTypeStatistics()
        {
            var statistics = new Dictionary<string, int>();
            
            foreach (var element in _items.OfType<Element>())
            {
                var glassType = element.GlassType ?? "未知";
                if (statistics.ContainsKey(glassType))
                {
                    statistics[glassType]++;
                }
                else
                {
                    statistics[glassType] = 1;
                }
            }

            return statistics;
        }

        /// <summary>
        /// 获取镀膜类型统计
        /// </summary>
        /// <returns>镀膜类型统计字典</returns>
        public Dictionary<CoatingType, int> GetCoatingTypeStatistics()
        {
            var statistics = new Dictionary<CoatingType, int>();
            
            foreach (var element in _items.OfType<Element>())
            {
                foreach (var coating in element.CoatingMarks)
                {
                    if (statistics.ContainsKey(coating.CoatingType))
                    {
                        statistics[coating.CoatingType]++;
                    }
                    else
                    {
                        statistics[coating.CoatingType] = 1;
                    }
                }
            }

            return statistics;
        }

        /// <summary>
        /// 获取折射率分布
        /// </summary>
        /// <returns>折射率分布字典</returns>
        public Dictionary<string, int> GetRefractiveIndexDistribution()
        {
            var distribution = new Dictionary<string, int>();
            
            foreach (var element in _items.OfType<Element>())
            {
                var range = GetRefractiveIndexRange(element.RefractiveIndex);
                if (distribution.ContainsKey(range))
                {
                    distribution[range]++;
                }
                else
                {
                    distribution[range] = 1;
                }
            }

            return distribution;
        }

        /// <summary>
        /// 获取阿贝数分布
        /// </summary>
        /// <returns>阿贝数分布字典</returns>
        public Dictionary<string, int> GetAbbeNumberDistribution()
        {
            var distribution = new Dictionary<string, int>();
            
            foreach (var element in _items.OfType<Element>())
            {
                var range = GetAbbeNumberRange(element.AbbeNumber);
                if (distribution.ContainsKey(range))
                {
                    distribution[range]++;
                }
                else
                {
                    distribution[range] = 1;
                }
            }

            return distribution;
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证所有元件的光学参数
        /// </summary>
        /// <returns>验证结果列表</returns>
        public List<ValidationResult> ValidateAllElements()
        {
            var results = new List<ValidationResult>();
            
            foreach (var element in _items.OfType<Element>())
            {
                var result = element.ValidateOpticalParameters();
                if (!result.IsValid || result.WarningMessages.Count > 0)
                {
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// 检查元件冲突
        /// </summary>
        /// <returns>冲突信息列表</returns>
        public List<string> CheckElementConflicts()
        {
            var conflicts = new List<string>();
            var elements = _items.OfType<Element>().ToList();
            
            for (int i = 0; i < elements.Count; i++)
            {
                for (int j = i + 1; j < elements.Count; j++)
                {
                    var element1 = elements[i];
                    var element2 = elements[j];
                    
                    // 检查位置重叠
                    if (CheckPositionOverlap(element1, element2))
                    {
                        conflicts.Add($"元件 {element1.id} 和 {element2.id} 位置重叠");
                    }
                    
                    // 检查参数不兼容
                    if (CheckParameterIncompatibility(element1, element2))
                    {
                        conflicts.Add($"元件 {element1.id} 和 {element2.id} 参数不兼容");
                    }
                }
            }

            return conflicts;
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 获取折射率范围字符串
        /// </summary>
        /// <param name="refractiveIndex">折射率</param>
        /// <returns>范围字符串</returns>
        private string GetRefractiveIndexRange(double refractiveIndex)
        {
            if (refractiveIndex < 1.4)
                return "< 1.4";
            else if (refractiveIndex < 1.5)
                return "1.4 - 1.5";
            else if (refractiveIndex < 1.6)
                return "1.5 - 1.6";
            else if (refractiveIndex < 1.7)
                return "1.6 - 1.7";
            else if (refractiveIndex < 1.8)
                return "1.7 - 1.8";
            else
                return "> 1.8";
        }

        /// <summary>
        /// 获取阿贝数范围字符串
        /// </summary>
        /// <param name="abbeNumber">阿贝数</param>
        /// <returns>范围字符串</returns>
        private string GetAbbeNumberRange(double abbeNumber)
        {
            if (abbeNumber < 20)
                return "< 20";
            else if (abbeNumber < 30)
                return "20 - 30";
            else if (abbeNumber < 40)
                return "30 - 40";
            else if (abbeNumber < 50)
                return "40 - 50";
            else if (abbeNumber < 60)
                return "50 - 60";
            else if (abbeNumber < 70)
                return "60 - 70";
            else
                return "> 70";
        }

        /// <summary>
        /// 检查位置重叠
        /// </summary>
        /// <param name="element1">元件1</param>
        /// <param name="element2">元件2</param>
        /// <returns>是否重叠</returns>
        private bool CheckPositionOverlap(Element element1, Element element2)
        {
            var center1 = element1.GetOpticalCenter();
            var center2 = element2.GetOpticalCenter();
            
            var distance = Math.Sqrt(Math.Pow(center1.X - center2.X, 2) + Math.Pow(center1.Y - center2.Y, 2));
            var minDistance = (element1.SemiDiameter + element2.SemiDiameter) / 2;
            
            return distance < minDistance;
        }

        /// <summary>
        /// 检查参数不兼容
        /// </summary>
        /// <param name="element1">元件1</param>
        /// <param name="element2">元件2</param>
        /// <returns>是否不兼容</returns>
        private bool CheckParameterIncompatibility(Element element1, Element element2)
        {
            // 检查折射率差异过大
            var indexDiff = Math.Abs(element1.RefractiveIndex - element2.RefractiveIndex);
            if (indexDiff > 0.5)
                return true;

            // 检查阿贝数差异过大
            var abbeDiff = Math.Abs(element1.AbbeNumber - element2.AbbeNumber);
            if (abbeDiff > 40)
                return true;

            return false;
        }

        #endregion
    }
}