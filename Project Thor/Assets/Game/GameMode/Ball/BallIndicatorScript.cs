using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallIndicatorScript : MonoBehaviour
{
    public GameObject IndicatorObject;
    private Transform IndicatorTransform;

    public float CullIndicatorDistance;
    public float IndicatorDistanceFactor;
    public float IndicatorDisplacementFactor;

    private Transform OwningPlayer;

    private void Awake()
    {
        IndicatorTransform = IndicatorObject.transform;
    }

    private void FixedUpdate()
    {
        float Dist = Vector3.Distance(IndicatorTransform.position, OwningPlayer.position);

        if (Dist < CullIndicatorDistance)
        {
            if(IndicatorObject.activeSelf)
            {
                IndicatorObject.SetActive(false);
            }

            return;
        }

        if (!IndicatorObject.activeSelf)
        {
            IndicatorObject.SetActive(true);
        }

        IndicatorTransform.localScale = Vector3.one * Dist * IndicatorDistanceFactor;
        IndicatorTransform.LookAt(OwningPlayer.position);
        IndicatorTransform.localPosition = Dist * Vector3.up * IndicatorDisplacementFactor + Vector3.up;
    }

    public void SetOwningPlayer(Transform player)
    {
        OwningPlayer = player;
    }
}
