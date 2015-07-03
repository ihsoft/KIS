using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KIS
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class KISAddonCursor : MonoBehaviour
    {
        private static bool cursorShow = false;
        private static bool partDetectionActive = false;
        
        public static Texture2D cursorTexture = null;
        public static string cursorText, cursorText2, cursorText3 = "";
        public static Part hoveredPart = null;
        private static OnMousePartAction delegateOnMousePartClick;
        private static OnMousePartAction delegateOnMouseEnterPart;
        private static OnMousePartAction delegateOnMouseHoverPart;
        private static OnMousePartAction delegateOnMouseExitPart;
        public delegate void OnMousePartAction(Part part);


        public static void StartPartDetection()
        {
            StartPartDetection(null, null, null, null);
        }

        public static void StartPartDetection(OnMousePartAction onMousePartClick, OnMousePartAction onMouseEnterPart, OnMousePartAction onMouseHoverPart, OnMousePartAction onMouseExitPart)
        {
            delegateOnMousePartClick = onMousePartClick;
            delegateOnMouseEnterPart = onMouseEnterPart;
            delegateOnMouseHoverPart = onMouseHoverPart;
            delegateOnMouseExitPart = onMouseExitPart;
            partDetectionActive = true;
            KIS_Shared.DebugLog("Part detection started");
        }

        public static void StopPartDetection()
        {
            partDetectionActive = false;
            if (hoveredPart && delegateOnMouseExitPart != null)
            {
                delegateOnMouseExitPart(hoveredPart);
            }
            hoveredPart = null;
            KIS_Shared.DebugLog("Part detection stopped");
        }

        void Update()
        {
            if (partDetectionActive)
            {
                Part part = KIS_Shared.GetPartUnderCursor();
                // OnMouseDown
                if (Input.GetMouseButtonDown(0))
                {
                    if (part)
                    {
                        if (delegateOnMousePartClick != null) delegateOnMousePartClick(part);
                    }
                }
                // OnMouseOver   
                if (part)
                {
                    if (delegateOnMouseHoverPart != null) delegateOnMouseHoverPart(part);
                }
                
                if (part)
                {
                    // OnMouseEnter
                    if (part != hoveredPart)
                    {
                        if (hoveredPart)
                        {
                            if (delegateOnMouseExitPart != null) delegateOnMouseExitPart(hoveredPart);
                        }
                        if (delegateOnMouseEnterPart != null) delegateOnMouseEnterPart(part);
                        hoveredPart = part;
                    }
                }
                else
                {
                    // OnMouseExit
                    if (part != hoveredPart)
                    {
                        if (delegateOnMouseExitPart != null) delegateOnMouseExitPart(hoveredPart);
                        hoveredPart = null;
                    }
                }
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!UIManager.instance.DidPointerHitUI(0) && InputLockManager.IsUnlocked(ControlTypes.EDITOR_PAD_PICK_PLACE))
                    {
                        Part part = KIS_Shared.GetPartUnderCursor();
                        if (part)
                        {
                            if (delegateOnMousePartClick != null) delegateOnMousePartClick(part);
                        }
                    }
                }
            }
        }

        public static void CursorEnable(string texturePath, string text = "", string text2 = "", string text3 = "")
        {
            cursorShow = true;
            Screen.showCursor = false;
            cursorTexture = GameDatabase.Instance.GetTexture(texturePath, false);
            cursorText = text;
            cursorText2 = text2;
            cursorText3 = text3;
        }

        public static void CursorDefault()
        {
            cursorShow = false;
            Screen.showCursor = true;
        }

        public static void CursorDisable()
        {
            cursorShow = false;
            Screen.showCursor = false;
        }

        private void OnGUI()
        {
            /*
            if (draggedPart)
            {
                GUI.depth = 0;
                GUI.DrawTexture(new Rect(Event.current.mousePosition.x - (draggedIconSize / 2), Event.current.mousePosition.y - (draggedIconSize / 2), draggedIconSize, draggedIconSize), icon.texture, ScaleMode.ScaleToFit);
            */

            if (cursorShow)
            {
                GUI.DrawTexture(new Rect(Event.current.mousePosition.x - 12, Event.current.mousePosition.y - 12, 24, 24), cursorTexture, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(Event.current.mousePosition.x + 16, Event.current.mousePosition.y - 10, 400, 20), cursorText);

                GUIStyle StyleComments = new GUIStyle(GUI.skin.label);
                StyleComments.fontSize = 10;
                GUI.Label(new Rect(Event.current.mousePosition.x + 16, Event.current.mousePosition.y + 5, 400, 20), cursorText2, StyleComments);
                GUI.Label(new Rect(Event.current.mousePosition.x + 16, Event.current.mousePosition.y + 20, 400, 20), cursorText3, StyleComments);
            }
        }
    }
}
