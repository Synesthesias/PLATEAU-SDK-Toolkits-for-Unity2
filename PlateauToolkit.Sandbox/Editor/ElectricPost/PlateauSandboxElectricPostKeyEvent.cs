using PlateauToolkit.Sandbox.Runtime.ElectricPost;
using System.Collections.Generic;
using UnityEngine;

namespace PlateauToolkit.Sandbox.Editor
{
    public class PlateauSandboxElectricPostKeyEvent
    {
        private EventType keyEventType;
        private KeyCode keyCode;
        private List<(string focusKey, PlateauSandboxElectricPost post)> focusKeys = new();

        public void TryAddFocusPost(PlateauSandboxElectricPost post, int count)
        {
            string controlKey = $"{post.name}_{count}";
            if (focusKeys.Exists(x => x.focusKey == controlKey))
            {
                return;
            }
            focusKeys.Add((controlKey, post));
            GUI.SetNextControlName(controlKey);
        }

        public void RemoveFocusPost(PlateauSandboxElectricPost post, int count)
        {
            string controlKey = $"{post.name}_{count}";
            focusKeys.RemoveAll(x => x.focusKey == controlKey);
        }

        private void SetKeyEvent()
        {
            keyEventType = Event.current.type;
            keyCode = Event.current.keyCode;
        }

        public bool IsFocusDelete(out PlateauSandboxElectricPost post)
        {
            post = null;
            if (!IsDeleteKey())
            {
                return false;
            }

            string focusedControl = GUI.GetNameOfFocusedControl();
            var focusKey = focusKeys.Find(x => x.focusKey == focusedControl);
            if (focusKey.focusKey == null)
            {
                return false;
            }
            post = focusKey.post;
            return true;
        }

        public bool IsDeleteKey()
        {
            SetKeyEvent();
            Debug.Log($"keyEventType: {keyEventType}, keyCode: {keyCode}");
            return keyEventType == EventType.KeyUp && keyCode is KeyCode.Backspace or KeyCode.Delete;
        }

        public bool IsEscapeKey()
        {
            SetKeyEvent();
            return keyEventType == EventType.KeyUp && keyCode == KeyCode.Escape;
        }
    }
}