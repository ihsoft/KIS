using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KIS
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class KISAddonCursor : MonoBehaviour
    {
        private static bool cursorShow = false;
        private static bool partDetectionActive = false;
        
        private static Texture2D cursorTexture = null;
        private static string cursorText;
        private static List<string> cursorAdditionalTexts;
        public static Part hoveredPart = null;
        public static int partClickedFrame = -1;
        private static OnMousePartAction delegateOnMousePartClick;
        private static OnMousePartAction delegateOnMouseEnterPart;
        private static OnMousePartAction delegateOnMouseHoverPart;
        private static OnMousePartAction delegateOnMouseExitPart;
        public delegate void OnMousePartAction(Part part);

        // Cursor hint text settings.
        private const int ActionIconSize = 24;  // It's quare.
        private const int HintFontSize = 10;
        private const int HintTextLeftMargin = 4;  // A gap between action icon and the text.
        private static Color hintBackground = new Color(0.0f, 0.0f, 0.0f, 0.5f);
        private static GUIStyle hintWindowStyle = new GUIStyle {
            normal = {
                background = CreateTextureFromColour(hintBackground),
                textColor = Color.white
            },
            padding = new RectOffset(3, 3, 3, 3),
            fontSize = HintFontSize
        };

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
        }

        public static void StopPartDetection()
        {
            partDetectionActive = false;
            if (hoveredPart && delegateOnMouseExitPart != null)
            {
                delegateOnMouseExitPart(hoveredPart);
            }
            hoveredPart = null;
        }

        public void Update() {
            KSP_Dev.LoggedCallWrapper.Action(Internal_Update);
        }

        private void Internal_Update()
        {
            if (partDetectionActive)
            {
                Part part = KIS_Shared.GetPartUnderCursor();
                // OnMouseDown
                if (Input.GetMouseButtonDown(0))
                {
                    if (part)
                    {
                        partClickedFrame = Time.frameCount;
                        KSP_Dev.Logger.logTrace("Set click frame {0}", partClickedFrame);
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

        public static void CursorEnable(string texturePath, string text, string text2)
        {
            List<String> texts = new List<String>();
            texts.Add(text2);
            CursorEnable(texturePath, text, texts);
        }

        public static void CursorEnable(string texturePath, string text, List<string> additionalTexts = null)
        {
            cursorShow = true;
            Screen.showCursor = false;
            cursorTexture = GameDatabase.Instance.GetTexture(texturePath, false);
            cursorText = text;
            cursorAdditionalTexts = additionalTexts;
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

        /// <summary>Makes a texture with the requested background color.</summary>
        /// <remarks>
        /// Borrowed from <see href="https://github.com/CYBUTEK/KerbalEngineer">KER Redux</see>
        /// </remarks>
        /// <param name="colour"></param>
        /// <returns></returns>
        private static Texture2D CreateTextureFromColour(Color colour)
        {
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            texture.SetPixel(1, 1, colour);
            texture.Apply();
            return texture;
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
                // Display action icon.
                GUI.DrawTexture(
                    new Rect(Event.current.mousePosition.x - ActionIconSize / 2,
                             Event.current.mousePosition.y - ActionIconSize / 2,
                             ActionIconSize, ActionIconSize),
                    cursorTexture, ScaleMode.ScaleToFit);
                
                // Compile the whole hint text.
                var allLines = new List<String>{cursorText};
                if (cursorAdditionalTexts != null && cursorAdditionalTexts.Any()) {
                    allLines.Add("");  // A linefeed between status and hint text. 
                    allLines.AddRange(cursorAdditionalTexts);
                }
                var hintText = String.Join("\n", allLines.ToArray());
                // Calculate the label region.
                Vector2 textSize = hintWindowStyle.CalcSize(new GUIContent(hintText));
                var hintLabelRect = new Rect(
                    Event.current.mousePosition.x + ActionIconSize / 2 + HintTextLeftMargin,
                    Event.current.mousePosition.y - ActionIconSize / 2,
                    textSize.x, textSize.y);

                GUI.Label(hintLabelRect, hintText, hintWindowStyle);
            }
        }
    }
}
