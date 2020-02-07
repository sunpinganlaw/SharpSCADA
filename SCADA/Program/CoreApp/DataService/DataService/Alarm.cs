using System;
using System.ComponentModel;

namespace DataService
{
   
    public class AlarmItem : IComparable<AlarmItem>, INotifyPropertyChanged
    {
        int _condiId;
       
        Severity _severity;
        SubAlarmType _alarmType;
        DateTime _startTime;
        TimeSpan _duration;       
        object _alarmValue;
        string _alarmText;
        string _source;

        public SubAlarmType SubAlarmType
        {
            get
            {
                return _alarmType;
            }
            set
            {
                _alarmType = value;
            }
        }

        public Severity Severity
        {
            get
            {
                return _severity;
            }
            set
            {
                _severity = value;
            }
        }

        public DateTime StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                _startTime = value;
            }
        }

        public int ConditionId
        {
            get
            {
                return _condiId;
            }
            set
            {
                _condiId = value;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                //return _endTime-_startTime;
                return _duration;
            }
            set
            {
                _duration = value;
                OnPropertyChanged("Duration");
            }
        }

        public object AlarmValue
        {
            get
            {
                return _alarmValue;
            }
            set
            {
                _alarmValue = value;
            }
        }

        public string AlarmText
        {
            get
            {
                return _alarmText;
            }
            set
            {
                _alarmText = value;
            }
        }

        public string Source
        {
            get
            {
                return _source;
            }
            set
            {
                _source = value;
            }
        }

        public AlarmItem(DateTime time, string alarmText, object alarmValue, SubAlarmType type, Severity severity, int condId, string source)
        {
            this._startTime = time;
            this._alarmType = type;
            this._alarmText = alarmText;
            this._alarmValue = alarmValue;
            this._severity = severity;
            this._condiId = condId;
            this._source = source;
        }

        public AlarmItem()
        {
            this._startTime = DateTime.Now;
            this._alarmType = SubAlarmType.None;
            this._alarmText = string.Empty;
            this._severity = Severity.Normal;
            this._condiId = -1;
            this._source = string.Empty;
        }

        #region IComparable<AlarmItem> Members

        public int CompareTo(AlarmItem other)
        {
            return this._startTime.CompareTo(other._startTime);
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {

                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
    /// <summary>
    /// 【变化率报警=3（高，低，死区，延时，报警级别，EventType=4）】；【质量报警=5（延时，报警级别，EventType=4）】
    /// </summary>
    [Flags]
    public enum AlarmType
    {
        None = 0,
        /// <summary>
        /// 差限报警（低，高，低低，高高，死区，延时，报警级别，EventType=4）
        /// </summary>
        Level = 1,
        /// <summary>
        /// 偏差报警（上偏差，下偏差，死区，延时，参数 报警级别，EventType=4）
        /// </summary>
        Dev = 2,
        /// <summary>
        /// 开关量报警(延时，正触发，负触发，报警级别，EventType=4）
        /// </summary>
        Dsc = 4,
        ROC = 8,
        Quality = 16,
        Complex = 32,
        WordDsc = 64
    }

    [Flags]
    public enum SubAlarmType
    {
        None = 0,
        /// <summary>
        /// 低低（差限报警）
        /// </summary>
        LoLo = 1,
        /// <summary>
        ///  低（差限报警）
        /// </summary>
        Low = 2,
        /// <summary>
        /// 高（差限报警）
        /// </summary>
        High = 4,
        /// <summary>
        /// 高高（差限报警）
        /// </summary>
        HiHi = 8,
        /// <summary>
        /// 上偏差（偏差报警）
        /// </summary>
        MajDev = 16,
        /// <summary>
        /// 下偏差（偏差报警）
        /// </summary>
        MinDev = 32,
        /// <summary>
        /// 开关量报警（正触发=Threshold=1;负触发=Threshold=0）
        /// </summary>
        Dsc = 64,
        /// <summary>
        /// 通信质量报警
        /// </summary>
        BadPV = 128,
        /// <summary>
        /// 变化率报警（高）
        /// </summary>
        MajROC = 256,
        /// <summary>
        /// 变化率报警（低）
        /// </summary>
        MinROC = 512
    }

    public enum Severity 
    {
        Error = 7,
        High = 6,
        MediumHigh = 5,
        Medium = 4,
        MediumLow = 3,
        Low = 2,
        Information = 1,
        Normal = 0
    }

    [Flags]
    public enum ConditionState : byte
    {
        Acked = 4,
        Actived = 2,
        Enabled = 1
    }

    public enum EventType : byte
    {
        Simple = 1,
        /// <summary>
        /// 归档用
        /// </summary>
        TraceEvent = 2,
        ConditionEvent = 4,
    }

    public enum ConditionType : byte
    {
        /// <summary>
        /// 绝对值（偏差报警用）
        /// </summary>
        Absolute = 0,
        /// <summary>
        /// 百分比（偏差报警用）
        /// </summary>
        Percent = 1
    }

}
