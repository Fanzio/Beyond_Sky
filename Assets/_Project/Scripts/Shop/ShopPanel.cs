// Popola il negozio e aggiorna le schede dopo gli acquisti o la selezione.
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using BeyondSky.UI;

namespace BeyondSky.Shop
{
    public class ShopPanel : MonoBehaviour
    {
        [SerializeField] private SkinDatabase database;
        [SerializeField] private SkinSave skinSave;
        [SerializeField] private UIFade fade;
        [SerializeField] private Transform skinGrid;
        [SerializeField] private SkinCard cardPrefab;
        [SerializeField] private TMP_Text coinsText;
        private readonly List<SkinCard> cards = new List<SkinCard>();

        public void Open()
        {
            fade.Show();
        }

        public void Close()
        {
            fade.Hide();
        }

        public void HideImmediately()
        {
            fade.HideImmediately();
        }

        private void OnEnable()
        {
            skinSave.Reload();
            for (int i = 0; i < database.Count; i++)
            {
                SkinCard card = Instantiate(cardPrefab, skinGrid);
                card.Initialize(this, i);
                cards.Add(card);
            }
            Refresh();
        }

        private void OnDisable()
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null)
                {
                    Destroy(cards[i].gameObject);
                }
            }
            cards.Clear();
        }

        public void Act(int index)
        {
            if (!fade.IsVisible)
            {
                return;
            }
            if (skinSave.IsOwned(index))
            {
                skinSave.Equip(index);
            }
            else
            {
                skinSave.Buy(index);
            }
            Refresh();
        }

        private void Refresh()
        {
            int coins = PlayerPrefs.GetInt("Coins", 0);
            coinsText.text = coins.ToString();
            int equipped = skinSave.GetEquipped();
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].Refresh(database.GetSkin(i), skinSave.IsOwned(i), equipped == i, coins);
            }
        }
    }
}
