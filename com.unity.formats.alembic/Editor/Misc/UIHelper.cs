using UnityEngine;
using System;

namespace UnityEditor.Formats.Alembic
{
    class UIHelper
    {
        const float k_IndentMargin = 15.0f;

        static GUIStyle s_HelpBoxStyle = null;

        static GUIStyle helpBoxStyle
        {
            get
            {
                if (s_HelpBoxStyle == null)
                {
                    s_HelpBoxStyle = new GUIStyle()
                    {
                        imagePosition = ImagePosition.ImageLeft,
                        fontSize = 10,
                        wordWrap = true,
                        alignment = TextAnchor.MiddleLeft
                    };
                    s_HelpBoxStyle.normal.textColor = EditorStyles.helpBox.normal.textColor;
                }
                return s_HelpBoxStyle;
            }
        }

        /// <summary>Draw a help box with a button.</summary>
        /// <param name="message">The message.</param>
        /// <param name="buttonLabel">The button text.</param>
        /// <param name="action">When the user clicks the button, Unity performs this action.</param>
        internal static void HelpBoxWithAction(string message, MessageType messageType, string buttonLabel, Action action)
        {
            var messageContent = EditorGUIUtility.TrTextContentWithIcon(message, messageType);

            EditorGUILayout.BeginHorizontal();

            float indent = EditorGUI.indentLevel * k_IndentMargin - EditorStyles.helpBox.margin.left;
            GUILayoutUtility.GetRect(indent, EditorGUIUtility.singleLineHeight, EditorStyles.helpBox, GUILayout.ExpandWidth(false));

            Rect leftRect = GUILayoutUtility.GetRect(new GUIContent(buttonLabel), EditorStyles.miniButton, GUILayout.MinWidth(60), GUILayout.ExpandWidth(false));
            Rect rect = GUILayoutUtility.GetRect(messageContent, EditorStyles.helpBox);
            Rect boxRect = new Rect(leftRect.x, rect.y, rect.xMax - leftRect.xMin, rect.height);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            if (Event.current.type == EventType.Repaint)
                EditorStyles.helpBox.Draw(boxRect, false, false, false, false);

            Rect labelRect = new Rect(boxRect.x + 4, boxRect.y, rect.width - 8, rect.height);
            EditorGUI.LabelField(labelRect, messageContent, helpBoxStyle);

            var buttonRect = leftRect;
            buttonRect.x += rect.width - 2;
            buttonRect.y = rect.yMin + (rect.height - EditorGUIUtility.singleLineHeight) / 2;
            bool clicked = GUI.Button(buttonRect, buttonLabel);

            EditorGUI.indentLevel = oldIndent;
            EditorGUILayout.EndHorizontal();

            if (clicked)
                action();
        }
    }
}
