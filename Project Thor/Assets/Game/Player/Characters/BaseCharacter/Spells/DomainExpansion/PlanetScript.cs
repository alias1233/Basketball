using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetScript : MonoBehaviour
{
    public float Speed;
    public float ColorInterval;

    Transform SelfTransform;
    Vector3 Right;
    RedHollowControl color;
    float lastcolorswitch;

    // Start is called before the first frame update
    void Start()
    {
        SelfTransform = transform;
        color = GetComponent<RedHollowControl>();
    }

    // Update is called once per frame
    void Update()
    {
        SelfTransform.RotateAround(Vector3.zero, Vector3.up, Speed * Time.deltaTime);

        if(Time.time - lastcolorswitch > 0.25f)
        {
            lastcolorswitch = Time.time;

            color.updateHue(ColorInterval);
        }
    }
}
