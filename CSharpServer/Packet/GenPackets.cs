using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ServerCore;

public enum PacketID
{
    C_PlayerInfoReq = 1,
	S_PlayerOKTest = 2,
}

interface IPacket
{
	ushort Protocol { get; }

	void Read(ArraySegment<byte> segment);

	ArraySegment<byte> Write();
}

class C_PlayerInfoReq : IPacket
{
    public byte testByte;
	public long playerId;
	public string name;
	
	public class Skill
	{
	    public int id;
		public short level;
		public float duration;
		
		public class Attribute
		{
		    public int att;
		
		    public void Read(ReadOnlySpan<byte>s, ref ushort count)
		    {
		        // int, float, double, ...
				this.att = BitConverter.ToInt32(s.Slice(count, s.Length - count));
				count += sizeof(int);
		    }
		
		    public bool Write(Span<byte> s, ref ushort count)
		    {
		        bool success = true;
		
		        // bool, short, ushort, int, long, float, double
				success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.att);
				count += sizeof(int);
		
		        return success;
		    }
		}
		
		public List<Attribute> attributes = new List<Attribute>();
	
	    public void Read(ReadOnlySpan<byte>s, ref ushort count)
	    {
	        // int, float, double, ...
			this.id = BitConverter.ToInt32(s.Slice(count, s.Length - count));
			count += sizeof(int);
			// int, float, double, ...
			this.level = BitConverter.ToInt16(s.Slice(count, s.Length - count));
			count += sizeof(short);
			// int, float, double, ...
			this.duration = BitConverter.ToSingle(s.Slice(count, s.Length - count));
			count += sizeof(float);
			// List
			this.attributes.Clear();
			ushort attributeLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
			count += sizeof(ushort);
			
			for (int i = 0; i < attributeLen; i++)
			{
			    Attribute attribute = new Attribute();
			    attribute.Read(s, ref count);
			    attributes.Add(attribute);
			}
	    }
	
	    public bool Write(Span<byte> s, ref ushort count)
	    {
	        bool success = true;
	
	        // bool, short, ushort, int, long, float, double
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.id);
			count += sizeof(int);
			// bool, short, ushort, int, long, float, double
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.level);
			count += sizeof(short);
			// bool, short, ushort, int, long, float, double
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.duration);
			count += sizeof(float);
			// List
			success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.attributes.Count);
			count += sizeof(ushort);
			
			foreach (Attribute attribute in this.attributes)
			{
			    success &= attribute.Write(s, ref count);
			}
	
	        return success;
	    }
	}
	
	public List<Skill> skills = new List<Skill>();

    public ushort Protocol { get { return (ushort)PacketID.C_PlayerInfoReq; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        // byte
		this.testByte = (byte)segment.Array[segment.Offset + count];
		count += sizeof(byte);
		// int, float, double, ...
		this.playerId = BitConverter.ToInt64(s.Slice(count, s.Length - count));
		count += sizeof(long);
		// string
		ushort nameLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		this.name = Encoding.Unicode.GetString(s.Slice(count, nameLen));
		count += nameLen;
		// List
		this.skills.Clear();
		ushort skillLen = BitConverter.ToUInt16(s.Slice(count, s.Length - count));
		count += sizeof(ushort);
		
		for (int i = 0; i < skillLen; i++)
		{
		    Skill skill = new Skill();
		    skill.Read(s, ref count);
		    skills.Add(skill);
		}
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count , s.Length - count), (ushort)PacketID.C_PlayerInfoReq);
        count += sizeof(ushort);

        // byte
		segment.Array[segment.Offset + count] = (byte)this.testByte;
		count += sizeof(byte);
		// bool, short, ushort, int, long, float, double
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.playerId);
		count += sizeof(long);
		// string
		ushort nameLen = (ushort)Encoding.Unicode.GetBytes(this.name, 0, this.name.Length, segment.Array, segment.Offset + count + sizeof(ushort));
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), nameLen);
		count += sizeof(ushort);
		count += nameLen;
		// List
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), (ushort)this.skills.Count);
		count += sizeof(ushort);
		
		foreach (Skill skill in this.skills)
		{
		    success &= skill.Write(s, ref count);
		}

        success &= BitConverter.TryWriteBytes(s, count);
        if (success == false)
        {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}

class S_PlayerOKTest : IPacket
{
    public int TestInt;

    public ushort Protocol { get { return (ushort)PacketID.S_PlayerOKTest; } }

    public void Read(ArraySegment<byte> segment)
    {
        ushort count = 0;

        ReadOnlySpan<byte> s = new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count);
        count += sizeof(ushort);
        count += sizeof(ushort);

        // int, float, double, ...
		this.TestInt = BitConverter.ToInt32(s.Slice(count, s.Length - count));
		count += sizeof(int);
    }

    public ArraySegment<byte> Write()
    {
        ArraySegment<byte> segment = SendBufferHelper.Open(4096);
        ushort count = 0;
        bool success = true;

        Span<byte> s = new Span<byte>(segment.Array, segment.Offset, segment.Count);

        count += sizeof(ushort);
        success &= BitConverter.TryWriteBytes(s.Slice(count , s.Length - count), (ushort)PacketID.S_PlayerOKTest);
        count += sizeof(ushort);

        // bool, short, ushort, int, long, float, double
		success &= BitConverter.TryWriteBytes(s.Slice(count, s.Length - count), this.TestInt);
		count += sizeof(int);

        success &= BitConverter.TryWriteBytes(s, count);
        if (success == false)
        {
            return null;
        }

        return SendBufferHelper.Close(count);
    }
}
