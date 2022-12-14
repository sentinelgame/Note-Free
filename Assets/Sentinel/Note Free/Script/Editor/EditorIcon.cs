using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sentinel.NotePlus
{
    // Name of the icons in the file path (Assets/Sentinel/Note/Icon)
    public enum EditorIconName
    {
        Arrow,
        ArrowHead,
        Circle,
        HeaderIcon,
        HomeIcon,
        Scale,
        SettingsIcon,
        TextAreaIcon,
    }

    public static class EditorIcon
    {
        [System.Serializable]
        private class TryGetData
        {
            public EditorIconName iconName;
            public Texture2D icon;

            public TryGetData(EditorIconName iconName, Texture2D icon) { this.iconName = iconName; this.icon = icon; }
        }

        static List<TryGetData> tryGetDatas = new List<TryGetData>();

        /// <summary>
        /// Editor icons.
        /// </summary>
        public static Texture2D GetIcon(EditorIconName name)
        {
            TryGetData tryGetData = tryGetDatas.Find((x) => x.iconName == name);
            if (tryGetData == null)
            {
                Texture2D icon = AssetDatabase.LoadAssetAtPath("Assets/Sentinel/Note Free/Icon/" + name + ".png", typeof(Texture2D)) as Texture2D;
                tryGetDatas.Add(new TryGetData(name, icon));
                return icon;
            }
            else
            {
                return tryGetData.icon;
            }
        }

        public static Texture2D[] GetIcons(EditorIconName[] names)
        {
            List<Texture2D> result = new List<Texture2D>();
            foreach (EditorIconName name in names)
            {
                result.Add(GetIcon(name));
            }
            return result.ToArray();
        }

    }
}
