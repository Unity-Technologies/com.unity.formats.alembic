using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public abstract class AlembicCustomComponentCapturer : MonoBehaviour
{
    public abstract void CreateAbcObject(AbcAPI.aeObject parent);
    public abstract void Capture();
}
