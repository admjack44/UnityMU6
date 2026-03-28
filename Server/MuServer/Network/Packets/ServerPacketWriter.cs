using System.Text;

namespace MuServer.Network.Packets
{
    public enum PacketType : byte
    {
        C1 = 0xC1,
        C2 = 0xC2,
        C3 = 0xC3,
        C4 = 0xC4
    }

    public class ServerPacketWriter
    {
        private readonly byte[] _buffer;
        private int _pos;
        private readonly PacketType _type;

        public ServerPacketWriter(PacketType type, byte headCode, int extraCapacity = 64)
        {
            _type = type;
            bool isLong = type == PacketType.C2 || type == PacketType.C4;
            int headerSize = isLong ? 4 : 3;
            _buffer = new byte[headerSize + extraCapacity];
            _buffer[0] = (byte)type;
            _pos = headerSize;
            _buffer[isLong ? 3 : 2] = headCode;
        }

        public ServerPacketWriter WriteByte(byte v)     { _buffer[_pos++] = v; return this; }
        public ServerPacketWriter WriteUShort(ushort v) { _buffer[_pos++] = (byte)(v >> 8); _buffer[_pos++] = (byte)(v & 0xFF); return this; }
        public ServerPacketWriter WriteInt(int v)       { WriteUInt((uint)v); return this; }
        public ServerPacketWriter WriteUInt(uint v)
        {
            _buffer[_pos++] = (byte)(v >> 24); _buffer[_pos++] = (byte)(v >> 16);
            _buffer[_pos++] = (byte)(v >> 8);  _buffer[_pos++] = (byte)(v & 0xFF);
            return this;
        }
        public ServerPacketWriter WriteString(string s, int fixedLen)
        {
            var bytes = Encoding.ASCII.GetBytes(s ?? "");
            int writeLen = Math.Min(bytes.Length, fixedLen);
            Array.Copy(bytes, 0, _buffer, _pos, writeLen);
            _pos += fixedLen;
            return this;
        }
        public ServerPacketWriter WriteBytes(byte[] data)
        {
            Array.Copy(data, 0, _buffer, _pos, data.Length);
            _pos += data.Length;
            return this;
        }

        public byte[] Build()
        {
            bool isLong = _type == PacketType.C2 || _type == PacketType.C4;
            var result = new byte[_pos];
            Array.Copy(_buffer, result, _pos);
            if (isLong) { result[1] = (byte)(_pos >> 8); result[2] = (byte)(_pos & 0xFF); }
            else result[1] = (byte)_pos;
            return result;
        }
    }

    public class ServerPacketReader
    {
        private readonly byte[] _data;
        private int _pos;
        public byte HeadCode { get; }
        public byte SubCode  { get; }

        public ServerPacketReader(byte[] data)
        {
            _data = data;
            bool isLong = data[0] == 0xC2 || data[0] == 0xC4;
            HeadCode = isLong ? data[3] : data[2];
            _pos = isLong ? 4 : 3;
            SubCode = _pos < data.Length ? data[_pos] : (byte)0;
        }

        public byte   ReadByte()   => _data[_pos++];
        public ushort ReadUShort() { ushort v = (ushort)((_data[_pos] << 8) | _data[_pos+1]); _pos += 2; return v; }
        public int    ReadInt()    { int v = (_data[_pos]<<24)|(_data[_pos+1]<<16)|(_data[_pos+2]<<8)|_data[_pos+3]; _pos+=4; return v; }
        public uint   ReadUInt()   => (uint)ReadInt();
        public string ReadString(int len)
        {
            int end = _pos + len;
            int nullAt = _pos;
            while (nullAt < end && nullAt < _data.Length && _data[nullAt] != 0) nullAt++;
            string r = Encoding.ASCII.GetString(_data, _pos, nullAt - _pos);
            _pos = end;
            return r;
        }
        public void Skip(int n) => _pos += n;
        public bool HasMore => _pos < _data.Length;
    }
}
