using System;
using System.Text;

namespace MuOnline.Network.Packets
{
    /// <summary>
    /// Tipos de encabezado del protocolo MU Online:
    /// C1 / C2 = sin cifrar   |   C3 / C4 = cifrado XOR
    /// C1 / C3 = tamaño 1 byte (max 255)
    /// C2 / C4 = tamaño 2 bytes (max 65535)
    /// </summary>
    public enum PacketType : byte
    {
        C1 = 0xC1,
        C2 = 0xC2,
        C3 = 0xC3,
        C4 = 0xC4
    }

    /// <summary>
    /// Wrapper de lectura sobre un paquete recibido.
    /// </summary>
    public class PacketReader
    {
        private readonly byte[] _data;
        private int _pos;

        public int Length => _data.Length;
        public PacketType Type => (PacketType)_data[0];
        public byte HeadCode => IsLongPacket ? _data[3] : _data[2];
        public byte SubCode => IsLongPacket ? _data[4] : (_data.Length > 3 ? _data[3] : (byte)0);
        public bool IsLongPacket => Type == PacketType.C2 || Type == PacketType.C4;

        public PacketReader(byte[] data)
        {
            _data = data;
            // HeadCode está en _data[2] (C1) ó _data[3] (C2).
            // SubCode está en _data[3] (C1) ó _data[4] (C2).
            // Avanzamos el cursor PASADO el sub-code para que los handlers
            // lean directamente los datos de payload.
            _pos = IsLongPacket ? 5 : 4;
        }

        public byte ReadByte() => _data[_pos++];
        public sbyte ReadSByte() => (sbyte)_data[_pos++];

        public ushort ReadUShort()
        {
            ushort val = (ushort)((_data[_pos] << 8) | _data[_pos + 1]);
            _pos += 2;
            return val;
        }

        public short ReadShort()
        {
            short val = (short)((_data[_pos] << 8) | _data[_pos + 1]);
            _pos += 2;
            return val;
        }

        public uint ReadUInt()
        {
            uint val = ((uint)_data[_pos] << 24) | ((uint)_data[_pos + 1] << 16) |
                       ((uint)_data[_pos + 2] << 8) | _data[_pos + 3];
            _pos += 4;
            return val;
        }

        public int ReadInt()
        {
            int val = (_data[_pos] << 24) | (_data[_pos + 1] << 16) |
                      (_data[_pos + 2] << 8) | _data[_pos + 3];
            _pos += 4;
            return val;
        }

        public string ReadString(int maxLength)
        {
            int end = _pos + maxLength;
            int nullAt = _pos;
            while (nullAt < end && nullAt < _data.Length && _data[nullAt] != 0) nullAt++;
            string result = Encoding.ASCII.GetString(_data, _pos, nullAt - _pos);
            _pos = end;
            return result;
        }

        public byte[] ReadBytes(int count)
        {
            var result = new byte[count];
            Array.Copy(_data, _pos, result, 0, count);
            _pos += count;
            return result;
        }

        public void Skip(int count) => _pos += count;
        public bool HasMore => _pos < _data.Length;
    }

    /// <summary>
    /// Builder para construir paquetes a enviar al servidor.
    /// </summary>
    public class PacketWriter
    {
        private readonly byte[] _buffer;
        private int _pos;
        private readonly PacketType _type;

        public PacketWriter(PacketType type, byte headCode, int extraCapacity = 64)
        {
            _type = type;
            bool isLong = type == PacketType.C2 || type == PacketType.C4;
            int headerSize = isLong ? 4 : 3;
            _buffer = new byte[headerSize + extraCapacity];
            _buffer[0] = (byte)type;
            _pos = headerSize;

            if (isLong)
                _buffer[3] = headCode;
            else
                _buffer[2] = headCode;
        }

        public PacketWriter WriteByte(byte val) { EnsureCapacity(1); _buffer[_pos++] = val; return this; }
        public PacketWriter WriteSByte(sbyte val) { return WriteByte((byte)val); }

        public PacketWriter WriteUShort(ushort val)
        {
            EnsureCapacity(2);
            _buffer[_pos++] = (byte)(val >> 8);
            _buffer[_pos++] = (byte)(val & 0xFF);
            return this;
        }

        public PacketWriter WriteShort(short val) => WriteUShort((ushort)val);

        public PacketWriter WriteUInt(uint val)
        {
            EnsureCapacity(4);
            _buffer[_pos++] = (byte)(val >> 24);
            _buffer[_pos++] = (byte)(val >> 16);
            _buffer[_pos++] = (byte)(val >> 8);
            _buffer[_pos++] = (byte)(val & 0xFF);
            return this;
        }

        public PacketWriter WriteInt(int val) => WriteUInt((uint)val);

        public PacketWriter WriteString(string val, int fixedLength)
        {
            EnsureCapacity(fixedLength);
            var bytes = Encoding.ASCII.GetBytes(val ?? "");
            int writeLen = Math.Min(bytes.Length, fixedLength);
            Array.Copy(bytes, 0, _buffer, _pos, writeLen);
            _pos += fixedLength; // siempre avanza el tamaño fijo (rellena con 0)
            return this;
        }

        public PacketWriter WriteBytes(byte[] data)
        {
            EnsureCapacity(data.Length);
            Array.Copy(data, 0, _buffer, _pos, data.Length);
            _pos += data.Length;
            return this;
        }

        public byte[] Build()
        {
            bool isLong = _type == PacketType.C2 || _type == PacketType.C4;
            var result = new byte[_pos];
            Array.Copy(_buffer, result, _pos);

            if (isLong)
            {
                result[1] = (byte)(_pos >> 8);
                result[2] = (byte)(_pos & 0xFF);
            }
            else
            {
                result[1] = (byte)_pos;
            }

            return result;
        }

        private void EnsureCapacity(int needed)
        {
            // buffer fijo, validar que no se desborde
            if (_pos + needed > _buffer.Length)
                throw new InvalidOperationException($"PacketWriter: capacidad insuficiente ({_buffer.Length}). Necesita {_pos + needed}.");
        }
    }
}
