using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using LitMath;
using lcdb;

namespace lcdb
{
    public class ToleranceAnnotation : Entity
    {
        private Vector2 _position;
        private string _text;
        private double _textHeight;
        private BasicTolerance _tolerance;
        private bool _showFrame;
        private lcdb.TextAlignment _alignment;

        public ToleranceAnnotation()
        {
            _textHeight = 2.5;
            _showFrame = true;
            _alignment = lcdb.TextAlignment.CenterMiddle;
        }

        public ToleranceAnnotation(Vector2 position, BasicTolerance tolerance)
        {
            _position = position;
            _tolerance = tolerance;
            _textHeight = 2.5;
            _showFrame = true;
            _alignment = lcdb.TextAlignment.CenterMiddle;
            UpdateText();
        }

        public Vector2 Position
        {
            get { return _position; }
            set 
            { 
                _position = value;
            }
        }

        public BasicTolerance Tolerance
        {
            get { return _tolerance; }
            set 
            { 
                _tolerance = value;
                UpdateText();
            }
        }

        public double TextHeight
        {
            get { return _textHeight; }
            set 
            { 
                _textHeight = value;
            }
        }

        public bool ShowFrame
        {
            get { return _showFrame; }
            set 
            { 
                _showFrame = value;
            }
        }

        public lcdb.TextAlignment Alignment
        {
            get { return _alignment; }
            set 
            { 
                _alignment = value;
            }
        }

        private void UpdateText()
        {
            if (_tolerance != null)
            {
                _text = _tolerance.GetFormattedString();
            }
            else
            {
                _text = "";
            }
        }

        public override void Draw(OtoCAD.IGraphicsDraw gd)
        {
            if (_tolerance == null)
                return;

            double h = _textHeight;
            double totalW = StackedWidth(_tolerance, h);

            // 绘制边框（如果需要）
            if (_showFrame)
            {
                gd.DrawRectangle(
                    new Vector2(_position.X - (totalW + 4) / 2, _position.Y - (h * 1.5 + 4) / 2),
                    totalW + 4,
                    h * 1.5 + 4
                );
            }

            // 形心居中: 左基线 = 形心左移半宽、下移约 0.35h
            var left = new Vector2(_position.X - totalW / 2, _position.Y - h * 0.35);
            DrawStacked(gd, left, "", _tolerance, h, "Arial");
        }

        /// <summary>
        /// 按规范绘制公差文本: 对称偏差 → "公称±dev" 行内; 上下偏差不等 → 上偏差在上、下偏差在下"摞起来"(小字号).
        /// 渲染器左对齐基线 → <paramref name="position"/> 视为左基线点. 返回末端右基线点. dimension 等可复用.
        /// </summary>
        public static Vector2 DrawStacked(OtoCAD.IGraphicsDraw gd, Vector2 position, string prefix,
            BasicTolerance tol, double height, string font)
        {
            double up = tol.UpperDeviation, lo = tol.LowerDeviation;
            var end = gd.DrawText(position, $"{prefix}{tol.NominalSize:F2}", height, font, lcdb.TextAlignment.LeftBottom, 0);

            if (Math.Abs(up) < 1e-4 && Math.Abs(lo) < 1e-4)            // 无公差
                return end;
            if (Math.Abs(up + lo) < 1e-4)                              // 对称 → ±
                return gd.DrawText(end, $" ±{Math.Abs(up):F3}", height, font, lcdb.TextAlignment.LeftBottom, 0);

            // 上下偏差摞起来 (小字号, 各带正负号): 上偏差在上行, 下偏差在下行
            double dh = height * 0.62, gap = height * 0.12;
            string upText = (up >= 0 ? "+" : "-") + Math.Abs(up).ToString("F3");
            string loText = (lo >= 0 ? "+" : "-") + Math.Abs(lo).ToString("F3");
            var upEnd = gd.DrawText(new Vector2(end.X + gap, position.Y + height * 0.48), upText, dh, font, lcdb.TextAlignment.LeftBottom, 0);
            var loEnd = gd.DrawText(new Vector2(end.X + gap, position.Y - height * 0.10), loText, dh, font, lcdb.TextAlignment.LeftBottom, 0);
            return new Vector2(Math.Max(upEnd.X, loEnd.X), position.Y);
        }

        /// <summary>估算 <see cref="DrawStacked"/> 总宽 (用于居中/边框/包围盒, 与渲染近似一致).</summary>
        public static double StackedWidth(BasicTolerance tol, double height, string prefix = "")
        {
            double charW = height * 0.55;
            double w = (prefix.Length + $"{tol.NominalSize:F2}".Length) * charW;
            double up = tol.UpperDeviation, lo = tol.LowerDeviation;
            if (Math.Abs(up) < 1e-4 && Math.Abs(lo) < 1e-4) return w;
            if (Math.Abs(up + lo) < 1e-4) return w + $" ±{Math.Abs(up):F3}".Length * charW;
            int devLen = Math.Max(
                ((up >= 0 ? "+" : "-") + Math.Abs(up).ToString("F3")).Length,
                ((lo >= 0 ? "+" : "-") + Math.Abs(lo).ToString("F3")).Length);
            return w + height * 0.12 + devLen * (height * 0.62 * charW / height);
        }

        public override Bounding bounding
        {
            get
            {
                if (_tolerance == null)
                    return new Bounding();

                var width = StackedWidth(_tolerance, _textHeight);
                var height = _textHeight * 1.5;

                return new Bounding(
                    _position.X - width / 2,
                    _position.Y - height / 2,
                    _position.X + width / 2,
                    _position.Y + height / 2
                );
            }
        }

        public override object Clone()
        {
            var copy = new ToleranceAnnotation();
            copy._position = _position;
            copy._textHeight = _textHeight;
            copy._showFrame = _showFrame;
            copy._alignment = _alignment;
            
            if (_tolerance != null)
            {
                copy._tolerance = _tolerance.Clone() as BasicTolerance;
            }
            
            copy.UpdateText();
            return copy;
        }

        public override void Translate(LitMath.Vector2 translation)
        {
            _position = _position + translation;
        }

        public override void TransformBy(LitMath.Matrix3 transform)
        {
            _position = transform * _position;
            
            // 缩放文本高度
            var scale = Math.Sqrt(transform.m11 * transform.m11 + transform.m12 * transform.m12);
            _textHeight *= scale;
            
        }

        public override List<GripPoint> GetGripPoints()
        {
            return new List<GripPoint>
            {
                new GripPoint(GripPointType.End, _position)
            };
        }

        public override void SetGripPointAt(int index, GripPoint gripPoint, LitMath.Vector2 newPosition)
        {
            if (index == 0 && gripPoint.type == GripPointType.End)
            {
                _position = newPosition;
            }
        }

        public override List<ObjectSnapPoint> GetSnapPoints()
        {
            return new List<ObjectSnapPoint>
            {
                new ObjectSnapPoint(ObjectSnapMode.Center, _position)
            };
        }

        public override void Rotate(LitMath.Vector2 center, double angle)
        {
            // 旋转位置点
            _position = LitMath.Vector2.RotateInRadian(_position, center, angle);
        }

        protected override DBObject CreateInstance()
        {
            return new ToleranceAnnotation();
        }
    }
}