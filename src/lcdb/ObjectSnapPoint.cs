using System;

namespace lcdb
{
    /// <summary>
    /// 对象捕捉点
    /// </summary>
    public class ObjectSnapPoint
    {
        /// <summary>
        /// 类型
        /// </summary>
        public ObjectSnapMode type
        {
            get { return _type; }
            set { _type = value; }
        }
        private ObjectSnapMode _type = ObjectSnapMode.Undefined;

        /// <summary>
        /// 点坐标
        /// </summary>
        private LitMath.Vector2 _position = new LitMath.Vector2(0, 0);
        public LitMath.Vector2 position
        {
            get { return _position; }
            set { _position = value; }
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ObjectSnapPoint()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="snapType">捕捉类型</param>
        /// <param name="pos">位置</param>
        public ObjectSnapPoint(ObjectSnapMode snapType, LitMath.Vector2 pos)
        {
            _type = snapType;
            _position = pos;
        }
    }
}
