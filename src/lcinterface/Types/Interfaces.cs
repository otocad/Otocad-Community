using System;
using System.Drawing;

namespace OtoCAD.Interfaces
{
    /// <summary>
    /// 对象ID接口
    /// </summary>
    public interface IObjectId
    {
        bool IsNull { get; }
        string ToString();
    }

    /// <summary>
    /// 颜色接口
    /// </summary>
    public interface IColor
    {
        bool IsByLayer { get; }
        bool IsByBlock { get; }
        Color ToColor();
        byte R { get; }
        byte G { get; }
        byte B { get; }
    }

    /// <summary>
    /// 线宽接口
    /// </summary>
    public interface ILineWeight
    {
        float Value { get; }
        bool IsByLayer { get; }
        bool IsByBlock { get; }
        bool IsDefault { get; }
    }

    /// <summary>
    /// 图层接口（统一契约）
    /// </summary>
    public interface ILayer
    {
        IObjectId Id { get; }
        string Name { get; }
        IColor Color { get; }
        ILineWeight LineWeight { get; }
        bool IsVisible { get; }
        bool IsLocked { get; }
        bool IsFrozen { get; }
    }

    /// <summary>
    /// 实体接口（避免直接依赖Entity类）
    /// </summary>
    public interface IEntity
    {
        IObjectId Id { get; }
        IObjectId LayerId { get; }
        IColor Color { get; }
        ILineWeight LineWeight { get; }
        void Draw(object graphicsDraw);
    }
}
