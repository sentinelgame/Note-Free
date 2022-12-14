using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sentinel.NotePlus
{

    [System.Serializable]
    public class NoteData : ScriptableObject
    {
        [HideInInspector] public bool snap = true;
        [HideInInspector] public int snapSize = 10;
        [HideInInspector] public Color snapColor = new Color(0, 0, 0, 0.1f);
        [HideInInspector] public Rect noteWindowRect = new Rect(0, 0, 1000, 1000);
        [HideInInspector] public Vector2 scrollPosition = Vector2.zero;
        [HideInInspector] public List<Window> windows = new List<Window>();

        public Window GetWindowWithId(int id) { return windows.Find((x) => x.id == id); }
        public Window GetWindowWithPosition(Vector2 position) { return windows.Find((x) => x.rect.Contains(position)); }

        public void AddWindow(WindowType type)
        {
            Window data = new Window();
            data.type = type;
            data.id = Random.Range(10000, 99999);
            switch (type)
            {
                case WindowType.Window:
                    data.rect = new Rect(0, 0, 100, 100);
                    data.windowMinSize = new Vector2(100, 100);
                    break;
                case WindowType.Header:
                    data.rect = new Rect(0, 0, 100, 30);
                    data.windowMinSize = new Vector2(100, 30);
                    data.windowMaxSize = new Vector2(500, 100);
                    break;
                case WindowType.TextArea:
                    data.rect = new Rect(0, 0, 100, 50);
                    data.windowMinSize = new Vector2(100, 50);
                    break;
            }

            windows.Add(data);
        }

        public void RemoveWindow(int id)
        {
            Window removeWindow = GetWindowWithId(id);
            windows.Remove(removeWindow);

            foreach (Window item in windows)
            {
                item.connects.Remove(item.connects.Find((x) => x.id == id));
            }
        }

        public void Validate()
        {
            snapSize = Mathf.Clamp(snapSize, 5, 50);
            foreach (Window window in windows)
            {
                window.className = window.type.ToString() + "-" + window.id;
            }
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}