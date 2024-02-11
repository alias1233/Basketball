using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct ClientCorrection : INetworkSerializable
{
    public int TimeStamp;

    public Vector3 Position;
    public Vector3 Velocity;
    public int LastTimeJumped;

    public ClientCorrection(int timestamp, Vector3 position, Vector3 velocity, int lasttimejumped)
    {
        TimeStamp = timestamp;
        Position = position;
        Velocity = velocity;
        LastTimeJumped = lasttimejumped;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();

            fastBufferWriter.WriteValueSafe(TimeStamp);
            fastBufferWriter.WriteValueSafe(Position);
            fastBufferWriter.WriteValueSafe(Velocity);
            fastBufferWriter.WriteValueSafe(LastTimeJumped);
        }

        if (serializer.IsReader)
        {
            FastBufferReader fastBufferReader = serializer.GetFastBufferReader();

            fastBufferReader.ReadValueSafe(out int timestamp);
            fastBufferReader.ReadValueSafe(out Vector3 position);
            fastBufferReader.ReadValueSafe(out Vector3 velocity);
            fastBufferReader.ReadValueSafe(out int lasttimejumped);

            TimeStamp = timestamp;
            Position = position;
            Velocity = velocity;
            LastTimeJumped = lasttimejumped;
        }
    }
}
