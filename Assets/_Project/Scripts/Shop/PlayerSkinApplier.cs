// Applica al velivolo la skin selezionata nel negozio.
using UnityEngine;

namespace BeyondSky.Shop
{
    [DefaultExecutionOrder(-200)]
    public class PlayerSkinApplier : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private SkinDatabase database;
        [SerializeField] private SkinSave skinSave;

        private void Awake()
        {
            if (visual == null || database == null || skinSave == null)
            {
                return;
            }
            SkinEntry entry = database.GetSkin(skinSave.GetEquipped());
            if (entry != null && entry.Sprite != null)
            {
                visual.sprite = entry.Sprite;
            }
        }
    }
}
