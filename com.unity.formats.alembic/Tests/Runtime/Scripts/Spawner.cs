using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    bool on;

    int nFrames = 10;
    int curFrame = 0;

    // Update is called once per frame
    void Update()
    {
        curFrame++;
        if (curFrame % nFrames == 0)
        {
            if (on)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            else
            {
                var go = new GameObject(curFrame.ToString());
                go.transform.parent = transform;
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = go.transform;
            }

            on = !on;
        }
    }
}
