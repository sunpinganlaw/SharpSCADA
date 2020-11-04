using DataService;
using HslCommunication.ModBus;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace ModbusTCPDriver
{
    [Description("ModbusTcp协议")]
    public class ModbusTCPReader : IPLCDriver
    {
        public int DriverType { get; set; }

        private ModbusTcpNet modbusNet = null;

        int _pdu = 249;
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
            var sindex = address.IndexOf(':');
            if (sindex > 0)
            {
                int slaveId;
                if (int.TryParse(address.Substring(0, sindex), out slaveId))
                    dv.Area = slaveId;
                address = address.Substring(sindex + 1);
            }
            if(address.StartsWith("MW"))
            {
                int index = address.IndexOf('.');
                dv.DBNumber = 3;
                if (index > 0)
                {
                    dv.Start = int.Parse(address.Substring(2, index - 1));
                    dv.Bit = byte.Parse(address.Substring(index + 1));
                }
                else
                    dv.Start = int.Parse(address.Substring(2));
                dv.Start += 1;
                dv.ByteOrder = ByteOrder.Network;
            }
            switch (address[0])
            {
                case '1':
                    {
                        dv.DBNumber = 1;
                        int st;
                        int.TryParse(address, out st);
                        dv.Bit = (byte)(st % 16);
                        st /= 16;
                        dv.Start = st;
                    }
                    break;
                case '2':
                    {
                        dv.DBNumber = 2;
                        int st;
                        int.TryParse(address.Substring(1), out st);
                        dv.Bit = (byte)(st % 16);
                        st /= 16;
                        dv.Start = st;
                    }
                    break;
                case '3':
                    {
                        int index = address.IndexOf('.');
                        dv.DBNumber = 3;
                        if (index > 0)
                        {
                            dv.Start = int.Parse(address.Substring(1, index - 1));
                            dv.Bit = byte.Parse(address.Substring(index + 1));
                        }
                        else
                            dv.Start = int.Parse(address.Substring(1));
                        dv.ByteOrder = ByteOrder.Network;
                    }
                    break;
                case '4':
                    {
                        int index = address.IndexOf('.');
                        dv.DBNumber = 4;
                        if (index > 0)
                        {
                            dv.Start = int.Parse(address.Substring(1, index - 1));
                            dv.Bit = byte.Parse(address.Substring(index + 1));
                        }
                        else
                            dv.Start = int.Parse(address.Substring(1));
                        dv.ByteOrder = ByteOrder.Network;
                    }
                    break;
            }
            return dv;
        }

        public string GetAddress(DeviceAddress address)
        {
            if (address.Area == 0) return address.DBNumber == 4 ? string.Format("x={0};{1}",address.DBNumber, address.Start)
                : address.Start.ToString();
            else return address.DBNumber == 4 ? string.Format("s={0};x={0};{1}", address.Area, address.DBNumber, address.Start)
               : string.Format("s={0};{1}", address.Area, address.Start);
        }

        public string GetAddressBit(DeviceAddress address)
        {
            if (address.Area == 0) return address.DBNumber == 4 ? string.Format("s={0};x={0};{1}.{2}", address.DBNumber, address.Start, address.Bit)
              : address.DBNumber == 3 ? address.Start.ToString() : (address.Start + address.Bit).ToString();
            else return address.DBNumber == 4 ? string.Format("s={0};x={0};{1}.{2}", address.Area, address.DBNumber, address.Start, address.Bit)
              : address.DBNumber == 3 ? string.Format("s={0};{1}", address.Area, address.Start) : string.Format("s={0};{1}", address.Area, address.Start + address.Bit);
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
        int _port = 502; //服务端口
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
                return modbusNet == null || !_connected;
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

        public ModbusTCPReader(IDataServer server, short id, string name)
        {
            _id = id;
            _name = name;
            _server = server;
            modbusNet = new ModbusTcpNet();
        }

        /// <summary>
        /// 连接
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            if (string.IsNullOrEmpty(_ip)) return false;
            modbusNet.IpAddress = _ip;
            modbusNet.Port = _port;
            var result = modbusNet.ConnectServer();
            return result.IsSuccess = _connected;
        }


        public IGroup AddGroup(string name, short id, int updateRate, float deadBand = 0f, bool active = false)
        {
            NetShortGroup grp = new NetShortGroup(id, name, updateRate, active, this);
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
            modbusNet.ConnectClose();
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
            if (address.DBNumber != 1)
            {
                var addr = GetAddress(address);
                var result = modbusNet.Read(addr, length);
                if (result.IsSuccess) return result.Content;
                else
                {
                    _connected = false;
                    if (OnError != null)
                        //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                        OnError(this, new IOErrorEventArgs(result.Message));
                    return null;
                }
            }
            else
            {
                var addr = GetAddress(address);
                var result = address.DBNumber == 2 ? modbusNet.ReadDiscrete(addr, (ushort)(length * 16)) :
                    modbusNet.ReadCoil(addr, (ushort)(length * 16));
                if (result.IsSuccess)
                {
                    var content = result.Content;
                    byte[] data = new byte[2 * length];
                    BitArray array = new BitArray(content);
                    array.CopyTo(data, 0);
                    return data;
                }
                else
                {
                    _connected = false;
                    if (OnError != null)
                        //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                        OnError(this, new IOErrorEventArgs(result.Message));
                    return null;
                }
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
            var result = modbusNet.ReadInt32(addr);
            if (!result.IsSuccess)
                return new ItemData<int>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<int>(result.Content, 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<uint> ReadUInt32(DeviceAddress address)
        {
            var addr = GetAddress(address);
            var result = modbusNet.ReadUInt32(addr);
            if (!result.IsSuccess)
                return new ItemData<uint>(0, 0, QUALITIES.QUALITY_BAD);
            else
                return new ItemData<uint>(result.Content, 0, QUALITIES.QUALITY_GOOD);
        }

        public ItemData<ushort> ReadUInt16(DeviceAddress address)
        {
            var addr = GetAddress(address);
            var result = modbusNet.ReadUInt16(addr);
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
            var result = modbusNet.ReadInt16(addr);
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
            var result = modbusNet.ReadInt16(addr);
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
            var result = modbusNet.ReadString(addr, size);
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
            var result = modbusNet.ReadFloat(addr);
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
            var addr = GetAddressBit(address) ;
            var result = modbusNet.ReadCoil(addr);
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
            var result = modbusNet.Write(addr, bits);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteBit(DeviceAddress address, bool bit)
        {
            var addr = GetAddressBit(address) ;
            var result = modbusNet.Write(addr, bit);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteBits(DeviceAddress address, byte bits)
        {
            var addr = GetAddress(address);
            var result = modbusNet.Write(addr, bits);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteInt16(DeviceAddress address, short value)
        {
            var addr = GetAddress(address);
            var result = modbusNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteUInt16(DeviceAddress address, ushort value)
        {
            var addr = GetAddress(address);
            var result = modbusNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteUInt32(DeviceAddress address, uint value)
        {
            var addr = GetAddress(address);
            var result = modbusNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteInt32(DeviceAddress address, int value)
        {
            var addr = GetAddress(address);
            var result = modbusNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteFloat(DeviceAddress address, float value)
        {
            var addr = GetAddress(address);
            var result = modbusNet.Write(addr, value);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
                return -result.ErrorCode;
            }
        }

        public int WriteString(DeviceAddress address, string str)
        {
            var addr = GetAddress(address);
            var result = modbusNet.Write(addr, str);
            if (result.IsSuccess) return 0;
            else
            {
                _connected = false;
                if (OnError != null)
                    //OnError(this, new IOErrorEventArgs(this.ID, result.Message));
                    OnError(this, new IOErrorEventArgs(result.Message));
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

