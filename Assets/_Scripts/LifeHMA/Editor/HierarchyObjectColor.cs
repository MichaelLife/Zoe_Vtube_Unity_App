using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using LifeHMA.Utilities.Editor;
#if UNITY_EDITOR
using UnityEditor;

namespace LifeHMA.Utilities.Editor
{
    /// <summary> Sets a background color for game objects in the Hierarchy tab</summary>
    [UnityEditor.InitializeOnLoad]
#endif
    public class HierarchyObjectColor
    {
        static string[] dataArray;
        static string path;
        private static ObjectStringStyle objectStringStyle;

        private static Vector2 offset = new Vector2(20, 1);

        static HierarchyObjectColor()
        {
            dataArray = AssetDatabase.FindAssets("t:ObjectStringStyle");

            if (dataArray != null)
            {
                path = AssetDatabase.GUIDToAssetPath(dataArray[0]);

                objectStringStyle = AssetDatabase.LoadAssetAtPath<ObjectStringStyle>(path);
                EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
            }
        }

        private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
            if (obj != null)
            {
                Color backgroundColor = Color.white;
                Color textColor = Color.white;
                Color disabledTextColor = new Color(0.6f, 0.6f, 0.6f, 0.75f);
                Color disabledBackgroundColor = new Color(0.3f, 0.3f, 0.3f);
                Texture2D texture = null;
                Texture2D backgroundTexture = null;
                FontStyle _f = FontStyle.Bold;
                TextAnchor _ta = TextAnchor.MiddleLeft;
                int _size = 12;
                Font _font = default;
                Vector2 _offset = Vector2.zero;

                for (int i = 0; i < objectStringStyle.styles.Count; i++)
                {
                    if (obj.name == objectStringStyle.styles[i].nombre)
                    {
                        backgroundColor = objectStringStyle.styles[i].backgroundColor;
                        textColor = objectStringStyle.styles[i].fontColor;
                        _f = objectStringStyle.styles[i].fontStyle;
                        _ta = objectStringStyle.styles[i].alignment;
                        texture = objectStringStyle.styles[i].Icon;
                        _size = objectStringStyle.styles[i].size;
                        _font = objectStringStyle.styles[i].font;
                        backgroundTexture = objectStringStyle.styles[i].Background;
                        _offset = objectStringStyle.styles[i].textOffset;
                    }
                }

                if (backgroundColor != Color.white)
                {
                    Rect offsetRect = new Rect(selectionRect.position + offset + _offset, selectionRect.size);
                    Rect bgRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width + 50, selectionRect.height);

                    string name = obj.name;

                    if (EditorUtility.GetObjectEnabled(obj) == 0)
                    {
                        Rect bgRect2 = new Rect(selectionRect.x, selectionRect.y, selectionRect.width + 50, selectionRect.height);
                        EditorGUI.DrawRect(bgRect2, new Color(0.278431f, 0.278431f, 0.278431f));

                        textColor.a = 0.35f;
                        backgroundColor.a = 0.35f;

                        _f = FontStyle.Italic;
                    }

                    
                    if (backgroundTexture != null)
                    {
                        GUI.DrawTexture(bgRect, backgroundTexture, ScaleMode.StretchToFill, true, 1f, backgroundColor, 0, 0);
                    }
                    else
                    {
                        EditorGUI.DrawRect(bgRect, backgroundColor);
                    }

                    EditorGUI.LabelField(offsetRect, name, new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = textColor },
                        fontStyle = _f,
                        alignment = _ta,
                        fontSize = _size,
                        font = _font
                    }
                    );

                    if (texture != null)
                        GUI.DrawTexture(new Rect(selectionRect.position, new Vector2(selectionRect.height, selectionRect.height)), texture);
                }
            }
        }
    }
}