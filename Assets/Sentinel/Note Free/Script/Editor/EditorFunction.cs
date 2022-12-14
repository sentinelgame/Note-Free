using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System;

namespace Sentinel.NotePlus
{
    public static class EditorFunction
    {
        // EXTENTIONS

        /// <summary>
        /// Scales the window according to the delta position of the mouse.
        /// </summary>
        public static void ResizeMouseDelta(this Window window)
        {
            float deltaX = Event.current.delta.x * 0.5f;
            float deltaY = Event.current.delta.y * 0.5f;

            bool widthMaxLimit = false;
            bool heightMaxLimit = false;

            if (window.windowMaxSize == Vector2.zero)
            {
                widthMaxLimit = true;
                heightMaxLimit = true;
            }
            else
            {
                widthMaxLimit = (window.rect.width + deltaX) < window.windowMaxSize.x;
                heightMaxLimit = (window.rect.height + deltaY) < window.windowMaxSize.y;
            }

            if ((window.rect.width + deltaX) > window.windowMinSize.x & widthMaxLimit)
            {
                window.rect.xMax += deltaX;
            }
            if ((window.rect.height + deltaY) > window.windowMinSize.y & heightMaxLimit)
            {
                window.rect.yMax += deltaY;
            }
        }

        /// <summary>
        /// Rotate by angel.
        /// </summary>
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            float tx = v.x;
            float ty = v.y;

            return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
        }

        /// <summary>
        /// Calculates the rect position relative to the parent.
        /// </summary>
        public static Rect SetDirection(this Rect rect, Rect _parent, RectDirection direction)
        {
            switch (direction)
            {
                case RectDirection.Up:
                    return new Rect(new Vector2((_parent.width / 2) - (rect.width / 2), 0), rect.size);
                case RectDirection.UpRight:
                    return new Rect(new Vector2(_parent.width - rect.width, 0), rect.size);
                case RectDirection.Right:
                    return new Rect(new Vector2(_parent.width - rect.width, (_parent.height / 2) - (rect.height / 2)), rect.size);
                case RectDirection.RightDown:
                    return new Rect(new Vector2(_parent.width - rect.width, _parent.height - rect.height), rect.size);
                case RectDirection.Down:
                    return new Rect(new Vector2((_parent.width / 2) - (rect.width / 2), _parent.height - rect.height), rect.size);
                case RectDirection.DownLeft:
                    return new Rect(new Vector2(0, _parent.height - rect.height), rect.size);
                case RectDirection.Left:
                    return new Rect(new Vector2(0, (_parent.height / 2) - (rect.height / 2)), rect.size);
                case RectDirection.LeftUp:
                    return rect;
                default:
                    return rect;
            }
        }

        // FUNCTIONS

        /// <summary>
        /// The area where the mouse is located relative to the colliders.
        /// </summary>
        /// <param name="windows">Windows collider.</param>
        /// <param name="otherArea">Other collider.</param>
        /// <param name="offset">Offset.</param>
        /// <returns>Current Area Type.</returns>
        public static AreaType OnCurrentMouseItem(Window[] windows, Rect[] otherArea, Vector2 offset)
        {
            AreaType result = AreaType.Background;

            // Window control
            foreach (var item in windows)
                if (item.rect.Contains(Event.current.mousePosition - offset))
                {
                    result = AreaType.Window;
                    break;
                }

            // Window arrow button control
            foreach (Window window in windows)
                foreach (Connect connect in window.connects)
                {
                    if (connect.rect.Contains(Event.current.mousePosition - offset))
                    {
                        result = AreaType.Arrow;
                        break;
                    }
                }

            // Other areas control
            foreach (Rect item in otherArea)
                if (item.Contains(Event.current.mousePosition))
                {
                    result = AreaType.Area;
                    break;
                }

            return result;
        }

