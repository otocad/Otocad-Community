using lcdb.Colors;
﻿using System;
using System.Collections.Generic;
using System.Drawing;
using LitMath;
using lcdb;
using OtoCAD;
namespace lcdb.Annotation
{
    /// <summary>
    /// 镀膜标记实
    /// </summary>
    public class CoatingMark : Entity, ISurfaceAttachable
    {
        public override string className => "CoatingMark";

        /// <summary>
        /// Blackening 涂黑符号 `—·—` 模式实际总宽相对 Size 的比例:
        /// totalActualLength = 2*dashLen + dotLen + 2*gap = 2*0.3 + 0.05 + 2*0.1 = 0.85
        /// Story 8-2 的位置补偿值 (0.15·Size) 数学依赖此常数:旧版起点用 length=Size 假设,
        /// 视觉中心相对 Center 左偏 (Size − 0.85·Size)/2 = 0.075·Size,
        /// 完整位置补偿 = 2 × 0.075·Size = 0.15·Size。
        /// 修改此常数会破坏 Story 8-2 / FR-14 迁移逻辑。
        /// </summary>
        public const double BlackeningTotalWidthRatio = 0.85;

        /// <summary>
        /// 默认 TextSize 系数 (FR-2, Story 9-1):TextSize = Size × 0.4
        /// SM-C1 counter-metric:必须 ≤ 0.5,防止文字默认值膨胀压倒符号。
        /// </summary>
        public const double DefaultTextSizeRatio = 0.4;

        /// <summary>
        /// TextOffset 默认系数(按形态分组,FR-3, Story 9-1)。
        /// 改动时同步 [Source: epics.md § Epic 2 Story 2.1] 与 GB/T 13323 视觉规范。
        /// </summary>
        public const double DefaultTextOffsetRatio_Circular   = 0.8;  // 带圈符号 (AR/BBAR/HR/OR/Filter/Protective/Polarizing/PR/Other/Custom)
        public const double DefaultTextOffsetRatio_YShape     = 1.0;  // Y 形 (BS)
        public const double DefaultTextOffsetRatio_DashLine   = 0.6;  // 横线条 (Blackening) + TextPosition 强制 Top
        public const double DefaultTextOffsetRatio_WaveLine   = 0.9;  // 波浪线 (Conductive) + TextPosition 强制 Top

        /// <summary>
        /// 按 CoatingType 返回默认 TextSize(Size × DefaultTextSizeRatio)。
        /// </summary>
        public static double ComputeDefaultTextSize(double size) => size * DefaultTextSizeRatio;

        /// <summary>
        /// 按 CoatingType 形态分组返回默认 TextOffset。
        /// 形态分组(FR-3):
        ///   - 横线条 (Blackening) → 0.6·Size
        ///   - 波浪线 (Conductive) → 0.9·Size
        ///   - Y 形 (BS) → 1.0·Size
        ///   - 带圈 (其它所有) → 0.8·Size
        /// </summary>
        public static double ComputeDefaultTextOffset(CoatingType type, double size)
        {
            switch (type)
            {
                case CoatingType.Blackening:  return size * DefaultTextOffsetRatio_DashLine;
                case CoatingType.Conductive:  return size * DefaultTextOffsetRatio_WaveLine;
                case CoatingType.BS:          return size * DefaultTextOffsetRatio_YShape;
                default:                      return size * DefaultTextOffsetRatio_Circular;
            }
        }

        /// <summary>
        /// 判断 CoatingType 是否对 TextPosition 有约束(只能 Top/Bottom)。
        /// 横线条 (Blackening) 与波浪线 (Conductive) 的几何在水平方向受限,文字必须放上/下。
        /// </summary>
        public static bool IsTextPositionConstrained(CoatingType type)
            => type == CoatingType.Blackening || type == CoatingType.Conductive;

        /// <summary>
        /// 标记中心位置
        /// </summary>
        public Vector2 Center { get; set; } = new Vector2(0, 0);

        /// <summary>
        /// 标记大小
        /// </summary>
        public double Size { get; set; } = 2.0;

        /// <summary>
        /// 符号朝向(弧度)。0 = 默认放置。
        /// 贴面放置时设为「外法线方向 - 90°」,使符号沿外法线竖立、坐在面外侧。
        /// 带圈符号 (圆完全对称) 旋转后视觉相同; Y 形/横线条/波浪线等非对称几何会随之转向。
        /// 文字始终水平(字形不旋转,仅锚点随符号旋转)。
        /// </summary>
        public double Rotation { get; set; } = 0.0;

        // Story 10-3 (FR-5):CoatingType 改 backing field,联动 ShowText invariant
        private CoatingType _coatingType = CoatingType.AR;

        /// <summary>
        /// 镀膜类型。Story 10-3:切换到 AR/BBAR/Polarizing 时联动强制 ShowText=true
        /// (GB/T 13323 这 3 类共用 ⊕ 符号,文字标签必须显示以区分)。
        /// </summary>
        public CoatingType CoatingType
        {
            get => _coatingType;
            set
            {
                _coatingType = value;
                if (!_isLoading && IsShowTextRequired(value) && !_showText)
                {
                    _showText = true;
                }
            }
        }

