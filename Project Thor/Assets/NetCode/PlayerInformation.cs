using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerInformation : INetworkSerializable, System.IEquatable<PlayerInformation>
{
    public ulong Id;
    public ushort Team;
    //public FixedString32Bytes Name;

    /*
    public PlayerInformation(ulong id, ushort team, FixedString32Bytes name)
    {
        Id = id;
        Team = team;
        Name = name;
    }
    */

    public PlayerInformation(ulong id, ushort team)
    {
        Id = id;
        Team = team;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();

            fastBufferWriter.WriteValueSafe(Id);
            fastBufferWriter.WriteValueSafe(Team);
            //fastBufferWriter.WriteValueSafe(Name);
        }

        if (serializer.IsReader)
        {
            FastBufferReader fastBufferReader = serializer.GetFastBufferReader();

            fastBufferReader.ReadValueSafe(out ulong id);
            fastBufferReader.ReadValueSafe(out ushort team);
            //fastBufferReader.ReadValueSafe(out FixedString32Bytes Name);

            Id = id;
            Team = team;
        }
    }

    public bool Equals(PlayerInformation other)
    {
        return other.Id == Id;
    }
}
