using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace eradev.stolenrealm.BetterBattle
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ClickAction: MonoBehaviour, IPointerClickHandler
    {
        // ReSharper disable MemberCanBePrivate.Global UnassignedField.Global
        public UnityAction OnLeftClick;
        public UnityAction OnMiddleClick;
        public UnityAction OnRightClick;
        // ReSharper enable MemberCanBePrivate.Global UnassignedField.Global

        public void OnPointerClick(PointerEventData eventData)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    OnLeftClick?.Invoke();

                    break;

                case PointerEventData.InputButton.Middle:
                    OnMiddleClick?.Invoke();

                    break;

                case PointerEventData.InputButton.Right:
                    OnRightClick?.Invoke();

                    break;
            }
        }
    }
}
