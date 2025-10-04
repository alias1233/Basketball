using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapScript : MonoBehaviour
{
    public static MapScript Singleton;

    public GameObject PlayerUsable;
    public GameObject VisualOnly;
    public GameObject Domain;
    public float duration;
    public float DomainSize;

    private void Awake()
    {
        Singleton = this;
    }

    private void ChangeMapVisibility(bool bvisible)
    {
        ChangeMapVisibilityRecursive(PlayerUsable, bvisible);
        VisualOnly.SetActive(bvisible);
    }

    private void ChangeMapVisibilityRecursive(GameObject gameObject, bool bvisible)
    {
        MeshRenderer[] children = gameObject.GetComponentsInChildren<MeshRenderer>(includeInactive: false);

        foreach (MeshRenderer i in children)
        {
            i.enabled = bvisible;
        }

        ParticleSystem[] children1 = gameObject.GetComponentsInChildren<ParticleSystem>(includeInactive: false);

        foreach (ParticleSystem i in children1)
        {
            i.gameObject.SetActive(bvisible);
        }
    }

    public IEnumerator ChangeDomain(bool bactive)
    {
        if(bactive)
        {
            Domain.SetActive(true);
            ChangeMapVisibility(false);
        }

        float elapsed = 0;

        while (elapsed < duration)
        {
            Vector3 size;

            if (bactive)
            {
                size = Vector3.Lerp(Vector3.zero, Vector3.one * DomainSize, elapsed / duration);
            }

            else
            {
                size = Vector3.Lerp(Vector3.one * DomainSize, Vector3.zero, elapsed / duration);
            }

            Domain.transform.localScale = size;

            elapsed += Time.deltaTime;

            yield return null;
        }

        if (!bactive)
        {
            Domain.SetActive(false);
            ChangeMapVisibility(true);
        }
    }
}
