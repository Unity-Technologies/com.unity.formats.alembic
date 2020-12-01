using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.TestTools;

namespace UnityEditor.Formats.Alembic.Exporter.UnitTests
{
    class CurveTests : BaseFixture
    {
        const string assetGUID = "253cca792b1714bd985e9752217590a8";
        AlembicStreamPlayer streamPlayer;
        AlembicCurves curves;
        const double eps = 1e-5;

        [SetUp]
        public new void SetUp()
        {
            var path = AssetDatabase.GUIDToAssetPath(assetGUID);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var inst = PrefabUtility.InstantiatePrefab(asset) as GameObject;
            streamPlayer = inst.GetComponent<AlembicStreamPlayer>();
            curves = inst.GetComponentInChildren<AlembicCurves>();
        }

        [Test]
        public void TestPositions()
        {
            Assert.AreEqual(5, streamPlayer.EndTime);
            Assert.AreEqual(curves.Positions.Length, curves.CurvePointCount[0]);
            {
                streamPlayer.UpdateImmediately(0);
                var expect = new Vector3(0.0042689126f, 0.010365379f, -0.00339815859f) * 100f; // points with default scale, scalled 100x
                Assert.LessOrEqual((curves.Positions[0] - expect).magnitude, eps);
            }
            {
                streamPlayer.UpdateImmediately(1);
                var expect = new Vector3(0.014939514f, 0.010365379f, -0.00339815859f) * 100f;
                Assert.LessOrEqual((curves.Positions[0] - expect).magnitude, eps);
            }
        }

        [Test]
        public void TestWidths() // Not Animated
        {
            streamPlayer.UpdateImmediately(0);
            const float expect = 0.100000001f;
            Assert.LessOrEqual(Mathf.Abs(curves.Widths[0] - expect), eps);
        }

        [Test]
        public void TestVelocities()
        {
            {
                streamPlayer.UpdateImmediately(0);
                Assert.IsTrue(curves.Velocities.All(x => x == Vector3.zero));
            }
            {
                streamPlayer.UpdateImmediately(1);
                var expect = new Vector3(0.0106706014f, -0f, -0f) * 100;
                Assert.LessOrEqual((curves.Velocities[0] - expect).magnitude, eps);
            }
        }

        [Test]
        public void TestUVs() // Not Animated
        {
            streamPlayer.UpdateImmediately(0);
            var expect = new Vector2(0.0731087029f, 0.16018419f);
            Assert.IsTrue(curves.UVs.All(x => x == expect));
        }
    }
}
