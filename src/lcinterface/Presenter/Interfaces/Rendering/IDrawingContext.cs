#if WINDOWS
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using LitMath;
using OtoCAD.Interfaces;

namespace OtoCAD.Presenter.Interfaces.Rendering
{
    /// <summary>
    /// 绘图上下文接口
    /// 提供统一的绘图API，封装GDI+操作
    /// </summary>
    public interface IDrawingContext
    {
        #region 基本图形绘制

        /// <summary>
        /// 绘制线段
        /// </summary>
        void DrawLine(Vector2 start, Vector2 end, Pen pen = null);

        /// <summary>
        /// 绘制多段线
        /// </summary>
        void DrawPolyline(Vector2[] points, bool closed = false, Pen pen = null);

        /// <summary>
        /// 绘制圆
        /// </summary>
        void DrawCircle(Vector2 center, double radius, Pen pen = null);

        /// <summary>
        /// 绘制圆弧
        /// </summary>
        void DrawArc(Vector2 center, double radius, double startAngle, double endAngle, Pen pen = null);

        /// <summary>
        /// 绘制椭圆
        /// </summary>
        void DrawEllipse(Vector2 center, double majorRadius, double minorRadius, double rotation, Pen pen = null);

        /// <summary>
        /// 绘制矩形
        /// </summary>
        void DrawRectangle(RectangleF rect, Pen pen = null);

        /// <summary>
        /// 绘制点
        /// </summary>
        void DrawPoint(Vector2 point, float size = 3, Pen pen = null);

        /// <summary>
        /// 绘制文本
        /// </summary>
        void DrawText(string text, Vector2 position, Font font = null, Brush brush = null, StringFormat format = null);

        #endregion

        #region 填充图形

        /// <summary>
        /// 填充圆
        /// </summary>
        void FillCircle(Vector2 center, double radius, Brush brush = null);

        /// <summary>
        /// 填充椭圆
        /// </summary>
        void FillEllipse(Vector2 center, double majorRadius, double minorRadius, double rotation, Brush brush = null);

        /// <summary>
        /// 填充矩形
        /// </summary>
        void FillRectangle(RectangleF rect, Brush brush = null);

        /// <summary>
        /// 填充多边形
        /// </summary>
        void FillPolygon(Vector2[] points, Brush brush = null);

        #endregion

        #region 实体绘制

        /// <summary>
        /// 绘制实体
        /// </summary>
        void DrawEntity(IEntity entity, DrawingStyle style = DrawingStyle.Normal);

        /// <summary>
        /// 创建实体的画笔
        /// </summary>
        Pen CreatePenForEntity(IEntity entity, DrawingStyle style = DrawingStyle.Normal);

        /// <summary>
        /// 创建实体的画刷
        /// </summary>
        Brush CreateBrushForEntity(IEntity entity, DrawingStyle style = DrawingStyle.Normal);

        #endregion

        #region 坐标变换

        /// <summary>
        /// 推入变换矩阵
        /// </summary>
        void PushTransform(Matrix transform);

        /// <summary>
        /// 弹出变换矩阵
        /// </summary>
        void PopTransform();

        /// <summary>
        /// 重置变换
        /// </summary>
        void ResetTransform();

        #endregion

        #region 裁剪

        /// <summary>
        /// 设置裁剪区域
        /// </summary>
        void SetClip(RectangleF rect);

        /// <summary>
        /// 设置裁剪区域
        /// </summary>
        void SetClip(Region region);

        /// <summary>
        /// 重置裁剪区域
        /// </summary>
        void ResetClip();

        #endregion

        #region 属性

        /// <summary>
        /// 底层Graphics对象
        /// </summary>
        Graphics Graphics { get; }

        /// <summary>
        /// 变换管理器
        /// </summary>
        ITransformManager Transform { get; }

        /// <summary>
        /// 是否为模型空间坐标（true）还是画布空间坐标（false）
        /// </summary>
        bool IsModelSpace { get; set; }

        /// <summary>
        /// 当前画笔
        /// </summary>
        Pen CurrentPen { get; set; }

        /// <summary>
        /// 当前画刷
        /// </summary>
        Brush CurrentBrush { get; set; }

        /// <summary>
        /// 当前字体
        /// </summary>
        Font CurrentFont { get; set; }

        #endregion
    }

    /// <summary>
    /// 绘制样式
    /// </summary>
    public enum DrawingStyle
    {
        /// <summary>
        /// 正常样式
        /// </summary>
        Normal,

        /// <summary>
        /// 选中样式
        /// </summary>
        Selected,

        /// <summary>
        /// 高亮样式
        /// </summary>
        Highlighted,

        /// <summary>
        /// 预览样式
        /// </summary>
        Preview,

        /// <summary>
        /// 虚影样式
        /// </summary>
        Ghost
    }
}
#endif