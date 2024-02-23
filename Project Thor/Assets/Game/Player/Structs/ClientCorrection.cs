using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public struct ClientCorrection : INetworkSerializable
{
    public int TimeStamp;

    public Vector3 Position;
    public Vector3 Velocity;
    public bool bNoMovement;
    public int LastTimeJumped;

    public bool bDashing;
    public int StartDashTime;
    public Quaternion DashingStartRotation;

    public ClientCorrection(int timestamp, Vector3 position, Vector3 velocity, bool bnomovement, int lasttimejumped, bool bdashing, int startdashtime, Quaternion dashingstartrotation)
    {
        TimeStamp = timestamp;
        Position = position;
        Velocity = velocity;
        bNoMovement = bnomovement;
        LastTimeJumped = lasttimejumped;
        bDashing = bdashing;
        StartDashTime = startdashtime;
        DashingStartRotation = dashingstartrotation;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
        {
            FastBufferWriter fastBufferWriter = serializer.GetFastBufferWriter();

            fastBufferWriter.WriteValueSafe(TimeStamp);
            fastBufferWriter.WriteValueSafe(Position);
            fastBufferWriter.WriteValueSafe(Velocity);
            fastBufferWriter.WriteValueSafe(bNoMovement);
            fastBufferWriter.WriteValueSafe(LastTimeJumped);
            fastBufferWriter.WriteValueSafe(bDashing);
            fastBufferWriter.WriteValueSafe(StartDashTime);
            fastBufferWriter.WriteValueSafe(DashingStartRotation);
        }

        if (serializer.IsReader)
        {
            FastBufferReader fastBufferReader = serializer.GetFastBufferReader();

            fastBufferReader.ReadValueSafe(out int timestamp);
            fastBufferReader.ReadValueSafe(out Vector3 position);
            fastBufferReader.ReadValueSafe(out Vector3 velocity);
            fastBufferReader.ReadValueSafe(out bool bnomovement);
            fastBufferReader.ReadValueSafe(out int lasttimejumped);
            fastBufferReader.ReadValueSafe(out bool bdashing);
            fastBufferReader.ReadValueSafe(out int startdashtime);
            fastBufferReader.ReadValueSafe(out Quaternion dashingstartrotation);

            TimeStamp = timestamp;
            Position = position;
            Velocity = velocity;
            bNoMovement = bnomovement;
            LastTimeJumped = lasttimejumped;
            bDashing = bdashing;
            StartDashTime = startdashtime;
            DashingStartRotation = dashingstartrotation;
        }
    }
}
