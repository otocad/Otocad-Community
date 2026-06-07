using System;
using System.Drawing;
using LitMath;

namespace OtoCAD.Presenter.Interfaces.Rendering
{
    /// <summary>
    /// 坐标变换管理器接口
    /// 负责模型坐标和画布坐标之间的转换，以及视图操作
    /// </summary>
    public interface ITransformManager
    {
        #region 属性

        /// <summary>
        /// 当前缩放级别
        /// </summary>
        double Zoom { get; set; }

        /// <summary>
        /// 屏幕平移偏移量
        /// </summary>
        Vector2 ScreenPan { get; set; }

        /// <summary>
        /// 屏幕分辨率(DPI)
        /// </summary>
        float Resolution { get; set; }

        #endregion

        #region 坐标变换方法

        /// <summary>
        /// 将模型坐标转换为画布坐标
        /// </summary>
        Vector2 ModelToCanvas(Vector2 modelPoint);

        /// <summary>
        /// 将画布坐标转换为模型坐标
        /// </summary>
        Vector2 CanvasToModel(Vector2 canvasPoint);

        /// <summary>
        /// 将模型长度转换为画布长度（标量转换，不考虑位置）
        /// </summary>
        double ModelToCanvas(double modelLength);

        /// <summary>
        /// 将画布长度转换为模型长度（标量转换，不考虑位置）
        /// </summary>
        double CanvasToModel(double canvasLength);

        #endregion

        #region 视图操作

        /// <summary>
        /// 放大视图
        /// </summary>
        /// <param name="center">缩放中心点（画布坐标），null表示使用画布中心</param>
        void ZoomIn(Vector2? center = null);

        /// <summary>
        /// 缩小视图
        /// </summary>
        /// <param name="center">缩放中心点（画布坐标），null表示使用画布中心</param>
        void ZoomOut(Vector2? center = null);

        /// <summary>
        /// 缩放到指定级别
        /// </summary>
        /// <param name="zoom">目标缩放级别</param>
        /// <param name="center">缩放中心点（画布坐标），null表示使用画布中心</param>
        void ZoomTo(double zoom, Vector2? center = null);

        /// <summary>
        /// 缩放以显示所有内容
        /// </summary>
        /// <param name="bounds">要显示的边界（模型坐标），null表示使用数据库边界</param>
        void ZoomAll(RectangleF? bounds = null);

        /// <summary>
        /// 平移视图
        /// </summary>
        /// <param name="offset">平移偏移量（画布坐标）</param>
        void PanByCanvas(Vector2 offset);

        /// <summary>
        /// 设置画布尺寸
        /// </summary>
        void SetCanvasSize(float width, float height);

        #endregion

        #region 事件

        /// <summary>
        /// 变换矩阵改变时触发
        /// </summary>
        event EventHandler TransformChanged;

        #endregion
    }
}
