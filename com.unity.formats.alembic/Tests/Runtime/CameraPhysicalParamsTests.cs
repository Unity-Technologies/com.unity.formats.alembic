using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Util;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class CameraPhysicalParamsTests : BaseFixture
    {
        struct CamParams : IEquatable<CamParams>
        {
            public float focalLength;
            public Vector2 filmBack;
            public Vector2 nearFar;

            public void ToCamera(Camera cam)
            {
                cam.usePhysicalProperties = true;
                cam.focalLength = focalLength;
                cam.sensorSize = filmBack;
                cam.nearClipPlane = nearFar[0];
                cam.farClipPlane = nearFar[1];
            }
            
            public void FromCamera(Camera cam)
            {
                focalLength = cam.focalLength;
                filmBack = cam.sensorSize;
                nearFar = new Vector2(cam.nearClipPlane, cam.farClipPlane);
            }

            public bool Equals(CamParams other)
            {
                return NearlyEqual(focalLength, other.focalLength) &&
                       NearlyEqual(filmBack.x, other.filmBack.x) &&
                       NearlyEqual(filmBack.y, other.filmBack.y) &&
                       NearlyEqual(nearFar.x, other.nearFar.x) &&
                       NearlyEqual(nearFar.y, other.nearFar.y);
            }

            public override bool Equals(object obj)
            {
                return obj is CamParams other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = focalLength.GetHashCode();
                    hashCode = (hashCode * 397) ^ filmBack.GetHashCode();
                    hashCode = (hashCode * 397) ^ nearFar.GetHashCode();
                    return hashCode;
                }
            }

            static bool NearlyEqual(float f1, float f2, float eps = 1e-5f)
            {
                return Mathf.Abs(f1 -f2) < eps;
            }
            
        }

        CamParams camParams = new CamParams {focalLength = 300, filmBack = new Vector2(200,300), nearFar = new Vector2(0.1f,500)};
        

        [SetUp]
        public new void SetUp()
        {

            var cam = Object.FindObjectOfType<Camera>();
           camParams.ToCamera(cam);
        }


        [UnityTest]
        public IEnumerator TestPhysicalCamParams()
        {
            deleteFileList.Add(exporter.recorder.settings.OutputPath);
            exporter.OneShot ();
            yield return null;
            
            AssetDatabase.Refresh();
            Assert.That(File.Exists(exporter.recorder.settings.OutputPath));
            var abc = AssetDatabase.LoadMainAssetAtPath(exporter.recorder.settings.OutputPath) as GameObject;
            var importedParams = new CamParams();
            importedParams.FromCamera(abc.GetComponentInChildren<Camera>());
            Assert.IsTrue(camParams.Equals(importedParams));
        }
    }
}
