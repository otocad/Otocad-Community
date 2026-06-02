using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OtoCAD.OpticEntity;

namespace lcdb
{
    /// <summary>
    /// ���������ݿ����
    /// </summary>
    public abstract class DBTableRecord : DBObject

    {
        /// <summary>
        /// ����
        /// </summary>
        public override string className
        {
            get { return "DBTableRecord"; }
        }
        public string ER { get; set; } = "RE";
        /// <summary>
        /// ����
        /// </summary>
        protected string _name = "";
        public string name
        {
            get { return _name; }
            set { _name = value; }
        }

  
        /// <summary>
        /// ���ݱ�
        /// </summary>
        internal DBTable _datable = null;
        public override DBTable dbtable => _datable;

        /// <summary>
        /// ��¡����
        /// </summary>
        public override object Clone()
        {
            DBTableRecord tblRec = base.Clone() as DBTableRecord;
            tblRec._name = _name;
            tblRec._datable = null;
            return tblRec;
        }

        /// <summary>
        /// ɾ��
        /// </summary>
        protected override void _Erase()
        {
            if (_datable != null)
            {
                _datable.Remove(this);
            }
        }

        /// <summary>
        /// дXML
        /// </summary>

        /// <summary>
        /// ��XML
        /// </summary>
    }
}