        /// <summary>
        /// 判断 CoatingType 是否强制要求 ShowText=true (FR-5)。
        /// AR/BBAR/Polarizing 共用 ⊕ 符号,GB/T 13323-2009 要求文字标签区分。
        /// </summary>
        public static bool IsShowTextRequired(CoatingType type)
            => type == CoatingType.AR
            || type == CoatingType.BBAR
            || type == CoatingType.Polarizing;

        /// <summary>
        /// 镀膜标记文本
        /// </summary>
        public string CoatingText { get; set; } = "AR";

        /// <summary>
        /// 文本偏移距离。Story 9-1 后:新建时由 ComputeDefaultTextOffset(type, size) 填入,
        /// 反序列化覆盖,用户手动设置时尊重。属性初始化器保留 3.0 仅为防止未初始化场景下的 NaN。
        /// </summary>
        public double TextOffset { get; set; } = 3.0;

        // TextPosition 用 backing field + setter,以便实现 Blackening/Conductive 的强制约束(M-5 模式)
        private CoatingTextPosition _textPosition = CoatingTextPosition.Right;

        /// <summary>
        /// 文本位置方向。
        /// Story 9-1 setter invariant(FR-3, M-5):若 CoatingType 是 Blackening 或 Conductive,
        /// 用户/代码尝试设置非 Top/Bottom 值会被静默纠正为 Top,并通过 Debug.WriteLine 警告。
        /// 反序列化期(_isLoading=true)不强制,保证旧文件原样加载;
        /// 但反序列化完成后任何后续设置仍受约束。
        /// </summary>
        public CoatingTextPosition TextPosition
        {
            get => _textPosition;
            set
            {
                if (!_isLoading
                    && IsTextPositionConstrained(CoatingType)
                    && value != CoatingTextPosition.Top
                    && value != CoatingTextPosition.Bottom)
                {
                    // M-5:静默纠正 + 日志,不抛异常(给老用户出口)
                    System.Diagnostics.Debug.WriteLine(
                        $"[CoatingMark] TextPosition={value} 对 CoatingType={CoatingType} 无效," +
                        $"已静默改为 Top(横线条/波浪线几何受水平方向约束)");
                    _textPosition = CoatingTextPosition.Top;
                }
                else
                {
                    _textPosition = value;
                }
            }
        }

        /// <summary>
        /// 标记形状。默认 Standard:按 CoatingType 自动选 GB/T 13323 符号 (AR/HR/BS…),
        /// 而非画死的三角/方/菱 (那是旧版兼容用)。镀膜类型由 CoatingType 决定, 不是几何形状。
        /// </summary>
        public CoatingMarkShape MarkShape { get; set; } = CoatingMarkShape.Standard;

        // Story 10-3 (FR-5):ShowText 改 backing field,setter 在 AR/BBAR/Polarizing 时强制 true
        private bool _showText = true;

        /// <summary>
        /// 是否显示文本。Story 10-3:AR/BBAR/Polarizing 共用 ⊕ 符号,文字必须显示;
        /// 若尝试设 false,setter 静默改回 true + Debug.WriteLine 警告(M-5 模式)。
        /// 反序列化期(_isLoading=true)不强制,由 CoatingMarkMigrations.EnforceShowTextInvariant 统一校验。
        /// </summary>
        public bool ShowText
        {
            get => _showText;
            // 切到 AR/BBAR/Polarizing 时 CoatingType setter 会把文字「默认打开」(便于区分共用 ⊕ 符号),
            // 但用户可显式关闭 — 不再强制 true (产品决定: 图上常不需要文字, 类型在规格表/属性里)。
            set => _showText = value;
        }

        /// <summary>
        /// 文本大小。Story 9-1 后:新建时由 ComputeDefaultTextSize(size) 填入(Size × 0.4),
        /// 反序列化覆盖,用户手动设置时尊重。属性初始化器 1.5 仅为防止未初始化场景。
        /// </summary>
        public double TextSize { get; set; } = 1.5;

        private List<Entity> _markEntities = new List<Entity>();

        /// <summary>
        /// 反序列化期 flag(M-3 模式)— 与 <see cref="lcdb.IO.OtocadFileFormatV3.DeserializeCoatingMark"/> 配合。
        /// true 期间:
        ///   - setter 跳过 Generate 缓存清理(避免反序列化中途多次 Generate)
        ///   - setter invariant 不强制(Epic 3 Story 10-1/10-3 引入后会用此 flag)
        ///
        /// Story 8-2 仅引入字段,setter 逻辑不动(详见 Story 8-2 Dev Notes 段)。
        /// </summary>
        internal bool _isLoading = false;

