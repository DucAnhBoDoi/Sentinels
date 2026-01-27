using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MainMenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private const float ButtonHoverTargetX = 25;
    private const float ButtonHoverTransitionDuration = 0.25f;

    [SerializeField]
    private TMP_Text _txtBtn;

    private Button _btn;

    private void Awake()
    {
        _btn = GetComponent<Button>();
    }

    private void Start()
    {
        if (!_btn.interactable)
        {
            _txtBtn.color = _btn.colors.disabledColor;
        }
    }

    private void OnDisable()
    {
        DOTween.Kill(this);
        _txtBtn.rectTransform.anchoredPosition = Vector2.zero;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_btn.interactable)
        {
            _txtBtn.rectTransform.DOAnchorPosX(ButtonHoverTargetX, ButtonHoverTransitionDuration).SetId(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_btn.interactable)
        {
            _txtBtn.rectTransform.DOAnchorPosX(0, ButtonHoverTransitionDuration).SetId(this);
        }
    }
}
