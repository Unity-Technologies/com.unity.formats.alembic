using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public abstract class AlembicCustomComponentCapturer : MonoBehaviour
{
    public abstract void SetParent(aeAPI.aeObject parent);
    public abstract void Capture();
}
