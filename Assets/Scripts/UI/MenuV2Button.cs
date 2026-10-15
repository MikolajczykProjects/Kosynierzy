using UnityEngine;
using UnityEngine.UI;

namespace Kosynierzy
{
    /// <summary>Button of the v2 menus: restyles itself (text colour, bar, fill) on hover / selection instead of using a ColorBlock.</summary>
    public class MenuV2Button : Button
    {
        MenuV2.Kind kind;
        Image background, bar;
        Text label;
        bool ready;

        public void Init(MenuV2.Kind newKind, Image newBackground, Image newBar, Text newLabel)
        {
            kind = newKind;
            background = newBackground;
            bar = newBar;
            label = newLabel;
            ready = true;
            Apply(SelectionState.Normal);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);
            Apply(state);
        }

        void Apply(SelectionState state)
        {
            if (!ready || label == null) return;
            bool on = state == SelectionState.Highlighted || state == SelectionState.Selected || state == SelectionState.Pressed;
            bool off = state == SelectionState.Disabled;
            switch (kind)
            {
                case MenuV2.Kind.Item:
                    label.color = off ? MenuV2.Bone3 : on ? MenuV2.Bone : MenuV2.Bone2;
                    if (bar != null) bar.enabled = on;
                    break;
                case MenuV2.Kind.Filled:
                    background.color = off ? MenuV2.Bone3 : on ? Color.white : MenuV2.Bone;
                    label.color = MenuV2.Ink;
                    break;
                case MenuV2.Kind.Outline:
                    background.color = on ? new Color(MenuV2.Bone.r, MenuV2.Bone.g, MenuV2.Bone.b, 0.14f) : Color.clear;
                    label.color = off ? MenuV2.Bone3 : MenuV2.Bone;
                    break;
                default: // Tab
                    label.color = off ? MenuV2.Bone3 : on ? MenuV2.Bone : MenuV2.Bone2;
                    break;
            }
        }
    }
}
