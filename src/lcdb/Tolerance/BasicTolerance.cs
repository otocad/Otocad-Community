using System;
using lcdb;

namespace lcdb
{
    public enum ToleranceType
    {
        Symmetrical,        // ±公差
        Bilateral,         // 上下偏差
        Unilateral         // 单向偏差
    }

    public enum ToleranceGrade
    {
        IT6 = 6,
        IT7 = 7,
        IT8 = 8,
        IT9 = 9,
        IT10 = 10,
        IT11 = 11
    }

    public class BasicTolerance : DBObject
    {
        private double _nominalSize;
        private double _upperDeviation;
        private double _lowerDeviation;
        private ToleranceType _toleranceType;
        private ToleranceGrade? _toleranceGrade;

        public BasicTolerance()
        {
            _toleranceType = ToleranceType.Symmetrical;
            _upperDeviation = 0.0;
            _lowerDeviation = 0.0;
        }

        public BasicTolerance(double nominalSize, double upperDeviation, double lowerDeviation)
        {
            _nominalSize = nominalSize;
            _upperDeviation = upperDeviation;
            _lowerDeviation = lowerDeviation;
            UpdateToleranceType();
        }

        public BasicTolerance(double nominalSize, double tolerance)
        {
            _nominalSize = nominalSize;
            _upperDeviation = Math.Abs(tolerance);
            _lowerDeviation = -Math.Abs(tolerance);
            _toleranceType = ToleranceType.Symmetrical;
        }

        public double NominalSize
        {
            get { return _nominalSize; }
            set 
            { 
                _nominalSize = value;
            }
        }

        public double UpperDeviation
        {
            get { return _upperDeviation; }
            set 
            { 
                _upperDeviation = value;
                UpdateToleranceType();
            }
        }

        public double LowerDeviation
        {
            get { return _lowerDeviation; }
            set 
            { 
                _lowerDeviation = value;
                UpdateToleranceType();
            }
        }

        public ToleranceType ToleranceType
        {
            get { return _toleranceType; }
            set 
            { 
                _toleranceType = value;
            }
        }

        public ToleranceGrade? ToleranceGrade
        {
            get { return _toleranceGrade; }
            set 
            { 
                _toleranceGrade = value;
                if (value.HasValue)
                {
                    ApplyStandardTolerance(value.Value);
                }
            }
        }

        public double MaxLimit
        {
            get { return _nominalSize + _upperDeviation; }
        }

        public double MinLimit
        {
            get { return _nominalSize + _lowerDeviation; }
        }

        public double ToleranceValue
        {
            get { return _upperDeviation - _lowerDeviation; }
        }

        private void UpdateToleranceType()
        {
            if (Math.Abs(_upperDeviation + _lowerDeviation) < 0.0001)
            {
                _toleranceType = ToleranceType.Symmetrical;
            }
            else if (_upperDeviation > 0 && _lowerDeviation < 0)
            {
                _toleranceType = ToleranceType.Bilateral;
            }
            else
            {
                _toleranceType = ToleranceType.Unilateral;
            }
        }

        private void ApplyStandardTolerance(ToleranceGrade grade)
        {
            var table = new ToleranceTable();
            var standardTolerance = table.GetStandardTolerance(_nominalSize, grade);
            if (standardTolerance != null)
            {
                _upperDeviation = standardTolerance.UpperDeviation;
                _lowerDeviation = standardTolerance.LowerDeviation;
                UpdateToleranceType();
            }
        }

        public string GetFormattedString()
        {
            switch (_toleranceType)
            {
                case ToleranceType.Symmetrical:
                    if (Math.Abs(_upperDeviation) < 0.0001)
                        return $"{_nominalSize:F2}";
                    return $"{_nominalSize:F2}±{_upperDeviation:F3}";
                
                case ToleranceType.Bilateral:
                case ToleranceType.Unilateral:
                    return $"{_nominalSize:F2}{{\\H0.7x;\\S+{_upperDeviation:F3}^-{Math.Abs(_lowerDeviation):F3};}}";
                
                default:
                    return $"{_nominalSize:F2}";
            }
        }

        public override object Clone()
        {
            BasicTolerance clone = base.Clone() as BasicTolerance;
            clone._nominalSize = _nominalSize;
            clone._upperDeviation = _upperDeviation;
            clone._lowerDeviation = _lowerDeviation;
            clone._toleranceType = _toleranceType;
            clone._toleranceGrade = _toleranceGrade;
            return clone;
        }

        protected override DBObject CreateInstance()
        {
            return new BasicTolerance();
        }
    }
}