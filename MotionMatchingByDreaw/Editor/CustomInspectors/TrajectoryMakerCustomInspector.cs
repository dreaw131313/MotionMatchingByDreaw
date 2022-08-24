using MotionMatching.Gameplay;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MotionMatching.Tools
{
    //[CustomEditor(typeof(TrajectoryMaker))]
    public class TrajectoryMakerCustomInspector : Editor
    {
        TrajectoryMaker component;


        private void OnEnable()
        {
            component = this.target as TrajectoryMaker;
        }
        public override void OnInspectorGUI()
        {
            if (Application.isPlaying)
            {
                serializedObject.Update();
            }

            float margin = 5f;
            GUILayout.BeginHorizontal();
            GUILayout.Space(margin);
            DrawEditableProperties();
            //GUILayout.Space(margin);
            GUILayout.EndHorizontal();
            //if (!component.UseAttachedMotionMatchingComponent)
            //{
            //    GUILayout.Space(10);
            //    DrawTrajectoryTimes();
            //}

            if (Application.isPlaying)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawEditableProperties()
        {
            //component.Bias = EditorGUILayout.Slider("Bias", component.Bias, 0f, 20f);

            //component.Stiffness = EditorGUILayout.Slider("Stiffness", component.Stiffness, 0f, 1f);

            //component.MaxSpeed = Mathf.Clamp(
            //    EditorGUILayout.FloatField("Max Speed", component.MaxSpeed),
            //    0,
            //    float.MaxValue
            //    );


            //component.Acceleration = Mathf.Clamp(
            //    EditorGUILayout.FloatField("Acceleration", component.Acceleration),
            //    0.01f,
            //    float.MaxValue
            //    );

            //component.Deceleration = Mathf.Clamp(
            //    EditorGUILayout.FloatField("Deceleration", component.Deceleration),
            //    0.01f,
            //    float.MaxValue
            //    );

            //component.PastTrajectoryType = (PastTrajectoryType)EditorGUILayout.EnumPopup("Past Trajectory Type", component.PastTrajectoryType);

            //component.TrajectoryRecordUpdateTime = EditorGUILayout.FloatField("Trajectory Record Update Time", component.TrajectoryRecordUpdateTime);

            //component.Strafe = EditorGUILayout.Toggle("Strafe", component.Strafe);

            //component.OrientationFromCollisionTrajectory = EditorGUILayout.Toggle("Orientation From Collision Trajectory", component.OrientationFromCollisionTrajectory);

            //component.CapsuleHeight = Mathf.Clamp(
            //    EditorGUILayout.FloatField("Capsule Height", component.CapsuleHeight),
            //    0.01f,
            //    float.MaxValue
            //    );

            //component.CapsuleRadius = Mathf.Clamp(
            //    EditorGUILayout.FloatField("Capsule Radius", component.CapsuleRadius),
            //    0.01f,
            //    float.MaxValue
            //    );

            //LayerMask tempMask = EditorGUILayout.MaskField("Collision Mask", InternalEditorUtility.LayerMaskToConcatenatedLayersMask(component.CollisionMask), InternalEditorUtility.layers);

            //component.CollisionMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

            //GUILayout.Space(10);

            //component.drawDebug = EditorGUILayout.Toggle("Draw debug", component.drawDebug);
            //if (component.drawDebug)
            //{
            //    component.pointRadius = EditorGUILayout.Slider("Trajectory points radius", component.pointRadius, 0.001f, 1f);
            //}

            //GUILayout.Space(5);
            ////component.UseAttachedMotionMatchingComponent = EditorGUILayout.Toggle("Use MotionMatching Component", component.UseAttachedMotionMatchingComponent);

            //EditorGUILayout.EndVertical();
        }

        private void DrawTrajectoryTimes()
        {
            if (!Application.isPlaying)
            {
                //DrawTrajectoryTimes(ref component.trajectoryTimes);
            }
            
        }

        private static void DrawTrajectoryTimes(ref List<float> trajectoryTimes)
        {
            if (trajectoryTimes == null)
            {
                trajectoryTimes = new List<float>();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Trajectory Time"))
            {
                if (trajectoryTimes.Count == 0)
                {
                    trajectoryTimes.Add(0);
                }
                else
                {
                    trajectoryTimes.Add(trajectoryTimes[trajectoryTimes.Count - 1] + 0.33f);
                }
            }
            if (GUILayout.Button("Sort Trajectory"))
            {
                trajectoryTimes.Sort();
            }
            GUILayout.EndHorizontal();
            for (int i = 0; i < trajectoryTimes.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label(
                    new GUIContent(string.Format("Time {0}", i + 1)),
                    GUILayout.Width(75)
                    );
                trajectoryTimes[i] = EditorGUILayout.FloatField(
                    trajectoryTimes[i]
                    );

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    trajectoryTimes.RemoveAt(i);
                    i--;
                }
                GUILayout.Space(10);
                GUILayout.EndHorizontal();
            }
        }
    }
}