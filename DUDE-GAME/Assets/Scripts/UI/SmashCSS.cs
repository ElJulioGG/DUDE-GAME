using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SmashCSS : MonoBehaviour {

    private GridLayoutGroup gridLayout;
    [HideInInspector]
    public Vector2 slotArtworkSize;

    public static SmashCSS instance;

    [Header("Characters List")]
    public List<Character> characters = new List<Character>();

    [Space]
    [Header("Public References")]
    public GameObject charCellPrefab;
    public GameObject gridBgPrefab;
    public Transform playerSlotsContainer;

    [Space]
    [Header("Current Confirmed Character")]
    public Character[] confirmedCharacters = new Character[4];

    // Cached references to avoid repeated Find/GetComponent calls per hover event
    private class SlotCache
    {
        public Transform root;
        public Image artwork;
        public RectTransform artworkRect;
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI playerText;
        public TextMeshProUGUI iconPxText;
    }
    private SlotCache[] _slots;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        gridLayout = GetComponent<GridLayoutGroup>();
        GetComponent<RectTransform>().sizeDelta = new Vector2(gridLayout.cellSize.x * 5, gridLayout.cellSize.y * 2);

        RectTransform gridBG = Instantiate(gridBgPrefab, transform.parent).GetComponent<RectTransform>();
        gridBG.transform.SetSiblingIndex(transform.GetSiblingIndex());
        gridBG.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        slotArtworkSize = playerSlotsContainer.GetChild(0).Find("artwork").GetComponent<RectTransform>().sizeDelta;

        CacheSlotReferences();

        foreach (Character character in characters)
            SpawnCharacterCell(character);
    }

    private void CacheSlotReferences()
    {
        int count = playerSlotsContainer.childCount;
        _slots = new SlotCache[count];
        for (int i = 0; i < count; i++)
        {
            Transform slot = playerSlotsContainer.GetChild(i);
            Transform artworkTransform = slot.Find("artwork");
            _slots[i] = new SlotCache
            {
                root        = slot,
                artwork     = artworkTransform.GetComponent<Image>(),
                artworkRect = artworkTransform.GetComponent<RectTransform>(),
                icon        = slot.Find("icon").GetComponent<Image>(),
                nameText    = slot.Find("name").GetComponent<TextMeshProUGUI>(),
                playerText  = slot.Find("player").GetComponentInChildren<TextMeshProUGUI>(),
                iconPxText  = slot.Find("iconAndPx").GetComponentInChildren<TextMeshProUGUI>(),
            };
        }
    }

    private void SpawnCharacterCell(Character character)
    {
        GameObject charCell = Instantiate(charCellPrefab, transform);
        charCell.name = character.characterName;

        Image artwork = charCell.transform.Find("artwork").GetComponent<Image>();
        TextMeshProUGUI nameLabel = charCell.transform.Find("nameRect").GetComponentInChildren<TextMeshProUGUI>();

        artwork.sprite = character.characterSprite;
        nameLabel.text = character.characterName;

        RectTransform artRect = artwork.GetComponent<RectTransform>();
        artRect.pivot = uiPivot(artwork.sprite);
        artRect.sizeDelta *= character.zoom;
    }

    public void ShowCharacterInSlot(int player, Character character)
    {
        bool nullChar = character == null;
        SlotCache s = _slots[player];

        Color alpha = nullChar ? Color.clear : Color.white;
        Sprite artwork = nullChar ? null : character.characterSprite;
        string charName = nullChar ? string.Empty : character.characterName;

        Sequence seq = DOTween.Sequence();
        seq.Append(s.artworkRect.DOLocalMoveX(-300, .05f).SetEase(Ease.OutCubic));
        seq.AppendCallback(() => { s.artwork.sprite = artwork; s.artwork.color = alpha; });
        seq.Append(s.artworkRect.DOLocalMoveX(300, 0));
        seq.Append(s.artworkRect.DOLocalMoveX(0, .05f).SetEase(Ease.OutCubic));

        if (nullChar)
        {
            s.icon.DOFade(0, 0);
        }
        else
        {
            s.icon.sprite = character.characterIcon;
            s.icon.DOFade(.3f, 0);
        }

        if (artwork != null)
        {
            s.artworkRect.pivot = uiPivot(artwork);
            s.artworkRect.sizeDelta = slotArtworkSize;
            s.artworkRect.sizeDelta *= character.zoom;
        }

        s.nameText.text = charName;
        s.playerText.text = "Player " + (player + 1);
        s.iconPxText.text = "P" + (player + 1);
    }

    public void ConfirmCharacter(int player, Character character)
    {
        if (confirmedCharacters[player] == null)
        {
            confirmedCharacters[player] = character;
            _slots[player].root.DOPunchPosition(Vector3.down * 3, .3f, 10, 1);
        }
    }

    public Vector2 uiPivot(Sprite sprite)
    {
        Vector2 pixelSize = new Vector2(sprite.texture.width, sprite.texture.height);
        Vector2 pixelPivot = sprite.pivot;
        return new Vector2(pixelPivot.x / pixelSize.x, pixelPivot.y / pixelSize.y);
    }

    public void ClearConfirmedCharacter(int player)
    {
        confirmedCharacters[player] = null;
        ShowCharacterInSlot(player, null);
    }
}
