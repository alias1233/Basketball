using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public struct WeaponInputs : INetworkSerializable
{
    public int TimeStamp;
    public bool Mouse1;
    public bool Mouse2;

    public WeaponInputs(int timestamp, bool mouse1, bool mouse2)
    {
        TimeStamp = timestamp;
        Mouse1 = mouse1;
        Mouse2 = mouse2;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();

            fastBufferWriter.WriteValueSafe(TimeStamp);
            fastBufferWriter.WriteValueSafe(Mouse1);
            fastBufferWriter.WriteValueSafe(Mouse2);
        }

        if (serializer.IsReader)
        {
            FastBufferReader fastBufferReader = serializer.GetFastBufferReader();

            fastBufferReader.ReadValueSafe(out int timestamp);
            fastBufferReader.ReadValueSafe(out bool mouse1);
            fastBufferReader.ReadValueSafe(out bool mouse2);

            TimeStamp = timestamp;
            Mouse1 = mouse1;
            Mouse2 = mouse2;
        }
    }
}
