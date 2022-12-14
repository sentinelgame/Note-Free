using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace Sentinel.NotePlus
{
    public class NotePlusEditor : EditorWindow
    {
        NoteData _noteData;
        Vector2 _oldSize;
        bool _scrollButton;
        List<string> _errorDebug = new List<string>();

        // Panel
        public enum PanelType { None,NewNote,OpenNote };
        PanelType _panelType;
        string _newNoteName = "New Note Name";
        string _newNotePath = Directory.GetCurrentDirectory() + "/Assets";
        bool _mainPanelSettings;

        // Mouse tool area information variable.
        AreaType _currentToolArea;
        AreaType _selectedToolArea;
        AreaType _lastToolArea;

        // Rect
        Rect _toolAreaRect;
        Rect _noteAreaRect;

        // Selected Information
        int _selectedWindow = 0;
        int _currentWindow = 0;
        ArrowData _selectedArrow = new ArrowData(0, 0);

        [MenuItem("Sentinel/Note Plus #n")]
        static void ShowEditor()
        {
            NotePlusEditor editor = EditorWindow.GetWindow<NotePlusEditor>("Note Plus");
            editor.Init();
        }

        public void Init()
        {
            this.minSize = new Vector2(415, 300);
            CalculateAreaRect();
        }

        void CalculateAreaRect()
        {
            int toolAreaWidth = 60;
            _toolAreaRect = new Rect(0, 0, toolAreaWidth, position.height);
            _noteAreaRect = new Rect(toolAreaWidth, 0, position.width - toolAreaWidth, position.height);
        }


        void OnGUI()
        {
            if (_noteData == null)
            {
                MenuPanel();
                return;
            }

            FindCurrentToolArea();
            RepaintAndUnfocused();

            EditorGUI.BeginChangeCheck();
            ToolArea();
            NoteArea();
            if (EditorGUI.EndChangeCheck())
                ChangeSaveData();

            // Delete
            if (EditorFunction.DeleteWindow(_selectedWindow, _noteData))
                Repaint();
            if (EditorFunction.DeleteArrow(_selectedArrow, _noteData))
            {
                _selectedArrow = ArrowData.zero;
                Repaint();
            }

            // When the window size changes
            if (_oldSize != position.size)
            {
                _oldSize = position.size;
                CalculateAreaRect();
            }
        }

        void FindCurrentToolArea ()
        {
            _currentToolArea = EditorFunction.OnCurrentMouseItem(_noteData.windows.ToArray(), new Rect[1] { _toolAreaRect }, _noteAreaRect.position - _noteData.scrollPosition);
            if (Event.current.type == EventType.MouseDown & Event.current.button == 0 & _currentToolArea != AreaType.Area)
                _selectedToolArea = _currentToolArea;
        }

        // Calculate situations where it doesn't focus.
        void RepaintAndUnfocused()
        {
            if (_lastToolArea != _currentToolArea)
            {
                if (Event.current.button == 0 & Event.current.type == EventType.MouseDown & _currentToolArea != AreaType.Area)
                {
                    GUI.UnfocusWindow();
                    EditorGUI.FocusTextInControl(null);
                    _selectedArrow = ArrowData.zero;
                    _selectedWindow = 0;
                    _currentWindow = 0;
                    _lastToolArea = _currentToolArea;
                    Repaint();
                }
            }
            if (Event.current.type == EventType.KeyDown & Event.current.keyCode == KeyCode.Delete)
                EditorGUI.FocusTextInControl(null);
        }

        void ToolArea()
        {
            GUILayout.BeginArea(_toolAreaRect);

            GUI.Box(new Rect(0, 0, _toolAreaRect.width, _toolAreaRect.height), "");

            switch (_selectedToolArea)
            {
                case AreaType.Background:
                    if (_mainPanelSettings)
                    {
                        GUILayout.Label("Snap", EditorStyles.centeredGreyMiniLabel);
                        _noteData.snap = EditorGUILayout.Toggle(_noteData.snap);

                        GUILayout.Label("Snap Size", EditorStyles.centeredGreyMiniLabel);
                        _noteData.snapSize = EditorGUILayout.IntField(_noteData.snapSize);

                        GUILayout.Label("Snap Color", EditorStyles.centeredGreyMiniLabel);
                        _noteData.snapColor = EditorGUILayout.ColorField(_noteData.snapColor);
                    }
                    else
                    {
                        Texture2D[] icons = EditorIcon.GetIcons(new EditorIconName[2]
                        { EditorIconName.HeaderIcon, EditorIconName.TextAreaIcon});
                        EditorDraw.MainTool(this, _noteData, icons);
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(EditorIcon.GetIcon(EditorIconName.SettingsIcon), GUILayout.Width(45.5f), GUILayout.Height(27.27f)))
                    {
                        _mainPanelSettings = !_mainPanelSettings;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUIStyle settingsButtonStyle = new GUIStyle(GUI.skin.label);
                    settingsButtonStyle.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label("Settings", settingsButtonStyle);

                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(EditorIcon.GetIcon(EditorIconName.HomeIcon), GUILayout.Width(45.5f), GUILayout.Height(27.27f)))
                    {
                        _noteData = null;
                        _mainPanelSettings = false;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUIStyle homeButtonstyle = new GUIStyle(GUI.skin.label);
                    homeButtonstyle.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label("Home", homeButtonstyle);
                    break;
                case AreaType.Area:

                    break;
                case AreaType.Window:
                    EditorDraw.WindowTool(_noteData.GetWindowWithId(_selectedWindow));
                    break;
                case AreaType.Arrow:
                    EditorDraw.ArrowTool(_noteData, _selectedArrow);
                    break;
            }
            GUILayout.EndArea();
        }

        void NoteArea()
        {
            if (_noteData == null)
                return;

            if (Event.current.type == EventType.MouseUp)
                NoteAreaSize();

            if (Event.current.button == 2 & Event.current.type == EventType.MouseDown)
                _scrollButton = true;
            if (Event.current.button == 2 & Event.current.type == EventType.MouseUp)
                _scrollButton = false;

            if (_scrollButton)
            {
                if (_selectedToolArea == AreaType.Background)
                    NoteAreaMovement();
                GUI.BeginScrollView(_noteAreaRect, _noteData.scrollPosition, _noteData.noteWindowRect);
            }
            else
            {
                _noteData.scrollPosition = GUI.BeginScrollView(_noteAreaRect, _noteData.scrollPosition, _noteData.noteWindowRect);
            }

            if (_noteData.snap)
                EditorDraw.Background(new Rect(0, 0, _noteData.noteWindowRect.width, _noteData.noteWindowRect.height), _noteData.snapSize, _noteData.snapColor);

            // Draw arrow and button
            foreach (Window window in _noteData.windows)
            {
                foreach (Connect connect in window.connects)
                {
                    EditorDraw.BezierButton(_noteData, _selectedArrow, window, connect, EditorIcon.GetIcon(EditorIconName.ArrowHead), EditorIcon.GetIcon(EditorIconName.Circle));
                }
            }

            // Control selected window
            if (_currentToolArea == AreaType.Window)
            {
                if (Event.current.button == 0 & Event.current.type == EventType.MouseDown)
                {
                    _currentWindow = _noteData.GetWindowWithPosition(Event.current.mousePosition).id;
                    if (_currentWindow != _selectedWindow)
                        _noteData.GetWindowWithId(_currentWindow).moveHandleActive = true;
                }
                if (Event.current.button == 0 & Event.current.type == EventType.MouseUp)
                {
                    _selectedWindow = _currentWindow;
                    _currentWindow = 0;
                    _noteData.GetWindowWithId(_selectedWindow).moveHandleActive = false;
                }
            }
            if (Event.current.type == EventType.Ignore)
            {
                foreach (Window item in _noteData.windows)
                {
                    item.moveHandleActive = false;
                }
            }

            BeginWindows();
            foreach (Window window in _noteData.windows)
            {
                // Window draw.
                if (_selectedWindow == window.id)
                    window.rect.Outline(1, Color.cyan);
                GUI.BeginGroup(window.rect);
                DrawWindow(window.id);
                GUI.EndGroup();

                if (_selectedWindow != window.id)
                    EditorGUIUtility.AddCursorRect(window.rect, MouseCursor.MoveArrow);

                // Arrow connect draw.
                if (window.arrowHandleActive)
                {
                    // Calculate arrow start, end, mid positions
                    BezierPosition bezierPosition = EditorFunction.GetBezierPosition(window.rect, new Rect(Event.current.mousePosition, Vector2.one * 20), Vector2.zero);
                    EditorDraw.Bezier(bezierPosition, EditorIcon.GetIcon(EditorIconName.ArrowHead));
                    Vector2 size = new Vector2(50, 20);
                    Rect arrowRect = new Rect(bezierPosition.mid, size);
                    if (Event.current.type == EventType.MouseUp & Event.current.button == 0)
                    {
                        window.arrowHandleActive = false;
                        Window connectWindow = _noteData.GetWindowWithPosition(Event.current.mousePosition);
                        if (connectWindow != null)
                        {
                            if (connectWindow.id == window.id)
                            {
                                Repaint();
                                break;
                            }
                            window.AddConnect(connectWindow.id, arrowRect);
                            ChangeSaveData();
                        }
                    }
                    Repaint();
                }
            }
            EndWindows();

            GUI.EndScrollView();
        }

        void ChangeSaveData()
        {
            if (_noteData == null)
                return;

            _noteData.Validate();
            EditorUtility.SetDirty(_noteData);
        }

        void MenuPanel()
        {
            GUIStyle newNoteButtonStyle = new GUIStyle(GUI.skin.button);
            GUIStyle openNoteButtonStyle = new GUIStyle(GUI.skin.button);

            switch (_panelType)
            {
                case PanelType.NewNote:
                    newNoteButtonStyle.fontStyle = FontStyle.Bold;
                    newNoteButtonStyle.fontSize += 2;
                    break;
                case PanelType.OpenNote:
                    openNoteButtonStyle.fontStyle = FontStyle.Bold;
                    openNoteButtonStyle.fontSize += 2;
                    break;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("New Note", newNoteButtonStyle, GUILayout.Width(100), GUILayout.Height(30)))
            { _panelType = PanelType.NewNote; OnChangePanelType(); }
            if (GUILayout.Button("Open Note", openNoteButtonStyle, GUILayout.Width(100), GUILayout.Height(30)))
            { _panelType = PanelType.OpenNote; OnChangePanelType(); }
            GUILayout.EndHorizontal();
            EditorDraw.HorizontalLine();

            switch (_panelType)
            {
                case PanelType.NewNote:
                    NewNotePanel();
                    break;
                case PanelType.OpenNote:
                    OpenNotePanel();
                    break;
            }

            EditorDraw.ErrorWindowLayout(_errorDebug);
        }

        void OnChangePanelType ()
        {
            _errorDebug.Clear();
        }

        void AddError(string debug)
        {
            if (!_errorDebug.Contains(debug))
                _errorDebug.Add(debug);
        }

        void NewNotePanel ()
        {
            string pathResult = "";
            bool error_WrongFolder = false;
            bool error_SameFolderName = false;
            bool error_nameLenght = false;
            _errorDebug.Clear();

            if (!_newNotePath.Contains("Assets"))
            {
                error_WrongFolder = true;
                AddError("Please select folder from assets folder.");
            }
            if (_newNoteName.Length < 3)
            {
                error_nameLenght = true;
                AddError("Name length must be greater than 3 letters.");
            }
            if (!error_nameLenght & !error_WrongFolder)
            {
                pathResult = "Assets" + _newNotePath.Split("Assets")[1] + "/" + _newNoteName + ".asset";
            }
            if (AssetDatabase.LoadAssetAtPath(pathResult, typeof(NoteData)) != null)
            {
                error_SameFolderName = true;
                AddError("There is another note file with the same name.");
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name",GUILayout.Width(100));
            _newNoteName = EditorGUILayout.TextField(_newNoteName);
            GUILayout.EndHorizontal();

            _newNotePath = EditorDraw.FolderField(_newNotePath);

            if (!error_nameLenght & !error_SameFolderName & !error_WrongFolder)
            {
                if (GUILayout.Button("Create Note"))
                {
                    NoteData asset = ScriptableObject.CreateInstance<NoteData>();
                    AssetDatabase.CreateAsset(asset, pathResult);
                    AssetDatabase.SaveAssets();
                    _panelType = PanelType.None;
                    _noteData = asset;
                }
            }
        }

        void OpenNotePanel ()
        {
            NoteData data = EditorDraw.AllNoteGridButton(position.width);
            if(data != null)
            {
                _panelType = PanelType.None;
                _noteData = data;
            }
        }

        void NoteAreaSize ()
        {
            float offset = 300;
            float maxX = 0;
            float maxY = 0;
            foreach (Window window in _noteData.windows)
            {
                if (window.rect.x + window.rect.width + offset > maxX)
                    maxX = window.rect.x + window.rect.width + offset;
                if (window.rect.y + window.rect.height + offset > maxY)
                    maxY = window.rect.y + window.rect.height + offset;
            }
            _noteData.noteWindowRect.size = new Vector2(maxX, maxY);
            Repaint();
        }

        void NoteAreaMovement()
        {
            _noteData.scrollPosition -= Event.current.delta * 0.5f;
            // Cancel the positive movement direction.
            if (_noteData.scrollPosition.x < 0)
                _noteData.scrollPosition = new Vector2(0, _noteData.scrollPosition.y);
            if (_noteData.scrollPosition.y < 0)
                _noteData.scrollPosition = new Vector2(_noteData.scrollPosition.x, 0);
            // Limit right movement
            int maxPosX = (int)(_noteData.noteWindowRect.width - _noteAreaRect.width);
            if (maxPosX > 0)
            {
                if (_noteData.scrollPosition.x > maxPosX)
                    _noteData.scrollPosition = new Vector2(maxPosX, _noteData.scrollPosition.y);
            }
            // Limit down movement
            int maxPosY = (int)(_noteData.noteWindowRect.height - _noteAreaRect.height);
            if (maxPosY > 0)
            {
                if (_noteData.scrollPosition.y > maxPosY)
                    _noteData.scrollPosition = new Vector2(_noteData.scrollPosition.x, maxPosY);
            }

            if (Event.current.type == EventType.MouseDrag)
                Repaint();
        }

        void DrawWindow(int id)
        {
            bool selected = _selectedWindow == id;

            // Get Window Data with NoteData
            Window window = _noteData.GetWindowWithId(id);
        
            // Size handler
            Rect sizeHandleRect = new Rect(0, 0, 15, 15).SetDirection(window.rect, RectDirection.RightDown);

            // Arrow handler or size function
            Rect arrowHandleRect = new Rect(0, 0, 15, 15).SetDirection(window.rect, RectDirection.UpRight);

            if (selected)
            {
                if (!window.arrowHandleActive)
                {
                    if (window.windowMinSize.magnitude < window.windowMaxSize.magnitude | window.windowMaxSize == Vector2.zero)
                        window.sizeHandleActive = EditorFunction.WindowHandler(sizeHandleRect, window.sizeHandleActive, MouseCursor.ResizeUpLeft);
                }
                if (!window.sizeHandleActive)
                    window.arrowHandleActive = EditorFunction.WindowHandler(arrowHandleRect, window.arrowHandleActive, MouseCursor.Pan);
            }


            if (window.sizeHandleActive)
            {
                window.ResizeMouseDelta();
                Repaint();
            }

            if (window.moveHandleActive)
            {
                window.rect.position += Event.current.delta * 0.5f;
                if (window.rect.position.x < 0)
                    window.rect.position = new Vector2(0, window.rect.position.y);
                if (window.rect.position.y < 0)
                    window.rect.position = new Vector2(window.rect.position.x, 0);
                Repaint();
            }
            else
            {
                if (_noteData.snap & Event.current.type == EventType.MouseUp)
                {
                    window.rect.position = Snapping.Snap(window.rect.position, Vector2.one * _noteData.snapSize);
                    window.rect.size = Snapping.Snap(window.rect.size, Vector2.one * _noteData.snapSize);
                }
            }

            // Window type draw
            GUILayout.BeginArea(new Rect(0, 0, window.rect.width, window.rect.height));
            EditorDraw.WindowDraw(window,selected);
            GUILayout.EndArea();

            if (selected)
            {
                if (!window.arrowHandleActive)
                    if (window.windowMinSize.magnitude < window.windowMaxSize.magnitude | window.windowMaxSize == Vector2.zero)
                        EditorDraw.WindowHandler(sizeHandleRect, EditorIcon.GetIcon(EditorIconName.Scale));
                if (!window.sizeHandleActive)
                    EditorDraw.WindowHandler(arrowHandleRect, EditorIcon.GetIcon( EditorIconName.Arrow));

                Repaint();
            }
        }
    }

}