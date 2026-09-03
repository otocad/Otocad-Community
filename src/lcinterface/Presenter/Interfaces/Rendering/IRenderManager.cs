#if WINDOWS
using System;
using System.Drawing;
using System.Collections.Generic;

namespace OtoCAD.Presenter.Interfaces.Rendering
{
    /// <summary>
    /// 渲染管理器接口
    /// 负责管理渲染管线，协调各种渲染阶段
    /// </summary>
    public interface IRenderManager
    {
        #region 渲染控制

        /// <summary>
        /// 使指定区域失效，触发重绘
        /// </summary>
        /// <param name="region">需要重绘的区域，null表示整个画布</param>
        void Invalidate(Rectangle? region = null);

        /// <summary>
        /// 执行渲染
        /// </summary>
        /// <param name="graphics">绘图对象</param>
        /// <param name="clipRect">裁剪矩形</param>
        void Render(Graphics graphics, Rectangle clipRect);

        /// <summary>
        /// 开始批量更新（暂停渲染）
        /// </summary>
        void BeginUpdate();

        /// <summary>
        /// 结束批量更新（恢复渲染）
        /// </summary>
        void EndUpdate();

        #endregion

        #region 渲染选项

        /// <summary>
        /// 渲染质量
        /// </summary>
        RenderQuality Quality { get; set; }

        /// <summary>
        /// 是否启用双缓冲
        /// </summary>
        bool EnableDoubleBuffer { get; set; }

        /// <summary>
        /// 是否启用脏区域优化
        /// </summary>
        bool EnableDirtyRegion { get; set; }

        /// <summary>
        /// 背景颜色
        /// </summary>
        Color BackgroundColor { get; set; }

        #endregion

        #region 渲染管线

        /// <summary>
        /// 添加渲染阶段
        /// </summary>
        void AddRenderStage(IRenderStage stage);

        /// <summary>
        /// 移除渲染阶段
        /// </summary>
        void RemoveRenderStage(IRenderStage stage);

        /// <summary>
        /// 获取所有渲染阶段
        /// </summary>
        IEnumerable<IRenderStage> GetRenderStages();

        /// <summary>
        /// 清除所有渲染阶段
        /// </summary>
        void ClearRenderStages();

        #endregion

        #region 性能监控

        /// <summary>
        /// 获取最后一帧渲染时间（毫秒）
        /// </summary>
        double LastFrameTime { get; }

        /// <summary>
        /// 获取平均帧率
        /// </summary>
        double AverageFPS { get; }

        /// <summary>
        /// 重置性能统计
        /// </summary>
        void ResetPerformanceStats();

        #endregion
    }

    /// <summary>
    /// 渲染阶段接口
    /// </summary>
    public interface IRenderStage
    {
        /// <summary>
        /// 阶段名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 渲染顺序（数值越小越先渲染）
        /// </summary>
        int Order { get; }

        /// <summary>
        /// 是否启用
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// 执行渲染
        /// </summary>
        void Render(RenderContext context);

        /// <summary>
        /// 判断是否需要渲染
        /// </summary>
        bool ShouldRender(RenderContext context);
    }

    /// <summary>
    /// 渲染上下文
    /// </summary>
    public class RenderContext
    {
        /// <summary>
        /// 绘图对象
        /// </summary>
        public Graphics Graphics { get; set; }

        /// <summary>
        /// 变换管理器
        /// </summary>
        public ITransformManager Transform { get; set; }

        /// <summary>
        /// 裁剪矩形
        /// </summary>
        public Rectangle ClipRect { get; set; }

        /// <summary>
        /// 是否为打印模式
        /// </summary>
        public bool IsPrinting { get; set; }

        /// <summary>
        /// 渲染质量
        /// </summary>
        public RenderQuality Quality { get; set; }

        /// <summary>
        /// 自定义数据
        /// </summary>
        public Dictionary<string, object> CustomData { get; set; }
    }

    /// <summary>
    /// 渲染质量枚举
    /// </summary>
    public enum RenderQuality
    {
        /// <summary>
        /// 低质量（快速）
        /// </summary>
        Low,

        /// <summary>
        /// 中等质量
        /// </summary>
        Medium,

        /// <summary>
        /// 高质量（慢速）
        /// </summary>
        High,

        /// <summary>
        /// 打印质量
        /// </summary>
        Print
    }
}
#endif