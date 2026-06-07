using lcdb.Annotation;

namespace lcdb.Standards
{
    /// <summary>出图单位。</summary>
    public enum DrawingUnits
    {
        Millimeter,
        Inch
    }

    /// <summary>
    /// 一套命名的"出图惯例"——决定新建标记 + 下方属性区各行按哪种记法/单位渲染。
    ///
    /// 背景:光学出图惯例不统一(GB / ISO 10110 / MIL-ANSI / JIS / DIN / 各厂 house style),
    /// 不能锁死一种;选一个基准 → 可单项覆盖 → 派生为 "基准名-修改" → 命名保存复用。
    /// 各惯例项复用已有的 per-mark 枚举(<see cref="lcdb.Annotation"/>)作取值来源。
    ///
    /// 分层组合:本类是**顶层(图框级)出图惯例**——选哪种记法/单位 + 引用哪套**底层样式**
    /// (<see cref="DrawingStandard"/>:线型/颜色/图层/文本样式 profile,经 <see cref="DrawingStandardName"/>
    /// 引用、由 <see cref="DrawingStandardManager"/> 解析)。选一个惯例 = 一套记法 + 一套底层样式。
    /// </summary>
    public sealed class DrawingConvention
    {
        /// <summary>标准名(唯一键):如 "GB-ISO 10110" / "MIL-ANSI" / "GB-ISO 10110-修改"。</summary>
        public string Name { get; set; } = "";

        /// <summary>派生自哪个基准(自定义版用);内置预设为 null。</summary>
        public string? BaseName { get; set; }

        /// <summary>内置预设只读(用户库为可改的自定义)。</summary>
        public bool IsBuiltIn { get; set; }

        // -------- 惯例项(取值复用 per-mark 枚举) --------

        /// <summary>疵病记法(ISO 5/N×A · MIL 60-40)。</summary>
        public SurfaceQualityStandard SurfaceQuality { get; set; } = SurfaceQualityStandard.ISO_10110_7;

        /// <summary>面形记法(ISO 3/A(B/C) · PV/RMS)。</summary>
        public FormAccuracyStandard FormAccuracy { get; set; } = FormAccuracyStandard.ISO_10110_5;

        /// <summary>表面结构/粗糙度记法(ISO / DIN / JIS / GB / ANSI)。</summary>
        public RoughnessStandard Roughness { get; set; } = RoughnessStandard.ISO;

        /// <summary>激光损伤测试标准。</summary>
        public LaserDamageTestStandard LaserDamage { get; set; } = LaserDamageTestStandard.ISO21254;

        /// <summary>单位(mm / inch)。</summary>
        public DrawingUnits Units { get; set; } = DrawingUnits.Millimeter;

        /// <summary>尺寸样式 key(对接 Avalonia 端 DimensionStandardService 的标准 key)。</summary>
        public string DimensionStandardKey { get; set; } = "GB-Optical";

        /// <summary>
        /// 底层样式 profile 的引用 key(<see cref="DrawingStandardManager"/> 注册名,如 "GB" / "ISO")。
        /// 决定线型/颜色/图层/文本样式;经 <see cref="ResolveStandard"/> 取活的 <see cref="DrawingStandard"/>。
        /// </summary>
        public string DrawingStandardName { get; set; } = "GB";

        /// <summary>解析底层样式 profile;名字无效返回 null。</summary>
        public DrawingStandard? ResolveStandard() => DrawingStandardManager.GetStandard(DrawingStandardName);

        /// <summary>深拷贝(派生 / 另存为用)。</summary>
        public DrawingConvention Clone() => new DrawingConvention
        {
            Name = Name,
            BaseName = BaseName,
            IsBuiltIn = IsBuiltIn,
            SurfaceQuality = SurfaceQuality,
            FormAccuracy = FormAccuracy,
            Roughness = Roughness,
            LaserDamage = LaserDamage,
            Units = Units,
            DimensionStandardKey = DimensionStandardKey,
            DrawingStandardName = DrawingStandardName,
        };
    }
}
