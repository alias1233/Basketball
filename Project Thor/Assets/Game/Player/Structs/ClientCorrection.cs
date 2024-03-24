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
    public bool bWasSpace;
    public bool bTryJump;

    public bool bDashing;
    public int StartDashTime;
    public Vector3 DashingStartRotation;

    public bool bWasCTRL;
    public bool bTrySlideGroundPound;
    public int TimeStartSlideGroundPound;

    public bool bSliding;
    public Vector3 SlideDirection;
    public int LastTimeSlide;

    public bool bGroundPound;

    public bool bGrapple;
    public int GrappleStartTime;
    public Vector3 GrappleLocation;



    public ClientCorrection(int timestamp, Vector3 position, Vector3 velocity,
        bool bnomovement,
        int lasttimejumped,
        bool bwasspace,
        bool btryjump,
        bool bdashing,
        int startdashtime,
        Vector3 dashingstartrotation,
        bool bsliding,
        Vector3 slidedirection,
        int lasttimeslide,
        bool bwasctrl,
        bool btryslidegroundpound,
        int timestartslidegroundpound,
        bool bgroundpound,
        bool bgrapple,
        int grapplestarttime,
        Vector3 grapplelocation

        )
    {
        TimeStamp = timestamp;
        Position = position;
        Velocity = velocity;
        bNoMovement = bnomovement;
        LastTimeJumped = lasttimejumped;
        bWasSpace = bwasspace;
        bTryJump = btryjump;
        bDashing = bdashing;
        StartDashTime = startdashtime;
        DashingStartRotation = dashingstartrotation;
        bSliding = bsliding;
        SlideDirection = slidedirection;
        LastTimeSlide = lasttimeslide;
        bWasCTRL = bwasctrl;
        bTrySlideGroundPound = btryslidegroundpound;
        TimeStartSlideGroundPound = timestartslidegroundpound;
        bGroundPound = bgroundpound;
        bGrapple = bgrapple;
        GrappleStartTime = grapplestarttime;
        GrappleLocation = grapplelocation;

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
            fastBufferWriter.WriteValueSafe(bWasSpace);
            fastBufferWriter.WriteValueSafe(bTryJump);
            fastBufferWriter.WriteValueSafe(bDashing);
            fastBufferWriter.WriteValueSafe(StartDashTime);
            fastBufferWriter.WriteValueSafe(DashingStartRotation);
            fastBufferWriter.WriteValueSafe(bSliding);
            fastBufferWriter.WriteValueSafe(SlideDirection);
            fastBufferWriter.WriteValueSafe(LastTimeSlide);
            fastBufferWriter.WriteValueSafe(bWasCTRL);
            fastBufferWriter.WriteValueSafe(bTrySlideGroundPound);
            fastBufferWriter.WriteValueSafe(TimeStartSlideGroundPound);
            fastBufferWriter.WriteValueSafe(bGroundPound);
            fastBufferWriter.WriteValueSafe(bGrapple);
            fastBufferWriter.WriteValueSafe(GrappleStartTime);
            fastBufferWriter.WriteValueSafe(GrappleLocation);
        }

        if (serializer.IsReader)
        {
            FastBufferReader fastBufferReader = serializer.GetFastBufferReader();

            fastBufferReader.ReadValueSafe(out int timestamp);
            fastBufferReader.ReadValueSafe(out Vector3 position);
            fastBufferReader.ReadValueSafe(out Vector3 velocity);
            fastBufferReader.ReadValueSafe(out bool bnomovement);
            fastBufferReader.ReadValueSafe(out int lasttimejumped);
            fastBufferReader.ReadValueSafe(out bool bwasspace);
            fastBufferReader.ReadValueSafe(out bool btryjump);
            fastBufferReader.ReadValueSafe(out bool bdashing);
            fastBufferReader.ReadValueSafe(out int startdashtime);
            fastBufferReader.ReadValueSafe(out Vector3 dashingstartrotation);
            fastBufferReader.ReadValueSafe(out bool bsliding);
            fastBufferReader.ReadValueSafe(out Vector3 slidedirection);
            fastBufferReader.ReadValueSafe(out int lasttimeslide);
            fastBufferReader.ReadValueSafe(out bool bwasctrl);
            fastBufferReader.ReadValueSafe(out bool btryslidegroundpound);
            fastBufferReader.ReadValueSafe(out int timestartslidegroundpound);
            fastBufferReader.ReadValueSafe(out bool bgroundpound);
            fastBufferReader.ReadValueSafe(out bool bgrapple);
            fastBufferReader.ReadValueSafe(out int grapplestarttime);
            fastBufferReader.ReadValueSafe(out Vector3 grapplelocation);

            TimeStamp = timestamp;
            Position = position;
            Velocity = velocity;
            bNoMovement = bnomovement;
            LastTimeJumped = lasttimejumped;
            bWasSpace = bwasspace;
            bTryJump = btryjump;
            bDashing = bdashing;
            StartDashTime = startdashtime;
            DashingStartRotation = dashingstartrotation;
            bSliding = bsliding;
            SlideDirection = slidedirection;
            LastTimeSlide = lasttimeslide;
            bWasCTRL = bwasctrl;
            bTrySlideGroundPound = btryslidegroundpound;
            TimeStartSlideGroundPound = timestartslidegroundpound;
            bGroundPound = bgroundpound;
            bGrapple = bgrapple;
            GrappleStartTime = grapplestarttime;
            GrappleLocation = grapplelocation;
        }
    }
}
