using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Kosynierzy
{
    /// <summary>
    /// Settings row that cycles through options ("‹ Wł. ›"): left/right on gamepad or keyboard, Submit,
    /// or a click on the left/right half of the value area. Up/down navigation is left to the Selectable.
    /// </summary>
    public class SettingsMenuStepper : UnityEngine.UI.Selectable, ISubmitHandler, IPointerClickHandler
    {
        [SerializeField] UnityEngine.UI.Text valueText;
        [SerializeField] RectTransform clickArea;

        string[] options = Array.Empty<string>();
        int index;
        Action<int> onChanged;

        public int Index => index;

        public void Setup(UnityEngine.UI.Text text, RectTransform area, string[] newOptions, int startIndex, Action<int> callback)
        {
            valueText = text;
            clickArea = area;
            options = newOptions ?? Array.Empty<string>();
            onChanged = callback;
            SetIndexWithoutNotify(startIndex);
        }

        public void SetIndexWithoutNotify(int newIndex)
        {
            index = options.Length == 0 ? 0 : Mathf.Clamp(newIndex, 0, options.Length - 1);
            Refresh();
        }

        void Step(int delta)
        {
            if (options.Length == 0 || !IsInteractable()) return;
            index = (index + delta + options.Length) % options.Length;
            Refresh();
            onChanged?.Invoke(index);
        }

        void Refresh()
        {
            if (valueText != null) valueText.text = options.Length == 0 ? "-" : options[index];
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (IsActive() && IsInteractable())
            {
                if (eventData.moveDir == MoveDirection.Left) { Step(-1); eventData.Use(); return; }
                if (eventData.moveDir == MoveDirection.Right) { Step(1); eventData.Use(); return; }
            }
            base.OnMove(eventData);
        }

        public void OnSubmit(BaseEventData eventData) => Step(1);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            var area = clickArea != null ? clickArea : (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out var local)
                && area.rect.Contains(local))
            {
                Step(local.x < area.rect.center.x ? -1 : 1);
            }
            else Step(1);
        }
    }
}
