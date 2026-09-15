using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cerrado.Environment;
using Object = UnityEngine.Object;

namespace Cerrado.Editor
{
    [CustomEditor(typeof(SceneAnnotation))]
    public class SceneAnnotationEditor : UnityEditor.Editor
    {
        private static readonly string UXMLPath = "SceneAnnotation";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var visualTree = Resources.Load<VisualTreeAsset>(UXMLPath);
            if (visualTree == null)
            {
                var defaultInspector = new IMGUIContainer(() => DrawDefaultInspector());
                root.Add(defaultInspector);
                return root;
            }

            VisualElement inspectorUI = visualTree.CloneTree();
            root.Add(inspectorUI);

            var cameraBtn = root.Q<Button>("CameraBtn");
            if (cameraBtn != null)
                cameraBtn.clicked += () => { AlignCamera(((SceneAnnotation)target).transform); };

            var nextBtn = root.Q<Button>("NextBtn");
            if (nextBtn != null)
                nextBtn.clicked += () => { GoToAnnotation(1); };

            var backBtn = root.Q<Button>("BackBtn");
            if (backBtn != null)
                backBtn.clicked += () => { GoToAnnotation(-1); };

            var sceneAnnotation = (SceneAnnotation)target;
            if (sceneAnnotation == null || sceneAnnotation.textAsset == null) return root;

            var displayElement = root.Q<VisualElement>("Spans");
            if (displayElement != null)
            {
                foreach (var eachParagraphText in sceneAnnotation.textAsset.text.Split('\n'))
                {
                    var paragraph = new VisualElement();
                    paragraph.AddToClassList("paragraph-container");

                    foreach (var word in eachParagraphText.Split(' '))
                    {
                        var displayText = word;
                        var linkText = "";

                        if (word.StartsWith("[") && word.Contains("](") && word.EndsWith(")"))
                        {
                            var paren = word.IndexOf("(");
                            displayText = word.Substring(1, paren - 2).Replace("_", " ");
                            displayText = "<b><u>" + displayText + "</u></b>";
                            linkText = word.Substring(paren + 1, word.Length - paren - 2);
                        }
                        else if (word.StartsWith("*") && word.EndsWith("*") && word.Length > 2)
                        {
                            displayText = $"<b>{word.Trim('*')}</b>";
                        }
                        else if (word.StartsWith("_") && word.EndsWith("_") && word.Length > 2)
                        {
                            displayText = $"<i>{word.Trim('_')}</i>";
                        }

                        var displaySpan = new Label(displayText);
                        displaySpan.AddToClassList("display-text");
                        if (!string.IsNullOrEmpty(linkText))
                        {
                            displaySpan.RegisterCallback<MouseDownEvent>(evt => OpenURL(linkText, sceneAnnotation.gameObject));
                            displaySpan.AddToClassList("link");
                            displaySpan.tooltip = $"Acessar {linkText}";
                        }

                        paragraph.Add(displaySpan);
                    }

                    displayElement.Add(paragraph);
                }
            }

            return root;
        }

        private static void OpenURL(string link, Object target)
        {
            if (link.StartsWith("http"))
            {
                Application.OpenURL(link);
            }
            else
            {
                var linked = AssetDatabase.LoadAssetAtPath<Object>(link);
                if (linked != null)
                {
                    EditorGUIUtility.PingObject(linked);
                    AssetDatabase.OpenAsset(linked);
                }
                Selection.objects = new Object[] { target };
            }
        }

        private static List<SceneAnnotation> GetSceneAnnotations()
        {
            var list = new List<SceneAnnotation>(Object.FindObjectsByType<SceneAnnotation>(FindObjectsInactive.Include));
            list.Sort();
            return list;
        }

        private void GoToAnnotation(int delta)
        {
            var self = (SceneAnnotation)target;
            var annotationList = GetSceneAnnotations();
            if (annotationList.Count == 0) return;

            var here = annotationList.IndexOf(self);
            if (here < 0) here = 0;
            here += delta;
            here += annotationList.Count;
            here %= annotationList.Count;

            Selection.SetActiveObjectWithContext(annotationList[here].gameObject, null);
            AlignCamera(annotationList[here].transform);
        }

        private static void AlignCamera(Transform targetTransform)
        {
            var view = SceneView.lastActiveSceneView;
            if (view == null || targetTransform == null) return;

            view.LookAt(targetTransform.position, targetTransform.rotation);
        }
    }
}
