using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public sealed class LobbySlotView
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button addBotButton;
    [SerializeField] private Button removeBotButton;
    [SerializeField] private Image frameImage;
    [SerializeField] private Outline frameOutline;

    [NonSerialized] private MatchLobbyController _boundLobby;
    [NonSerialized] private int _boundSlotIndex = -1;
    [NonSerialized] private UnityAction _addBotAction;
    [NonSerialized] private UnityAction _removeBotAction;
    [NonSerialized] private Graphic _removeBotGraphic;
    [NonSerialized] private Color _removeBotGraphicColor;
    [NonSerialized] private bool _hasRemoveBotGraphicColor;
    [NonSerialized] private CanvasGroup _removeBotCanvasGroup;

    public bool IsAssigned => root != null;

    public void Bind(MatchLobbyController lobby, int slotIndex)
    {
        if (_boundLobby == lobby && _boundSlotIndex == slotIndex)
            return;

        Unbind();
        if (lobby == null)
            return;

        _boundLobby = lobby;
        _boundSlotIndex = slotIndex;

        if (addBotButton != null)
        {
            addBotButton.onClick = new Button.ButtonClickedEvent();
            _addBotAction = () => lobby.SetBotSlot(slotIndex, true);
            addBotButton.onClick.AddListener(_addBotAction);
        }

        if (removeBotButton != null)
        {
            removeBotButton.onClick = new Button.ButtonClickedEvent();
            _removeBotAction = () => lobby.SetBotSlot(slotIndex, false);
            removeBotButton.onClick.AddListener(_removeBotAction);
            CacheRemoveBotGraphic();
        }
    }

    public void Unbind()
    {
        if (addBotButton != null && _addBotAction != null)
            addBotButton.onClick.RemoveListener(_addBotAction);

        if (removeBotButton != null && _removeBotAction != null)
            removeBotButton.onClick.RemoveListener(_removeBotAction);

        _boundLobby = null;
        _boundSlotIndex = -1;
        _addBotAction = null;
        _removeBotAction = null;
    }

    public void ApplyState(string label, bool isHuman, bool isBot, bool canEditBots)
    {
        bool occupied = isHuman || isBot;

        if (statusText != null)
        {
            statusText.raycastTarget = false;
            statusText.text = occupied ? label : string.Empty;
            statusText.gameObject.SetActive(occupied);
            statusText.transform.SetAsLastSibling();
        }

        if (addBotButton != null)
        {
            addBotButton.gameObject.SetActive(canEditBots && !occupied);
            addBotButton.interactable = canEditBots && !occupied;
        }

        if (removeBotButton != null)
        {
            bool canRemoveBot = canEditBots && isBot;
            removeBotButton.gameObject.SetActive(canRemoveBot);
            removeBotButton.interactable = canRemoveBot;
            if (canRemoveBot)
            {
                SetRemoveBotGraphicVisible(false);
                removeBotButton.transform.SetAsLastSibling();
            }
            else
            {
                SetRemoveBotGraphicVisible(true);
            }
        }

        UpdateFrame(isHuman, isBot);
    }

    public void Validate(UnityEngine.Object owner, int index)
    {
        if (root == null)
            Debug.LogError($"{nameof(MatchLobbyController)} is missing lobby slot root at index {index}.", owner);
        if (statusText == null)
            Debug.LogError($"{nameof(MatchLobbyController)} is missing lobby slot status text at index {index}.", owner);
        if (addBotButton == null)
            Debug.LogError($"{nameof(MatchLobbyController)} is missing lobby slot add button at index {index}.", owner);
        if (removeBotButton == null)
            Debug.LogError($"{nameof(MatchLobbyController)} is missing lobby slot remove button at index {index}.", owner);
    }

    private void UpdateFrame(bool isHuman, bool isBot)
    {
        bool occupied = isHuman || isBot;
        Color fill = occupied
            ? new Color(0.015f, 0.018f, 0.02f, 0.82f)
            : new Color(1f, 1f, 1f, 0.025f);
        Color border = isHuman
            ? new Color(0f, 0.996f, 0.925f, 0.82f)
            : isBot
                ? new Color(0.72f, 0.82f, 0.88f, 0.72f)
                : new Color(1f, 1f, 1f, 0.2f);

        if (frameImage != null)
            frameImage.color = fill;
        if (frameOutline != null)
            frameOutline.effectColor = border;
    }

    private void CacheRemoveBotGraphic()
    {
        if (removeBotButton == null || _removeBotGraphic != null)
            return;

        _removeBotGraphic = removeBotButton.targetGraphic != null
            ? removeBotButton.targetGraphic
            : removeBotButton.GetComponent<Graphic>();

        if (_removeBotGraphic != null)
        {
            _removeBotGraphicColor = _removeBotGraphic.color;
            _hasRemoveBotGraphicColor = true;
        }
    }

    private void SetRemoveBotGraphicVisible(bool visible)
    {
        CacheRemoveBotGraphic();

        if (removeBotButton != null && _removeBotCanvasGroup == null)
            _removeBotCanvasGroup = removeBotButton.GetComponent<CanvasGroup>() ??
                removeBotButton.gameObject.AddComponent<CanvasGroup>();

        if (_removeBotCanvasGroup != null)
        {
            _removeBotCanvasGroup.alpha = visible ? 1f : 0f;
            _removeBotCanvasGroup.blocksRaycasts = true;
            _removeBotCanvasGroup.interactable = true;
        }

        if (_removeBotGraphic == null)
            return;

        if (!_hasRemoveBotGraphicColor)
        {
            _removeBotGraphicColor = _removeBotGraphic.color;
            _hasRemoveBotGraphicColor = true;
        }

        Color color = _removeBotGraphicColor;
        color.a = visible ? _removeBotGraphicColor.a : 0f;
        _removeBotGraphic.color = color;
    }
}
