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
    public bool Shift;
    public bool CTRL;
    public bool E;
    public bool R;

    public Inputs(int timestamp, Quaternion rotation, bool w, bool a, bool s, bool d, bool spacebar, bool shift, bool ctrl, bool e, bool r)
    {
        TimeStamp = timestamp;
        Rotation = rotation;
        W = w;
        A = a;
        S = s;
        D = d;
        SpaceBar = spacebar;
        Shift = shift;
        CTRL = ctrl;
        E = e;
        R = r;
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
            fastBufferWriter.WriteValueSafe(Shift);
            fastBufferWriter.WriteValueSafe(CTRL);
            fastBufferWriter.WriteValueSafe(E);
            fastBufferWriter.WriteValueSafe(R);
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
            fastBufferReader.ReadValueSafe(out bool shift);
            fastBufferReader.ReadValueSafe(out bool ctrl);
            fastBufferReader.ReadValueSafe(out bool e);
            fastBufferReader.ReadValueSafe(out bool r);

            TimeStamp = timestamp;
            Rotation = rotation;
            W = w;
            A = a;
            S = s;
            D = d;
            SpaceBar = spacebar;
            Shift = shift;
            CTRL = ctrl;
            E = e;
            R = r;
        }
    }
}
