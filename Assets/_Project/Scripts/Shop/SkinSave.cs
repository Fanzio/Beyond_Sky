// Gestisce acquisto, possesso e selezione persistente delle skin.
using System.Collections.Generic;
using UnityEngine;

namespace BeyondSky.Shop
{
    public class SkinSave : MonoBehaviour
    {
        [SerializeField] private SkinDatabase database;
        private readonly List<int> owned = new List<int>();
        private bool loaded;

        public void Reload()
        {
            owned.Clear();
            owned.Add(0);
            string[] values = PlayerPrefs.GetString("OwnedSkins", "0").Split(',');
            for (int i = 0; i < values.Length; i++)
            {
                int index;
                if (int.TryParse(values[i], out index) && index > 0 && !owned.Contains(index))
                {
                    owned.Add(index);
                }
            }
            loaded = true;
        }

        public bool IsOwned(int index)
        {
            if (!loaded)
            {
                Reload();
            }
            return owned.Contains(index);
        }

        public int GetEquipped()
        {
            int index = PlayerPrefs.GetInt("EquippedSkin", 0);
            if (database == null || !database.IsAvailable(index) || !IsOwned(index))
            {
                return 0;
            }
            return index;
        }

        public bool Buy(int index)
        {
            if (database == null || !database.IsAvailable(index) || IsOwned(index))
            {
                return false;
            }
            SkinEntry skin = database.GetSkin(index);
            int coins = PlayerPrefs.GetInt("Coins", 0);
            if (coins < skin.Price)
            {
                return false;
            }
            owned.Add(index);
            PlayerPrefs.SetInt("Coins", coins - skin.Price);
            SaveOwned();
            PlayerPrefs.Save();
            return true;
        }

        public bool Equip(int index)
        {
            if (database == null || !database.IsAvailable(index) || !IsOwned(index))
            {
                return false;
            }
            PlayerPrefs.SetInt("EquippedSkin", index);
            PlayerPrefs.Save();
            return true;
        }

        private void SaveOwned()
        {
            string value = "0";
            for (int i = 1; i < owned.Count; i++)
            {
                value = value + "," + owned[i];
            }
            PlayerPrefs.SetString("OwnedSkins", value);
        }
    }
}
