using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public struct WeaponInputs : INetworkSerializable
{
    public int TimeStamp;
    public ActiveWeaponNumber ActiveWeapon;
    public bool Mouse1;
    public bool Mouse2;
    public bool F;

    public WeaponInputs(int timestamp, ActiveWeaponNumber activeweapon, bool mouse1, bool mouse2, bool f)
    {
        TimeStamp = timestamp;
        ActiveWeapon = activeweapon;
        Mouse1 = mouse1;
        Mouse2 = mouse2;
        F = f;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();

            fastBufferWriter.WriteValueSafe(TimeStamp);
            fastBufferWriter.WriteValueSafe(ActiveWeapon);
            fastBufferWriter.WriteValueSafe(Mouse1);
            fastBufferWriter.WriteValueSafe(Mouse2);
            fastBufferWriter.WriteValueSafe(F);
        }

        if (serializer.IsReader)
        {
            FastBufferReader fastBufferReader = serializer.GetFastBufferReader();

            fastBufferReader.ReadValueSafe(out int timestamp);
            fastBufferReader.ReadValueSafe(out ActiveWeaponNumber activeweapon);
            fastBufferReader.ReadValueSafe(out bool mouse1);
            fastBufferReader.ReadValueSafe(out bool mouse2);
            fastBufferReader.ReadValueSafe(out bool f);

            TimeStamp = timestamp;
            ActiveWeapon = activeweapon;
            Mouse1 = mouse1;
            Mouse2 = mouse2;
            F = f;
        }
    }
}
