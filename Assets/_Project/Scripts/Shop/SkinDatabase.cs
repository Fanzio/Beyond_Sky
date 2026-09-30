// Conserva nomi, immagini e prezzi delle skin disponibili.
using UnityEngine;

namespace BeyondSky.Shop
{
    [System.Serializable]
    public class SkinEntry
    {
        [SerializeField] private string displayName;
        [SerializeField] private Sprite sprite;
        [SerializeField] private int price;

        public string DisplayName
        {
            get { return displayName; }
        }

        public Sprite Sprite
        {
            get { return sprite; }
        }

        public int Price
        {
            get { return Mathf.Max(0, price); }
        }
    }

    public class SkinDatabase : MonoBehaviour
    {
        [SerializeField] private SkinEntry[] skins;

        public int Count
        {
            get
            {
                if (skins == null)
                {
                    return 0;
                }
                return skins.Length;
            }
        }

        public SkinEntry GetSkin(int index)
        {
            if (index < 0 || index >= Count)
            {
                return null;
            }
            return skins[index];
        }

        public bool IsAvailable(int index)
        {
            SkinEntry entry = GetSkin(index);
            return entry != null && entry.Sprite != null;
        }
    }
}
