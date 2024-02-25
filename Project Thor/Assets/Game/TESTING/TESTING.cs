using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTING : MonoBehaviour
{
    public int Times = 1000000;

    public bool CreateNew;

    Inputs CurrentInput;
    public int CurrentTimeStamp;

    // Update is called once per frame
    void Update()
    {
        CurrentTimeStamp++;

        if (CreateNew)
        {
            for (int i = 0; i < Times; i++)
            {
                CurrentInput = new Inputs(CurrentTimeStamp, transform.rotation, Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.S), Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.CapsLock), Input.GetKey(KeyCode.LeftShift), Input.GetKey(KeyCode.Mouse0));
            }

            return;
        }

        for (int i = 0; i < Times; i++)
        {
            CreateInputs(ref CurrentInput);
        }
    }

    private void CreateInputs(ref Inputs input)
    {
        input.TimeStamp = CurrentTimeStamp;
        input.Rotation = transform.rotation;
        input.W = Input.GetKey(KeyCode.W);
        input.A = Input.GetKey(KeyCode.A);
        input.S = Input.GetKey(KeyCode.S);
        input.D = Input.GetKey(KeyCode.D);
        input.SpaceBar = Input.GetKey(KeyCode.Space);
        input.Shift = Input.GetKey(KeyCode.LeftShift);
        input.CTRL = Input.GetKey(KeyCode.CapsLock);
    }
}
