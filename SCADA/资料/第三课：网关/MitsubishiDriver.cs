using DataService;
using HslCommunication.Profinet.Melsec;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace MitsubishiDriver
{
    [Description("三菱MC协议")]
    public class MitsubishiReader : IPLCDriver
    {
        private MelsecMcNet melseNet = null;
        public int DriverType { get; set; }

        int _pdu = 127;
        /// <summary>
        /// 获取PDU的值
        /// </summary>
        public int PDU
        {
            get { return _pdu; }
            set { _pdu = value; }
        }
        /// <summary>
        /// 获取设备地址
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public DeviceAddress GetDeviceAddress(string address)
        {
            DeviceAddress dv = DeviceAddress.Empty;
            if (string.IsNullOrEmpty(address))
                return dv;

            var std = address[0];
            dv.DBNumber = std;
            if (std == 'X' || std == 'Y')
            {
                int st = Convert.ToInt32(address.Substring(1), 10);
                dv.Bit = (byte)(st % 16);
                st /= 16;
                dv.Start = st;
            }
            else
            {
                int index = address.IndexOf('.');
                if (index > 0)
                {
                    dv.Start = int.Parse(address.Substring(1, index - 1));
                    dv.Bit = byte.Parse(address.Substring(index + 1));
                }
                else
                    dv.Start = int.Parse(address.Substring(1));
            }
            return dv;
        }

        public string GetAddress(DeviceAddress address)
        {
            return ((char)address.DBNumber) + address.Start.ToString();
        }

        private int _timeout;//超时数据

        short _id;//驱动id
        //驱动id
        public short ID
        {
            get
            {
                return _id;
            }
        }

        string _name;//驱动名称
        /// <summary>
        /// 驱动名称
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        string _ip;//服务ip
        int _port = 9600; //服务端口
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        public string ServerName
        {
            get { return _ip; }
            set { _ip = value; }
        }

        bool _connected = false;
        /// <summary>
        /// 是否关闭
        /// </summary>
        public bool IsClosed
        {
            get
            {
                return melseNet == null || !_connected;
            }
        }
        /// <summary>
        /// 超时时间
        /// </summary>

        public int TimeOut
        {
            get { return _timeout; }
            set { _timeout = value; }
        }

        List<IGroup> _grps = new List<IGroup>(20);
        public IEnumerable<IGroup> Groups
        {
            get { return _grps; }
        }

        IDataServer _server;
        public IDataServer Parent
        {
            get { return _server; }
        }

        /// <summary>
        /// 设备的标识号
        /// </summary>
        public byte SID { get; set; } = 0x00;

        public MitsubishiReader(IDataServer server, short id, string name)
        {
            _id = id;
            _name = name;
            _server = server;
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            if (string.IsNullOrEmpty(_ip)) return false;
            if (melseNet != null)
            {
                try
                {
                    melseNet.ConnectClose();
                    melseNet.Dispose();
                }
                catch (Exception err)
                {
                    if (OnError != null)
                        OnError(this, new IOErrorEventArgs(this.ID, err.Message));
                }
            }
            melseNet = new MelsecMcNet();
            melseNet.IpAddress = _ip;
            melseNet.Port = _port;
            melseNet.ReceiveTimeOut = _timeout;
            var result = melseNet.ConnectServer();
            return _connected = true;
        }


        public IGroup AddGroup(string name, short id, int updateRate, float deadBand = 0f, bool active = false)
        {
            ShortGroup grp = new ShortGroup(id, name, updateRate, active, this);
            _grps.Add(grp);
            return grp;
        }

        public bool RemoveGroup(IGroup grp)
        {
            grp.IsActive = false;
            return _grps.Remove(grp);
        }

        public void Dispose()
        {
            melseNet.Dispose();
            _connected = false;
            foreach (IGroup grp in _grps)
            {
                grp.Dispose();
            }
            _grps.Clear();
        }

        /// <summary>
        /// 读取字节数组
        /// </summary>
        /// <param name="address">标签变量地址结构</param>
        /// <param name="size">长度,</param>
        /// <returns></returns>
        public byte[] ReadBytes(DeviceAddress address, ushort length)
        {
            if (!_connected) return null;
            //Thread.Sleep(2000);
            var addr = GetAddress(address);
            var result = melseNet.Read(addr, length);
            if (result.IsSuccess) return result.Content;
            else
            {
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                if (result.Message.Contains("连接失败")) _connected = false;
                //_connected = false;
                //Connect();
                return null;
            }
        }
        /// <summary>
        /// 读取32位整数
        /// </summary>
        /// <param name="address">标签变量地址结构</param>
        /// <returns></returns>
        public ItemData<int> ReadInt32(DeviceAddress address)
        {
            var addr = GetAddress(address);
            var result = melseNet.ReadInt32(addr);
            if (!result.IsSuccess)
                return new ItemData<int>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<int>(result.Content, 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<uint> ReadUInt32(DeviceAddress address)
        {
            var addr = GetAddress(address);
            var result = melseNet.ReadUInt32(addr);
            if (!result.IsSuccess)
                return new ItemData<uint>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<uint>(result.Content, 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<ushort> ReadUInt16(DeviceAddress address)
        {
            var addr = GetAddress(address);
            var result = melseNet.ReadUInt16(addr);
            if (!result.IsSuccess)
                return new ItemData<ushort>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<ushort>(result.Content, 0, QUALITIES.QUALITY_GOOD);
        }
        /// <summary>
        /// 读取16位整数
        /// </summary>
        /// <param name="address">标签变量地址结构</param>
        /// <returns></returns>
        public ItemData<short> ReadInt16(DeviceAddress address)
        {
            var addr = GetAddress(address);
            var result = melseNet.ReadInt16(addr);
            if (!result.IsSuccess)
                return new ItemData<short>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<short>(result.Content, 0, QUALITIES.QUALITY_GOOD);
        }
        /// <summary>
        /// 读取1字节
        /// </summary>
        /// <param name="address">标签变量地址结构</param>
        /// <returns></returns>
        public ItemData<byte> ReadByte(DeviceAddress address)
        {
            var addr = GetAddress(address);
            var result = melseNet.ReadInt16(addr);
            if (!result.IsSuccess)
                return new ItemData<byte>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<byte>((byte)result.Content, 0, QUALITIES.QUALITY_GOOD);
        }
        /// <summary>
        /// 读取字符串
        /// </summary>
        /// <param name="address">标签变量地址结构</param>
        /// <param name="size">长度</param>
        /// <returns></returns>
        public ItemData<string> ReadString(DeviceAddress address, ushort size)
        {
            var addr = GetAddress(address);
            var result = melseNet.ReadString(addr, size);
            if (!result.IsSuccess)
                return new ItemData<string>(string.Empty, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<string>(result.Content, 0, QUALITIES.QUALITY_GOOD);//是否考虑字节序问题？
        }
        /// <summary>
        /// 读取32位浮点数 
        /// </summary>
        /// <param name="address">标签变量地址结构</param>
        /// <returns></returns>
        public ItemData<float> ReadFloat(DeviceAddress address)
        {
            var addr = GetAddress(address);
            var result = melseNet.ReadFloat(addr);
            if (!result.IsSuccess)
                return new ItemData<float>(0.0f, 0, QUALITIES.QUALITY_BAD);
            else return new ItemData<float>(result.Content, 0, QUALITIES.QUALITY_GOOD);//是否考虑字节序问题？
        }
        /// <summary>
        /// 读取1位
        /// </summary>
        /// <param name="address">标签变量地址结构体</param>
        /// <returns></returns>
        public ItemData<bool> ReadBit(DeviceAddress address)
        {
            var addr = address.DBNumber == 'X' || address.DBNumber == 'Y' ?
                ((char)address.DBNumber) + (address.Start + address.Bit).ToString() :
               GetAddress(address) + "." + address.Bit.ToString();
            var result = melseNet.ReadBool(addr);
            if (!result.IsSuccess)
                return new ItemData<bool>(false, 0, QUALITIES.QUALITY_BAD);
            else return new ItemData<bool>(result.Content, 0, QUALITIES.QUALITY_GOOD);//是否考虑字节序问题？
        }
        /// <summary>
        /// 读object类型
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public ItemData<object> ReadValue(DeviceAddress address)
        {
            return this.ReadValueEx(address);
        }
        /// <summary>
        /// 写字节数组到设备
        /// </summary>
        /// <param name="address">标签变量地址结构</param>
        /// <param name="bits">需写的字节数组</param>
        /// <returns></returns>
        public int WriteBytes(DeviceAddress address, byte[] bits)
        {
            var addr = GetAddress(address);
            var result = melseNet.Write(addr, bits);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteBit(DeviceAddress address, bool bit)
        {
            var addr = address.DBNumber == 'X' || address.DBNumber == 'Y' ?
                ((char)address.DBNumber) + (address.Start + address.Bit).ToString() :
               GetAddress(address) + "." + address.Bit.ToString();
            var result = melseNet.Write(addr, bit);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteBits(DeviceAddress address, byte bits)
        {
            var addr = GetAddress(address);
            var result = melseNet.Write(addr, bits);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteInt16(DeviceAddress address, short value)
        {
            var addr = GetAddress(address);
            var result = melseNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteUInt16(DeviceAddress address, ushort value)
        {
            var addr = GetAddress(address);
            var result = melseNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteUInt32(DeviceAddress address, uint value)
        {
            var addr = GetAddress(address);
            var result = melseNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteInt32(DeviceAddress address, int value)
        {
            var addr = GetAddress(address);
            var result = melseNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteFloat(DeviceAddress address, float value)
        {
            var addr = GetAddress(address);
            var result = melseNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteString(DeviceAddress address, string str)
        {
            var addr = GetAddress(address);
            var result = melseNet.Write(addr, str);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteValue(DeviceAddress address, object value)
        {
            return this.WriteValueEx(address, value);
        }

        public event IOErrorEventHandler OnError;

        public int Limit
        {
            get { return 960; }
        }


    }
}