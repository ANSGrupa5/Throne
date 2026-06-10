using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum OpponentSlotOccupant
{
    Empty,
    Bot,
    Player,
    Host
}

[Serializable]
public sealed class OpponentSlotEntryView
{
    private const string AddBotButtonName = "AddBotButton";
    private const string RemoveBotButtonName = "RemoveBotButton";
    private const string SlotFrameName = "SlotFrame";
    private const string SlotStatusTextName = "SlotStatusText";

    private static readonly Color EmptyFill = new(0.012f, 0.018f, 0.03f, 0.82f);
    private static readonly Color EmptyBorder = new(0f, 0.996f, 0.925f, 0.62f);
    private static readonly Color OccupiedFill = new(0.012f, 0.018f, 0.03f, 0.9f);
    private static readonly Color OccupiedBorder = new(0f, 0.996f, 0.925f, 1f);
    private static readonly Color OccupiedText = new(0f, 0.996f, 0.925f, 1f);

    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button addBotButton;
    [SerializeField] private Button removeBotButton;
    [SerializeField] private Image frameImage;
    [SerializeField] private Outline frameOutline;

    [NonSerialized] private UnityAction _addBotAction;
    [NonSerialized] private UnityAction _removeBotAction;

    public bool IsAssigned => root != null;

    public void Bind(UnityAction addBotAction, UnityAction removeBotAction)
    {
        ResolveReferences();
        Unbind();

        _addBotAction = addBotAction;
        _removeBotAction = removeBotAction;

        if (addBotButton != null && _addBotAction != null)
            addBotButton.onClick.AddListener(_addBotAction);

        if (removeBotButton != null && _removeBotAction != null)
            removeBotButton.onClick.AddListener(_removeBotAction);
    }

    public void Unbind()
    {
        ResolveReferences();

        if (addBotButton != null && _addBotAction != null)
            addBotButton.onClick.RemoveListener(_addBotAction);

        if (removeBotButton != null && _removeBotAction != null)
            removeBotButton.onClick.RemoveListener(_removeBotAction);

        _addBotAction = null;
        _removeBotAction = null;
    }

    public void ApplyState(string label, OpponentSlotOccupant occupant, bool canAddBot, bool canRemoveBot)
    {
        ResolveReferences();

        bool occupied = occupant != OpponentSlotOccupant.Empty;

        if (addBotButton != null)
        {
            addBotButton.gameObject.SetActive(canAddBot);
            addBotButton.interactable = canAddBot;
        }

        if (removeBotButton != null)
        {
            removeBotButton.gameObject.SetActive(canRemoveBot);
            removeBotButton.interactable = canRemoveBot;
        }

        if (statusText != null)
        {
            statusText.raycastTarget = false;
            statusText.text = occupied ? label : string.Empty;
            statusText.color = OccupiedText;
            statusText.gameObject.SetActive(occupied);
            statusText.transform.SetAsLastSibling();
        }

        UpdateFrame(occupied);
    }

    public void Validate(UnityEngine.Object owner, int index)
    {
        ResolveReferences();

        if (root == null)
        {
            Debug.LogError($"{nameof(LobbyController)} is missing opponent slot root at index {index}.", owner);
            return;
        }

        if (statusText == null)
            Debug.LogError($"{nameof(LobbyController)} is missing opponent slot status text at index {index}.", owner);
        if (addBotButton == null)
            Debug.LogError($"{nameof(LobbyController)} is missing opponent slot add button at index {index}.", owner);
        if (removeBotButton == null)
            Debug.LogError($"{nameof(LobbyController)} is missing opponent slot remove button at index {index}.", owner);
    }

    private void UpdateFrame(bool occupied)
    {
        if (frameImage != null)
            frameImage.color = occupied ? OccupiedFill : EmptyFill;
        if (frameOutline != null)
            frameOutline.effectColor = occupied ? OccupiedBorder : EmptyBorder;
    }

    private void ResolveReferences()
    {
        if (root == null)
            return;

        if (statusText == null)
            statusText = FindChildComponent<TMP_Text>(SlotStatusTextName);
        if (addBotButton == null)
            addBotButton = FindChildComponent<Button>(AddBotButtonName);
        if (removeBotButton == null)
            removeBotButton = FindChildComponent<Button>(RemoveBotButtonName);
        if (frameImage == null)
            frameImage = FindChildComponent<Image>(SlotFrameName);
        if (frameOutline == null)
            frameOutline = FindChildComponent<Outline>(SlotFrameName);
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        Transform child = root.Find(childName);
        return child != null && child.TryGetComponent(out T component) ? component : null;
    }
}
