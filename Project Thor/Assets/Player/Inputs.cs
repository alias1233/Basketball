using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct Inputs : INetworkSerializable
{
    public int TimeStamp;
    public Quaternion Rotation;
    public bool W;
    public bool S;
    public bool A;
    public bool D;
    public bool SpaceBar;

    public Inputs(int timestamp, Quaternion rotation, bool w, bool a, bool s, bool d, bool spacebar)
    {
        TimeStamp = timestamp;
        Rotation = rotation;
        W = w;
        A = a;
        S = s;
        D = d;
        SpaceBar = spacebar;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();

            fastBufferWriter.WriteValueSafe(TimeStamp);
            fastBufferWriter.WriteValueSafe(Rotation);
            fastBufferWriter.WriteValueSafe(W);
            fastBufferWriter.WriteValueSafe(A);
            fastBufferWriter.WriteValueSafe(S);
            fastBufferWriter.WriteValueSafe(D);
            fastBufferWriter.WriteValueSafe(SpaceBar);
        }

        if (serializer.IsReader)
        {
            FastBufferReader fastBufferReader = serializer.GetFastBufferReader();

            fastBufferReader.ReadValueSafe(out int timestamp);
            fastBufferReader.ReadValueSafe(out Quaternion rotation);
            fastBufferReader.ReadValueSafe(out bool w);
            fastBufferReader.ReadValueSafe(out bool a);
            fastBufferReader.ReadValueSafe(out bool s);
            fastBufferReader.ReadValueSafe(out bool d);
            fastBufferReader.ReadValueSafe(out bool spacebar);

            TimeStamp = timestamp;
            Rotation = rotation;
            W = w;
            A = a;
            S = s;
            D = d;
            SpaceBar = spacebar;
        }
    }
}
