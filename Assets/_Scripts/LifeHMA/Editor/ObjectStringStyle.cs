using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LifeHMA.Utilities.Editor
{
    [CreateAssetMenu(fileName = "HierarchyStyle", menuName ="LifeHMA/Editor/HierarchyStyle")]
    public class ObjectStringStyle : ScriptableObject
    {
        public List<StringStyle> styles = new List<StringStyle>();
    }

    [System.Serializable]
    public class StringStyle
    {
        public string nombre;
        public Font font;
        public Color fontColor;
        public Color backgroundColor;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public Texture2D Icon;
        public Texture2D Background;
        public int size;
        public Vector2 textOffset;
    }
}
