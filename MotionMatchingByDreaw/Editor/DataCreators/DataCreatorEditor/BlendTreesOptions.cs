using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MotionMatching.Tools
{
    public static class BlendTreesOptions
    {
        public static void DrawTreesList(DataCreator creator, EditorWindow editor)
        {
            GUILayoutElements.DrawHeader("Blend Trees", GUIResources.GetDarkHeaderStyle_MD());

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                creator.blendTrees.Add(new BlendTreeInfo("New info"));
            }
            //if (GUILayout.Button("Clear"))
            //{
            //    creator.blendTrees.Clear();
            //}
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Find", GUILayout.Width(50));
            creator.findingBlendTree = GUILayout.TextField(creator.findingBlendTree);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            for (int i = 0; i < creator.blendTrees.Count; i++)
            {
                if (creator.findingBlendTree != "" && !creator.blendTrees[i].name.ToLower().Contains(creator.findingBlendTree.ToLower()))
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Space(5);
                GUILayout.Label(
                    creator.blendTrees[i].name,
                     i == creator.selectedBlendTree ? GUIResources.GetDarkHeaderStyle_SM() : GUIResources.GetLightHeaderStyle_SM()
                    );


                Event e = Event.current;
                Rect r = GUILayoutUtility.GetLastRect();

                if (r.Contains(e.mousePosition) && e.type == EventType.MouseDown && e.button == 0)
                {
                    if (creator.selectedBlendTree == i)
                    {
                        creator.selectedBlendTree = -1;
                    }
                    else
                    {
                        creator.selectedBlendTree = i;
                    }
                    e.Use();
                    editor.Repaint();
                }

                if (GUILayout.Button("Copy", GUILayout.Width(40)))
                {
                    creator.blendTrees.Add(new BlendTreeInfo(creator.blendTrees[i].name + "_NEW"));
                    for (int j = 0; j < creator.blendTrees[i].clips.Count; j++)
                    {
                        creator.blendTrees[creator.blendTrees.Count - 1].AddClip(creator.blendTrees[i].clips[j]);
                    }
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    creator.blendTrees.RemoveAt(i);
                    i--;
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            GUILayout.Space(5);
        }

        public static void DrawTreesElements(DataCreator creator, EditorWindow editor)
        {
            if (creator.selectedBlendTree == -1 || creator.blendTrees.Count == 0 || creator.selectedBlendTree >= creator.blendTrees.Count)
            {
                creator.selectedBlendTree = -1;
                GUILayout.Label("No blend tree item is selected");
                return;
            }

            BlendTreeInfo blendTree = creator.blendTrees[creator.selectedBlendTree];

            GUILayoutElements.DrawHeader(blendTree.name, GUIResources.GetLightHeaderStyle_MD());
            blendTree.name = EditorGUILayout.TextField(
                new GUIContent("Blend Tree name"),
                blendTree.name
                );

            blendTree.findInYourself = EditorGUILayout.Toggle(new GUIContent("Find in yourself"), blendTree.findInYourself);
            blendTree.blendToYourself = EditorGUILayout.Toggle(new GUIContent("Blend to yourself"), blendTree.blendToYourself);

            blendTree.useSpaces = EditorGUILayout.Toggle(new GUIContent("Use spaces"), blendTree.useSpaces);
            if (blendTree.useSpaces)
            {
                if (blendTree.clips.Count == 2)
                {
                    blendTree.spaces = EditorGUILayout.IntField(new GUIContent("Spaces"), blendTree.spaces);
                }
                else
                {
                    GUILayout.Label("You can use \"Spaces\" with only 2 animations!");
                }
            }

            DrawElement(blendTree, creator.selectedBlendTree);
        }

        private static void DrawElement(BlendTreeInfo element, int elementIndex)
        {
            GUILayout.Space(5);

            GUILayoutElements.DrawHeader(
                "Animation clips",
                GUIResources.GetLightHeaderStyle_MD()
                );

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Clip", GUILayout.Width(100)))
            {
                element.AddClip(null);
            }
            if (GUILayout.Button("Clear clips", GUILayout.Width(100)))
            {
                element.ClearClips();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            for (int i = 0; i < element.clips.Count; i++)
            {
                GUILayout.BeginHorizontal();
                element.clips[i] = (AnimationClip)EditorGUILayout.ObjectField(element.clips[i], typeof(AnimationClip), true);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    element.RemoveClip(i);
                    i--;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);

            GUILayoutElements.DrawHeader(
                "Clips weights",
                GUIResources.GetLightHeaderStyle_MD()
                );
            GUILayout.Space(5);
            for (int weightIndex = 0; weightIndex < element.clipsWeights.Count; weightIndex++)
            {
                if (element.clips[weightIndex] != null)
                {
                    GUILayout.Label(new GUIContent(element.clips[weightIndex].name + " weight"));
                    element.clipsWeights[weightIndex] = EditorGUILayout.Slider(
                        element.clipsWeights[weightIndex],
                        0,
                        1f
                        );
                }
            }
            GUILayout.Space(10);
        }

    }
}
