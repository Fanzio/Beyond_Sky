using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private ScoreManager score;
    [SerializeField] private FloorManager floor;
    [SerializeField] private WorldScroller world;
    [SerializeField] private BossManager boss;
    [SerializeField] private PlaneDodge dodge;
    [SerializeField] private PlaneWeapon weapon;
    [SerializeField] private PlaneShield shield;
    [SerializeField] private CloseCallDetector closeCalls;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text floorText;
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text bossDistanceText;
    [SerializeField] private TMP_Text ammoProgressText;
    [SerializeField] private TMP_Text ammoOverflowText;
    [SerializeField] private TMP_Text closeCallText;
    [SerializeField] private Image dodgeFill;
    [SerializeField] private Image bossFill;
    [SerializeField] private GameObject bossBar;
    [SerializeField] private GameObject shieldIcon;
    [SerializeField] private Image[] ammoIcons;

    private void LateUpdate()
    {
        scoreText.text = score.Score.ToString();
        distanceText.text = world.DistanceTravelled.ToString("F0") + " m";
        string floorName = "City";
        if (floor.CurrentFloorIndex == 1)
        {
            floorName = "Sky";
        }
        if (floor.CurrentFloorIndex == 2)
        {
            floorName = "Space";
        }
        floorText.text = floorName + " x" + floor.CoinMultiplier;
        coinText.text = score.RunCoins.ToString();
        bossDistanceText.text = "Boss in " + floor.DistanceToBoss.ToString("F0") + " m";
        bossDistanceText.gameObject.SetActive(!boss.IsArenaActive);
        dodgeFill.fillAmount = dodge.CooldownNormalized;
        if (dodge.IsRolling)
        {
            dodgeFill.fillAmount = 0f;
        }
        shieldIcon.SetActive(shield.HasShield);
        for (int i = 0; i < ammoIcons.Length; i++)
        {
            ammoIcons[i].gameObject.SetActive(i < weapon.Ammo);
        }
        ammoProgressText.text = "Close calls: " + closeCalls.ProgressToAmmo +
            "/" + closeCalls.CloseCallsPerAmmo;
        ammoOverflowText.gameObject.SetActive(weapon.Ammo > ammoIcons.Length);
        if (weapon.Ammo > ammoIcons.Length)
        {
            ammoOverflowText.text = "+" + (weapon.Ammo - ammoIcons.Length);
        }
        closeCallText.gameObject.SetActive(closeCalls.TimeSinceLastCloseCall <= 0.5f);
        closeCallText.text = "CLOSE CALL";
        if (closeCalls.LastAwardedAmmo)
        {
            closeCallText.text = "CLOSE CALL  +1 AMMO";
        }
        bool fighting = boss.IsFightActive && boss.CurrentBoss != null;
        bossBar.SetActive(fighting);
        if (fighting)
        {
            bossFill.fillAmount = boss.CurrentBoss.NormalizedTime;
        }
    }
}