        /// <summary>
        /// Returns true if the object is deleted.
        /// </summary>
        /// <param name="windowID">Selected window id.</param>
        /// <param name="data"></param>
        public static bool DeleteWindow(int windowID, NoteData data)
        {
            bool result = false;
            // Delete select window.
            if (windowID != 0)
                if (Event.current.type == EventType.KeyDown & Event.current.keyCode == KeyCode.Delete)
                {
                    result = true;
                    data.RemoveWindow(windowID);
                }

            return result;
        }

        /// <summary>
        /// Returns true if the object is deleted.
        /// </summary>
        /// <param name="arrowData">Selected arrow data.</param>
        /// <param name="data"></param>
        public static bool DeleteArrow(ArrowData arrowData, NoteData data)
        {
            bool result = false;
            // Delete select arrow.
            if (arrowData.windowID != 0)
                if (Event.current.type == EventType.KeyDown & Event.current.keyCode == KeyCode.Delete)
                {
                    result = true;
                    data.GetWindowWithId(arrowData.windowID).RemoveConnect(arrowData.arrowID);
                }

            return result;
        }

        /// <summary>
        /// Recalculates the position according to the specific direction.
        /// </summary>
        public static Vector2 MouseDeltaDirection (Vector2 position,Vector2 target)
        {
            Vector2 dir = target - position;
            float angel = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
            return Event.current.delta.Rotate(angel) * 0.5f;
        }

        /// <summary>
        /// Draw window handler
        /// </summary>
        /// <param name="rect">Handler size.</param>
        /// <param name="handleActive">Handle active.</param>
        /// <param name="mouseCursor">Mouse cursor type.</param>
        /// <returns></returns>
        public static bool WindowHandler(Rect rect, bool handleActive, MouseCursor mouseCursor)
        {
            if (GUIUtility.hotControl == 0)
                handleActive = false;
            EditorGUIUtility.AddCursorRect(rect, mouseCursor);
            bool action = (Event.current.type == EventType.MouseDown) || (Event.current.type == EventType.MouseDrag);
            if (!handleActive && action)
            {
                if (rect.Contains(Event.current.mousePosition, true))
                {
                    handleActive = true;
                    GUIUtility.hotControl = EditorGUIUtility.GetControlID(FocusType.Passive);
                }
            }
            return handleActive;
        }

        /// <summary>
        /// Calculates the middle, start and end points between two windows.Return bezier positions.
        /// </summary>
        /// <param name="startRect">Start window rect.</param>
        /// <param name="endRect">End window rect.</param>
        /// <param name="offset">Mid position offset.</param>
        public static BezierPosition GetBezierPosition(Rect startRect, Rect endRect, Vector2 offset)
        {
            Vector2 startPos = new Vector2(startRect.x + startRect.width / 2, startRect.y + startRect.height / 2);
            Vector2 endPos = new Vector2(endRect.x + endRect.width / 2, endRect.y + endRect.height / 2);
            Vector2 r_dotPos = Vector2.zero;
            Vector2 r_arrowPos = Vector2.zero;
            Vector2 r_midPos = Vector2.zero;

            // Point closest to end window
            int lenght = (int)Vector3.Distance(startPos, endPos);
            for (int i = 0; i < lenght; i++)
            {
                r_dotPos = Vector3.Lerp(startPos, endPos, (float)i / lenght);
                if (!startRect.Contains(r_dotPos))
                    break;
            }

            for (int i = lenght; i > 1; i--)
            {
                r_arrowPos = Vector2.Lerp(startPos, endPos, (float)i / lenght);
                if (!endRect.Contains(r_arrowPos))
                {
                    Vector2 horizontalAxis = (r_dotPos - r_arrowPos).normalized;
                    Vector2 verticalAxis = Vector2.Perpendicular(horizontalAxis);
                    Vector2 tangentResult = horizontalAxis * offset.x + verticalAxis * offset.y;
                    r_midPos = Vector2.Lerp(r_dotPos, r_arrowPos, 0.5f) + tangentResult;
                    break;
                }
            }

            return new BezierPosition(r_dotPos, r_midPos, r_arrowPos);
        }
    }
}