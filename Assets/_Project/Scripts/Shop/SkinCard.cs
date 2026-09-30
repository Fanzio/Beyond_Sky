// Visualizza una skin del negozio e il relativo pulsante di azione.
using TMPro;
using UnityEngine;

namespace BeyondSky.Shop
{
    public class SkinCard : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image previewImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text buttonText;
        [SerializeField] private UnityEngine.UI.Button actionButton;
        [SerializeField] private UnityEngine.UI.Outline equippedBorder;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Color normalText = Color.white;
        [SerializeField] private Color insufficientText = new Color(1f, 0.48f, 0.42f, 1f);
        [SerializeField] private Color equippedText = new Color(1f, 0.83f, 0.3f, 1f);
        private ShopPanel shop;
        private int index;

        private void OnEnable()
        {
            actionButton.onClick.AddListener(OnAction);
        }

        private void OnDisable()
        {
            actionButton.onClick.RemoveListener(OnAction);
        }

        public void Initialize(ShopPanel owner, int skinIndex)
        {
            shop = owner;
            index = skinIndex;
        }

        public void Refresh(SkinEntry skin, bool owned, bool equipped, int coins)
        {
            nameText.text = "Skin " + index;
            if (skin != null)
            {
                nameText.text = skin.DisplayName;
            }
            equippedBorder.enabled = false;
            previewImage.enabled = false;
            lockOverlay.SetActive(true);
            actionButton.interactable = false;
            priceText.color = normalText;

            if (skin == null || skin.Sprite == null)
            {
                priceText.text = "COMING SOON";
                buttonText.text = "UNAVAILABLE";
                return;
            }
            previewImage.sprite = skin.Sprite;
            previewImage.enabled = true;
            lockOverlay.SetActive(false);

            if (equipped)
            {
                priceText.text = "EQUIPPED";
                buttonText.text = "IN USE";
                priceText.color = equippedText;
                equippedBorder.enabled = true;
            }
            else if (owned)
            {
                priceText.text = "OWNED";
                buttonText.text = "EQUIP";
                actionButton.interactable = true;
            }
            else
            {
                priceText.text = skin.Price + " COINS";
                buttonText.text = "BUY";
                actionButton.interactable = coins >= skin.Price;
                if (coins < skin.Price)
                {
                    lockOverlay.SetActive(true);
                    priceText.color = insufficientText;
                }
            }
        }

        private void OnAction()
        {
            if (shop != null)
            {
                shop.Act(index);
            }
        }
    }
}
