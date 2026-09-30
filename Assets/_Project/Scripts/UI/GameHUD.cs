// Aggiorna gli indicatori della partita quando cambiano i valori visualizzati.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BeyondSky.Boss;
using BeyondSky.Player;
using BeyondSky.Systems;
using BeyondSky.World;

namespace BeyondSky.UI
{
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

        private int lastScore = -1;
        private int lastDistance = -1;
        private int lastFloor = -1;
        private int lastCoins = -1;
        private int lastBossDistance = -1;
        private int lastAmmo = -1;
        private int lastProgress = -1;
        private int lastRequired = -1;
        private bool lastAwardedAmmo;

        private void OnEnable()
        {
            lastScore = -1;
            lastDistance = -1;
            lastFloor = -1;
            lastCoins = -1;
            lastBossDistance = -1;
            lastAmmo = -1;
            lastProgress = -1;
            lastRequired = -1;
            lastAwardedAmmo = false;
            closeCallText.SetText("CLOSE CALL");
        }

        private void LateUpdate()
        {
            if (lastScore != score.Score)
            {
                lastScore = score.Score;
                scoreText.SetText("{0}", lastScore);
            }
            int distance = Mathf.RoundToInt(world.DistanceTravelled);
            if (lastDistance != distance)
            {
                lastDistance = distance;
                distanceText.SetText("{0} m", distance);
            }
            if (lastFloor != floor.CurrentFloorIndex)
            {
                lastFloor = floor.CurrentFloorIndex;
                string format = "City x{0}";
                if (lastFloor == 1)
                {
                    format = "Sky x{0}";
                }
                if (lastFloor == 2)
                {
                    format = "Space x{0}";
                }
                floorText.SetText(format, floor.CoinMultiplier);
            }
            if (lastCoins != score.RunCoins)
            {
                lastCoins = score.RunCoins;
                coinText.SetText("{0}", lastCoins);
            }
            int bossDistance = Mathf.RoundToInt(floor.DistanceToBoss);
            if (lastBossDistance != bossDistance)
            {
                lastBossDistance = bossDistance;
                bossDistanceText.SetText("Boss in {0} m", bossDistance);
            }
            SetVisible(bossDistanceText.gameObject, !boss.IsArenaActive);
            float dodgeAmount = dodge.CooldownNormalized;
            if (dodge.IsRolling)
            {
                dodgeAmount = 0f;
            }
            if (dodgeFill.fillAmount != dodgeAmount)
            {
                dodgeFill.fillAmount = dodgeAmount;
            }
            SetVisible(shieldIcon, shield.HasShield);
            if (lastAmmo != weapon.Ammo)
            {
                lastAmmo = weapon.Ammo;
                for (int i = 0; i < ammoIcons.Length; i++)
                {
                    SetVisible(ammoIcons[i].gameObject, i < lastAmmo);
                }
                SetVisible(ammoOverflowText.gameObject, lastAmmo > ammoIcons.Length);
                if (lastAmmo > ammoIcons.Length)
                {
                    ammoOverflowText.SetText("+{0}", lastAmmo - ammoIcons.Length);
                }
            }
            if (lastProgress != closeCalls.ProgressToAmmo || lastRequired != closeCalls.CloseCallsPerAmmo)
            {
                lastProgress = closeCalls.ProgressToAmmo;
                lastRequired = closeCalls.CloseCallsPerAmmo;
                ammoProgressText.SetText("Close calls: {0}/{1}", lastProgress, lastRequired);
            }
            SetVisible(closeCallText.gameObject, closeCalls.TimeSinceLastCloseCall <= 0.5f);
            if (lastAwardedAmmo != closeCalls.LastAwardedAmmo)
            {
                lastAwardedAmmo = closeCalls.LastAwardedAmmo;
                string message = "CLOSE CALL";
                if (lastAwardedAmmo)
                {
                    message = "CLOSE CALL  +1 AMMO";
                }
                closeCallText.SetText(message);
            }
            bool fighting = boss.IsFightActive && boss.CurrentBoss != null;
            SetVisible(bossBar, fighting);
            if (fighting && bossFill.fillAmount != boss.CurrentBoss.NormalizedTime)
            {
                bossFill.fillAmount = boss.CurrentBoss.NormalizedTime;
            }
        }

        private void SetVisible(GameObject target, bool visible)
        {
            if (target.activeSelf != visible)
            {
                target.SetActive(visible);
            }
        }
    }
}