        /// <summary>
        /// 外围边框
        /// </summary>
        public override Bounding bounding
        {
            get
            {
                double totalSize = Size + (ShowText ? TextOffset + TextSize * 2 : 0);
                return new Bounding(Center, totalSize * 2, totalSize * 2);
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public CoatingMark()
        {
        }

        public CoatingMark(Vector2 center, CoatingType coatingType, double size = 2.0)
        {
            Center = center;
            CoatingType = coatingType;
            Size = size;
            CoatingText = GetCoatingText(coatingType);

            // Story 9-1 (FR-2/3):按形态分组应用视觉默认值
            TextSize = ComputeDefaultTextSize(size);
            TextOffset = ComputeDefaultTextOffset(coatingType, size);

            // FR-3 invariant:Blackening/Conductive 默认 Top 位置(几何受约束)
            // 通过 backing field 直接赋值,绕开 setter 警告日志(构造期非用户错误)
            if (IsTextPositionConstrained(coatingType))
            {
                _textPosition = CoatingTextPosition.Top;
            }
        }

        /// <summary>
        /// 使绘制缓存失效，下次Draw时重新生成图形
        /// 用于PropertyManager修改属性后触发重绘
        /// </summary>
        public void InvalidateCache()
        {
            _markEntities.Clear();
        }

        /// <summary>
        /// 贴面放置 (ISurfaceAttachable): 镀膜标记只「贴面」不「转向」——
        /// 圆形符号沿外法线外移半个符号尺寸, 使圆与被测面相切坐在面外侧, 但符号本身不旋转
        /// (保持原朝向便于辨识 AR/HR/BS, 区别于粗糙度等随法线竖立的标记)。
        /// </summary>
        public void AttachToSurface(Vector2 surfacePoint, Vector2 outwardNormal)
        {
            Rotation = 0.0;
            Center = surfacePoint + outwardNormal * (Size * 0.5);
            if (AttachState != null) AttachState.Height = surfacePoint.Y - AttachState.Apex.Y;   // 记录贴面高度, 关联重锚保持
            _markEntities.Clear();
        }

        /// <summary>绕 Center 旋转 Rotation 弧度 (Rotation == 0 时原样返回, 零开销)。</summary>
        private Vector2 R(Vector2 p) =>
            Rotation == 0.0 ? p : Vector2.RotateInRadian(p, Center, Rotation);

        /// <summary>
        /// 生成标记图形
        /// 根据GB/T 13323-2009标准绘制镀膜符号
        /// </summary>
        protected void Generate()
        {
            _markEntities.Clear();

            // 优先使用标准符号
            if (MarkShape == CoatingMarkShape.Standard)
            {
                GenerateStandardMark();
            }
            else
            {
                // 兼容旧版形状
                switch (MarkShape)
                {
                    case CoatingMarkShape.Triangle:
                        GenerateLegacyTriangleMark();
                        break;
                    case CoatingMarkShape.Circle:
                        GenerateLegacyCircleMark();
                        break;
                    case CoatingMarkShape.Square:
                        GenerateLegacySquareMark();
                        break;
                    case CoatingMarkShape.Diamond:
                        GenerateLegacyDiamondMark();
                        break;
                }
            }

            if (ShowText)
            {
                GenerateText();
            }
        }

        #region GB/T 13323-2009 标准符号绘制

        /// <summary>
        /// 根据CoatingType生成标准符号
        /// GB/T 13323-2009 表1 序号10-18
        ///
        /// 符号对照表 (GB/T 13323-2009 表1):
        /// | 序号 | 名称       | 符号           | CoatingType      |
        /// |------|-----------|----------------|------------------|
        /// | 10   | 内反射膜   | 圆+上三角 △    | HR               |
        /// | 11   | 外反射膜   | 圆+下三角 ▽    | OuterReflective  |
        /// | 12   | 分束(色)膜 | 圆+Y           | BS               |
        /// | 13   | 滤光膜     | 圆+单横线 ⊖    | Filter           |
        /// | 14   | 保护膜     | 圆+双横线 ⊜    | Protective       |
        /// | 15   | 导电膜     | 圆+单波浪线    | Conductive       |
        /// | 16   | 偏振膜     | 圆+横+上半竖   | Polarizing       |
        /// | 17   | 涂黑       | 点划线 ---     | Blackening       |
        /// | 18   | 减反射膜   | 圆+完整十字 ⊕  | AR               |
        /// </summary>
        private void GenerateStandardMark()
        {
            switch (CoatingType)
            {
                case CoatingType.HR:
                    // #10 内反射膜 - 圆 + 上1/3横线 + 尖在圆底的两斜边 (闭合 ▽)
                    GenerateInnerReflection();
                    break;
                case CoatingType.OuterReflective:
                    // #11 外反射膜 - 圆 + 尖在圆底的两斜边 (只有尖, 无横线)
                    GenerateOuterReflection();
                    break;
                case CoatingType.BS:
                    // #12 分束(色)膜 - 外圆 + Y 形
                    GenerateCircleWithYShape();
                    break;
                case CoatingType.Filter:
                    // #13 滤光膜 ⊖ - 圆圈内单横线
                    GenerateCircleWithHorizontalLine();
                    break;
                case CoatingType.Protective:
                    // #14 保护膜 ⊜ - 圆圈内双横线
                    GenerateCircleWithDoubleHorizontalLine();
                    break;
                case CoatingType.Conductive:
                    // #15 导电膜 - 外圆 + 一条波浪线
                    GenerateCircleWithSingleWave();
                    break;
                case CoatingType.Polarizing:
                    // #16 偏振膜 - 圆 + 整条横 + 上半竖 (非完整十字)
                    GenerateCircleWithHalfCross();
                    break;
                case CoatingType.Blackening:
                    // #17 涂黑 --- - 粗点划线(无圆圈)
                    GenerateBlackeningMark();
                    break;
                case CoatingType.AR:
                case CoatingType.BBAR:
                    // #18 减反射膜 ⊕ - 圆圈内完整十字
                    GenerateCircleWithCross();
                    break;
                default:
                    // 其他/自定义 - 简单圆圈
                    GenerateOuterCircle();
                    break;
            }
        }

        /// <summary>
        /// 绘制外圆圈 (所有标准符号的基础)
        /// </summary>
        private void GenerateOuterCircle()
        {
            var circle = new Circle
            {
                center = Center,
                radius = Size * 0.5,
                color = GetMarkColor()
            };
            _markEntities.Add(circle);
        }

        /// <summary>
        /// #10 内反射膜 - 圆 + 顶横线(上 1/3) + 上尖(顶点在线上、朝上)、两腿向下张到圆弧(单边 30°, 总张角 60°)
        /// GB/T 13323-2009 表1 序号10. 上尖在 (0,+r/3) 线上; 两腿沿与竖直成 ±30° 的方向向下, 端点落在圆周(圆弧)上.
        /// </summary>
        private void GenerateInnerReflection()
        {
            GenerateOuterCircle();

            double r   = Size * 0.5;
            double by  = Center.Y + r / 3.0;                       // 顶横线 / 上尖 Y (上 1/3)
            double hw  = Math.Sqrt(r * r - (r / 3.0) * (r / 3.0)); // 顶横线半宽 (端点落在圆上)
            var apex = new Vector2(Center.X, by);                  // 上尖 (在线上, 朝上)

            // 两腿从上尖沿"与竖直成 ±30°、向下"的方向延伸, 端点落在圆周上 (求射线与圆的正向交点)
            double half = 30.0 * Math.PI / 180.0;                  // 单边 30° → 总张角 60°
            double s = Math.Sin(half), c = Math.Cos(half);
            Vector2 LegEnd(double dx, double dy)
            {
                double fx = apex.X - Center.X, fy = apex.Y - Center.Y;   // 上尖相对圆心
                double b = 2.0 * (fx * dx + fy * dy);                    // |dir|=1
                double cc = fx * fx + fy * fy - r * r;
                double t = (-b + Math.Sqrt(b * b - 4.0 * cc)) / 2.0;     // 正向交点
                return new Vector2(apex.X + t * dx, apex.Y + t * dy);
            }
            var endL = LegEnd(-s, -c);   // 左下
            var endR = LegEnd( s, -c);   // 右下

            _markEntities.Add(new Line(R(new Vector2(Center.X - hw, by)),
                                       R(new Vector2(Center.X + hw, by))) { color = GetMarkColor() });  // 顶横线
            _markEntities.Add(new Line(R(apex), R(endL)) { color = GetMarkColor() });  // 左腿 (上尖↘圆弧)
            _markEntities.Add(new Line(R(apex), R(endR)) { color = GetMarkColor() });  // 右腿 (上尖↙圆弧)
        }

        /// <summary>
        /// #11 外反射膜 - 圆 + 尖在圆底的两条斜边 (只有尖, 无横线)
        /// GB/T 13323-2009 表1 序号11. = 内反射膜去掉顶横线.
        /// </summary>
        private void GenerateOuterReflection()
        {
            GenerateOuterCircle();

            double r = Size * 0.5;
            double ty = Center.Y + r / 3.0;                       // 斜边上端 Y (与内反一致, 但不画横线)
            double hw = Math.Sqrt(r * r - (r / 3.0) * (r / 3.0));
            var tl   = R(new Vector2(Center.X - hw, ty));
            var tr   = R(new Vector2(Center.X + hw, ty));
            var apex = R(new Vector2(Center.X, Center.Y - r));    // 尖在圆底

            _markEntities.Add(new Line(apex, tl) { color = GetMarkColor() });  // 左斜边
            _markEntities.Add(new Line(apex, tr) { color = GetMarkColor() });  // 右斜边
        }

        /// <summary>
        /// #12 分束(色)膜 - 外圆 + Y 形符号
        /// GB/T 13323-2009 表1 序号12
        /// </summary>
        private void GenerateCircleWithYShape()
        {
            GenerateOuterCircle();   // 分束(色)膜有外圆

            double r = Size * 0.5;
            // Y形从中心出发，三个端点120度均匀分布
            var bottom = R(new Vector2(Center.X, Center.Y - r)); // 底部端点
            double angle = Math.PI / 3; // 60度
            var topLeft = R(new Vector2(Center.X - r * Math.Sin(angle), Center.Y + r * Math.Cos(angle)));
            var topRight = R(new Vector2(Center.X + r * Math.Sin(angle), Center.Y + r * Math.Cos(angle)));

            _markEntities.Add(new Line(Center, bottom) { color = GetMarkColor() });
            _markEntities.Add(new Line(Center, topLeft) { color = GetMarkColor() });
            _markEntities.Add(new Line(Center, topRight) { color = GetMarkColor() });
        }

        /// <summary>
        /// #13 滤光膜 ⊖ - 圆圈内单横线
        /// GB/T 13323-2009 表1 序号13
        /// </summary>
        private void GenerateCircleWithHorizontalLine()
        {
            GenerateOuterCircle();

            double r = Size * 0.5;
            var left = R(new Vector2(Center.X - r, Center.Y));
            var right = R(new Vector2(Center.X + r, Center.Y));

            _markEntities.Add(new Line(left, right) { color = GetMarkColor() });
        }

        /// <summary>
        /// #14 保护膜 ⊜ - 圆圈内双横线
        /// GB/T 13323-2009 表1 序号14
        /// </summary>
        private void GenerateCircleWithDoubleHorizontalLine()
        {
            GenerateOuterCircle();

            double r = Size * 0.5;
            double gap = Size * 0.15; // 两线间距

            // 计算横线端点，使其接触圆圈
            // 对于y偏移gap的横线，x范围为 sqrt(r^2 - gap^2)
            double halfWidth = Math.Sqrt(r * r - gap * gap);

            var left1 = R(new Vector2(Center.X - halfWidth, Center.Y + gap));
            var right1 = R(new Vector2(Center.X + halfWidth, Center.Y + gap));
            var left2 = R(new Vector2(Center.X - halfWidth, Center.Y - gap));
            var right2 = R(new Vector2(Center.X + halfWidth, Center.Y - gap));

            _markEntities.Add(new Line(left1, right1) { color = GetMarkColor() });
            _markEntities.Add(new Line(left2, right2) { color = GetMarkColor() });
        }

        /// <summary>
        /// #15 导电膜 - 外圆 + 一条水平波浪线(电波)
        /// GB/T 13323-2009 表1 序号15
        /// </summary>
        private void GenerateCircleWithSingleWave()
        {
            GenerateOuterCircle();   // 导电膜有外圆

            double r = Size * 0.5;
            GenerateSingleWaveLine(Center.X - r, Center.X + r, Center.Y);
        }

        /// <summary>
        /// 绘制单条波浪线
        /// </summary>
        private void GenerateSingleWaveLine(double xStart, double xEnd, double yCenter)
        {
            int segments = 4;
            double segWidth = (xEnd - xStart) / segments;
            double waveHeight = Size * 0.08;

            for (int i = 0; i < segments; i++)
            {
                double x1 = xStart + i * segWidth;
                double x2 = xStart + (i + 1) * segWidth;
                double y1 = yCenter + (i % 2 == 0 ? waveHeight : -waveHeight);
                double y2 = yCenter + (i % 2 == 0 ? -waveHeight : waveHeight);

                _markEntities.Add(new Line(R(new Vector2(x1, y1)), R(new Vector2(x2, y2))) { color = GetMarkColor() });
            }
        }

        /// <summary>
        /// #18 减反射膜 ⊕ - 圆圈内完整十字
        /// GB/T 13323-2009 表1 序号18
        /// </summary>
        private void GenerateCircleWithCross()
        {
            GenerateOuterCircle();

            double r = Size * 0.5;
            // 水平线端点接触圆圈
            _markEntities.Add(new Line(
                R(new Vector2(Center.X - r, Center.Y)),
                R(new Vector2(Center.X + r, Center.Y))) { color = GetMarkColor() });
            // 垂直线端点接触圆圈
            _markEntities.Add(new Line(
                R(new Vector2(Center.X, Center.Y - r)),
                R(new Vector2(Center.X, Center.Y + r))) { color = GetMarkColor() });
        }

        /// <summary>
        /// #16 偏振膜 - 圆 + 整条横 + 上半竖 (圆心→顶, 非完整十字)
        /// GB/T 13323-2009 表1 序号16
        /// </summary>
        private void GenerateCircleWithHalfCross()
        {
            GenerateOuterCircle();

            double r = Size * 0.5;
            // 整条横 (左右接触圆)
            _markEntities.Add(new Line(
                R(new Vector2(Center.X - r, Center.Y)),
                R(new Vector2(Center.X + r, Center.Y))) { color = GetMarkColor() });
            // 上半竖 (圆心 → 顶)
            _markEntities.Add(new Line(
                R(new Vector2(Center.X, Center.Y)),
                R(new Vector2(Center.X, Center.Y + r))) { color = GetMarkColor() });
        }

        /// <summary>
        /// #17 涂黑 --- - 粗点划线(无圆圈)
        /// GB/T 13323-2009 表1 序号17
        /// </summary>
        private void GenerateBlackeningMark()
        {
            // 涂黑使用粗点划线模式，不是圆圈
            double dashLen = Size * 0.3;
            double dotLen = Size * 0.05;
            double gap = Size * 0.1;
            // FR-1 / Story 8-1: 起点必须基于实际总宽 (= BlackeningTotalWidthRatio * Size = 0.85*Size),
            // 而不是 length=Size 的错误假设,否则符号会左偏 0.075*Size (= 0.15*Size 的位置补偿值的一半)。
            // 总宽锁定为 BlackeningTotalWidthRatio (public const, Story 8-2 source 层引用),
            // 任何改动 dashLen/dotLen/gap 都必须同步更新该常数,否则破坏 Story 8-2 的位置补偿数学。
            double totalActualLength = BlackeningTotalWidthRatio * Size;
            System.Diagnostics.Debug.Assert(
                System.Math.Abs(totalActualLength - (2 * dashLen + dotLen + 2 * gap)) < 1e-9,
                "BlackeningTotalWidthRatio 与几何参数不一致 — 必须同步更新");

            // 绘制 —·—·— 模式,起点保证整体关于 Center 对称
            double x = Center.X - totalActualLength / 2;
            double y = Center.Y;

            // 长划线
            _markEntities.Add(new Line(
                R(new Vector2(x, y)),
                R(new Vector2(x + dashLen, y))) { color = GetMarkColor() });
            x += dashLen + gap;

            // 点
            _markEntities.Add(new Line(
                R(new Vector2(x, y)),
                R(new Vector2(x + dotLen, y))) { color = GetMarkColor() });
            x += dotLen + gap;

            // 长划线
            _markEntities.Add(new Line(
                R(new Vector2(x, y)),
                R(new Vector2(x + dashLen, y))) { color = GetMarkColor() });
        }

        // 注: 减反射膜(AR/BBAR)现在直接使用GenerateCircleWithCross()
        // GB/T 13323-2009 表1 序号18规定减反射膜与偏振膜使用相同的⊕符号
        // 旧版的90°标注已移除，因为国标中没有此要求

        // 部分反射膜 PR: 国标表1 无此条目, 已删除 (不再画半三角占位; CoatingType.PR 若出现则落 default 简单圆).

        #endregion

        #region 兼容旧版形状

        private void GenerateLegacyTriangleMark()
        {
            double halfSize = Size * 0.5;
            double height = Size * Math.Sqrt(3) / 2;

            var p1 = R(new Vector2(Center.X, Center.Y + height * 2 / 3));
            var p2 = R(new Vector2(Center.X - halfSize, Center.Y - height / 3));
            var p3 = R(new Vector2(Center.X + halfSize, Center.Y - height / 3));

            _markEntities.Add(new Line(p1, p2) { color = GetMarkColor() });
            _markEntities.Add(new Line(p2, p3) { color = GetMarkColor() });
            _markEntities.Add(new Line(p3, p1) { color = GetMarkColor() });
        }

        private void GenerateLegacyCircleMark()
        {
            var circle = new Circle
            {
                center = Center,
                radius = Size * 0.5,
                color = GetMarkColor()
            };
            _markEntities.Add(circle);
        }

        private void GenerateLegacySquareMark()
        {
            double halfSize = Size * 0.5;

            var p1 = R(new Vector2(Center.X - halfSize, Center.Y - halfSize));
            var p2 = R(new Vector2(Center.X + halfSize, Center.Y - halfSize));
            var p3 = R(new Vector2(Center.X + halfSize, Center.Y + halfSize));
            var p4 = R(new Vector2(Center.X - halfSize, Center.Y + halfSize));

            _markEntities.Add(new Line(p1, p2) { color = GetMarkColor() });
            _markEntities.Add(new Line(p2, p3) { color = GetMarkColor() });
            _markEntities.Add(new Line(p3, p4) { color = GetMarkColor() });
            _markEntities.Add(new Line(p4, p1) { color = GetMarkColor() });
        }

        private void GenerateLegacyDiamondMark()
        {
            double halfSize = Size * 0.5;

            var p1 = R(new Vector2(Center.X, Center.Y + halfSize));
            var p2 = R(new Vector2(Center.X + halfSize, Center.Y));
            var p3 = R(new Vector2(Center.X, Center.Y - halfSize));
            var p4 = R(new Vector2(Center.X - halfSize, Center.Y));

            _markEntities.Add(new Line(p1, p2) { color = GetMarkColor() });
            _markEntities.Add(new Line(p2, p3) { color = GetMarkColor() });
            _markEntities.Add(new Line(p3, p4) { color = GetMarkColor() });
            _markEntities.Add(new Line(p4, p1) { color = GetMarkColor() });
        }

        #endregion

        private void GenerateText()
        {
            // 文字锚点随符号旋转, 但字形保持水平 (Text 实体不旋转)。
            var textPosition = R(GetTextPosition());
            var text = new Text();
            text.Value = CoatingText;
            text.Position = new LitMath.Vector3(textPosition.X, textPosition.Y, 0.0);
            text.Height = TextSize;
            text.color = GetMarkColor();
            text.alignment = lcdb.TextAlignment.CenterMiddle;
            _markEntities.Add(text);
        }

        private Vector2 GetTextPosition()
        {
            double offset = Size * 0.5 + TextOffset;

            switch (TextPosition)
            {
                case CoatingTextPosition.Right:
                    return new Vector2(Center.X + offset, Center.Y);
                case CoatingTextPosition.Left:
                    return new Vector2(Center.X - offset, Center.Y);
                case CoatingTextPosition.Top:
                    return new Vector2(Center.X, Center.Y + offset);
                case CoatingTextPosition.Bottom:
                    return new Vector2(Center.X, Center.Y - offset);
                case CoatingTextPosition.TopRight:
                    return new Vector2(Center.X + offset * 0.707, Center.Y + offset * 0.707);
                case CoatingTextPosition.TopLeft:
                    return new Vector2(Center.X - offset * 0.707, Center.Y + offset * 0.707);
                case CoatingTextPosition.BottomRight:
                    return new Vector2(Center.X + offset * 0.707, Center.Y - offset * 0.707);
                case CoatingTextPosition.BottomLeft:
                    return new Vector2(Center.X - offset * 0.707, Center.Y - offset * 0.707);
                default:
                    return new Vector2(Center.X + offset, Center.Y);
            }
        }

        private lcdb.Colors.Color GetMarkColor()
        {
            // Story 10-1 (FR-15, AC1):Monochrome 模式短路 — 所有 CoatingType 颜色覆盖为黑色
            // 几何与文字标签不变,只颜色变。打印/客户视觉预览场景。
            if (_lastGeneratedMode == RenderMode.Monochrome)
            {
                return lcdb.Colors.Color.FromColor(System.Drawing.Color.Black);
            }

            switch (CoatingType)
            {
                case CoatingType.AR:
                case CoatingType.BBAR:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.DarkGreen);        // (0,100,0), Story 10-2 WCAG AA
                case CoatingType.HR:
                case CoatingType.OuterReflective:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Crimson);          // (220,20,60), Story 10-2 WCAG AA
                case CoatingType.BS:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.MediumBlue);       // (0,0,205)
                case CoatingType.Filter:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Purple);           // (128,0,128) 已合规
                case CoatingType.Protective:
                    // epics spec 提议 Chocolate (210,105,30) 但 WCAG 仅 3.63:1 < 4.5,改 SaddleBrown (139,69,19) → 7.1:1
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.SaddleBrown);
                case CoatingType.Conductive:
                    // epics spec 提议 DarkGoldenrod (184,134,11) 但 WCAG 仅 3.25:1 < 4.5,
                    // 改自定义 #806000 (128,96,0) 深橄榄金 → 5.9:1,保留"金属导电"语义
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.FromArgb(128, 96, 0));
                case CoatingType.Polarizing:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Teal);             // (0,128,128) 替代 Cyan
                case CoatingType.Blackening:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.Black);
                case CoatingType.PR:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.DarkMagenta);      // (139,0,139) 替代 Magenta
                case CoatingType.Other:
                case CoatingType.Custom:
                default:
                    return lcdb.Colors.Color.FromColor(System.Drawing.Color.DimGray);          // (105,105,105) 替代 Gray
            }
        }

        private string GetCoatingText(CoatingType type)
        {
            switch (type)
            {
                case CoatingType.AR:
                    return "AR";
                case CoatingType.BBAR:
                    return "BBAR";
                case CoatingType.HR:
                    return "HR";
                case CoatingType.OuterReflective:
                    return "OR";
                case CoatingType.BS:
                    return "BS";
                case CoatingType.Filter:
                    return "FLT";  // Story 10-4 (FR-6):F → FLT,避焦距 f / Filter 混淆
                case CoatingType.Protective:
                    return "PRT";  // Story 10-4:P → PRT,避主点 P 混淆
                case CoatingType.Conductive:
                    return "CND";  // Story 10-4:C → CND,避曲率中心 C 混淆
                case CoatingType.Polarizing:
                    return "POL";
                case CoatingType.Blackening:
                    return "BLK";
                case CoatingType.PR:
                    return "PR";
                case CoatingType.Other:
                    return "OTH";  // Story 10-4:O → OTH,避象/物点 O 混淆
                case CoatingType.Custom:
                    return "?";
                default:
                    return "AR";
            }
        }

        /// <summary>
        /// 创建实例
        /// </summary>
        protected override DBObject CreateInstance()
        {
            return new CoatingMark();
        }

        /// <summary>
        /// 克隆函数
        /// </summary>
        public override object Clone()
        {
            CoatingMark mark = base.Clone() as CoatingMark;
            mark.Center = Center;
            mark.Size = Size;
            mark.Rotation = Rotation;
            mark.CoatingType = CoatingType;
            mark.CoatingText = CoatingText;
            mark.TextOffset = TextOffset;
            mark.TextPosition = TextPosition;
            mark.MarkShape = MarkShape;
            mark.ShowText = ShowText;
            mark.TextSize = TextSize;
            mark._markEntities = new List<Entity>();

            return mark;
        }

        /// <summary>
        /// 平移
        /// </summary>
        /// <summary>宿主曲面约束 (贴面生成时设): 拖动锚点沿该面滑。空=自由拖。</summary>
        public SurfaceAttachState? AttachState { get; set; }

        public override void Translate(Vector2 translation)
        {
            Center += translation;
            if (AttachState != null) AttachState.Apex += translation;   // 顶点随标记平移, 保持约束有效
            _markEntities.Clear();
        }

        /// <summary>
        /// 旋转
        /// </summary>
        public override void Rotate(Vector2 center, double angle)
        {
            Center = Vector2.RotateInRadian(Center, center, angle);
            Rotation += angle;
            _markEntities.Clear();
        }

        /// <summary>
        /// Transform
        /// </summary>
        public override void TransformBy(Matrix3 transform)
        {
            Center = transform * Center;
            // 只取线性部分缩放 Size (平移分量不得污染, 否则移动/撤销一次就爆大)
            Vector2 sizeVector = new Vector2(Size, 0);
            Size = (transform * sizeVector - transform * new Vector2(0, 0)).length;
            _markEntities.Clear();
        }

        /// <summary>
        /// 获取夹点
        /// </summary>
        public override List<GripPoint> GetGripPoints()
        {
            List<GripPoint> gripPoints = new List<GripPoint>();
            gripPoints.Add(new GripPoint(GripPointType.Center, Center));

            // 添加大小调整点

            gripPoints.Add(new GripPoint(GripPointType.Quad, Center + new Vector2(Size * 0.5, 0)));

            if (ShowText)
            {
                var textPos = GetTextPosition();
                gripPoints.Add(new GripPoint(GripPointType.Center, textPos));
            }

            return gripPoints;
        }

        /// <summary>
        /// 对象捕捉点
        /// </summary>
        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            List<ObjectSnapPoint> snapPnts = new List<ObjectSnapPoint>();
            
            // 中心捕捉点

            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, Center));
            
            // 四个方向的捕捉点
            double halfSize = Size * 0.5;
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Center + new Vector2(halfSize, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Center + new Vector2(0, halfSize)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Center + new Vector2(-halfSize, 0)));
            snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Quad, Center + new Vector2(0, -halfSize)));
            
            // 如果显示文本，添加文本位置捕捉点
            if (ShowText)
            {
                var textPos = GetTextPosition();
                snapPnts.Add(new ObjectSnapPoint(ObjectSnapMode.Center, textPos));
            }
            
            return snapPnts;
        }

        /// <summary>
        /// 设置夹点
        /// </summary>
        public override void SetGripPointAt(int index, GripPoint gripPoint, Vector2 newPosition)
        {
            switch (index)
            {
                case 0: // 中心点 — 贴面约束时沿曲面滑 (锚点不脱面、朝向随法线)
                    if (AttachState != null)
                    {
                        var (pt, n) = AttachState.Project(newPosition.Y);
                        AttachToSurface(pt, n);
                        return;
                    }
                    Center = newPosition;
                    break;
                case 1: // 大小调整点

                    Size = (newPosition - Center).length * 2;
                    break;
                case 2: // 文本位置点

                    if (ShowText)
                    {
                        var offset = (newPosition - Center).length;
                        TextOffset = offset - Size * 0.5;
                        if (TextOffset < 0) TextOffset = 0;
                    }
                    break;
            }
            _markEntities.Clear();
        }

        #region 绘制方法重写

        /// <summary>
        /// 重写Entity的Draw方法以绘制镀膜标记
        /// </summary>
        // Story 10-1:跟踪上次 Generate 时的渲染模式,模式切换时重新生成以应用颜色覆盖
        private RenderMode _lastGeneratedMode = RenderMode.Normal;

        public override void Draw(IGraphicsDraw gd)
        {
            // Story 10-1 (FR-15, AC1):模式切换 → 触发重生成以更新颜色
            // (Monochrome 短路在 GetMarkColor() 内基于 _lastGeneratedMode 判断)
            if (_markEntities.Count == 0 || _lastGeneratedMode != gd.CurrentMode)
            {
                _lastGeneratedMode = gd.CurrentMode;
                Generate();
            }

            // 绘制所有子实体
            foreach (var entity in _markEntities)
            {
                entity.Draw(gd);
            }
        }

        #endregion
    }
}