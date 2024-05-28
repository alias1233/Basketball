using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GrapplingHookTick : BaseProjectileTick
{
    public GrapplingHook Grapple;

    public int GrappleCurveSize = 10;

    public AnimationCurve GrappleCurvex;
    public AnimationCurve GrappleCurvey;

    public float MaxDistance = 25f;

    public override void FixedUpdate()
    {
        if(Grapple.bHit)
        {
            if(Grapple.OwningPlayerMovement)
            {
                if(Grapple.grapple.positionCount != 2)
                {
                    Grapple.grapple.positionCount = 2;
                }

                Grapple.grapple.SetPosition(0, Grapple.OwningPlayerMovement.GetGrappleShootStartLocation() + 
                    new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f), 0));
                Grapple.grapple.SetPosition(1, SelfTransform.position);
            }

            return;
        }

        base.FixedUpdate();

        if (!Grapple.OwningPlayerMovement)
        {
            return;
        }

        if (Grapple.grapple.positionCount != GrappleCurveSize)
        {
            Grapple.grapple.positionCount = GrappleCurveSize;
        }

        Vector3[] GrapplePositions = new Vector3[GrappleCurveSize];

        Vector3 PlayerLocation = Grapple.OwningPlayerMovement.GetGrappleShootStartLocation();
        Vector3 Right = Vector3.Cross((SelfTransform.position - PlayerLocation).normalized, Vector3.up);
        float Dampen = ((float)TimeStamp - StartTime) / Lifetime;

        GrapplePositions[0] = PlayerLocation;
        GrapplePositions[GrappleCurveSize - 1] = SelfTransform.position;

        for (int i = 1; i < GrappleCurveSize - 1; i++)
        {
            float alpha = (float)i / GrappleCurveSize;

            GrapplePositions[i] = Vector3.Lerp(PlayerLocation, SelfTransform.position, alpha) + 
                (Right * GrappleCurvex.Evaluate(alpha) + Vector3.down * GrappleCurvey.Evaluate(alpha)) * Dampen;
        }

        Grapple.grapple.SetPositions(GrapplePositions);
    }
}
