using UnityEngine;
using UnityEngine.InputSystem;

// Throwaway on screen readout for tuning. Uses OnGUI on purpose: no setup at all,
// and it gets deleted once the real HUD exists.
//
// The dodge cooldown belongs on the real HUD as well, not just here. A recharge the
// player cannot see turns every death into a coin flip.
public class PlaneDebugHud : MonoBehaviour
{
    [SerializeField] private PlaneController planeController;
    [SerializeField] private PlaneDodge planeDodge;
    [SerializeField] private CloseCallDetector closeCallDetector;
    [SerializeField] private PlaneShield planeShield;
    [SerializeField] private PlaneWeapon planeWeapon;
    [SerializeField] private SpeedManager speedManager;
    [SerializeField] private WorldScroller worldScroller;
    [SerializeField] private FloorManager floorManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private BossManager bossManager;

    [Header("Debug only")]
    [SerializeField] private float closeCallFlashTime = 0.7f;

    private GUIStyle style;
    private GUIStyle flashStyle;
    private float fps;

    private void Update()
    {
        TrackFps();

        if (Keyboard.current == null)
        {
            return;
        }
        if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            floorManager.NotifyBossDefeated();
        }
    }
    private void TrackFps()
    {
        if (Time.unscaledDeltaTime <= 0f)
        {
            return;
        }
        fps = Mathf.Lerp(fps, 1f / Time.unscaledDeltaTime, 0.08f);
    }
    private void OnGUI()
    {
        BuildStyles();

        if (gameManager.IsGameOver)
        {
            DrawGameOver();
            return;
        }

        string dodgeState = "COOLDOWN";

        if (planeDodge.IsRolling)
        {
            dodgeState = "ROLLING";
        }
        else if (planeDodge.IsReady)
        {
            dodgeState = "READY";
        }

        string shieldState = "no";

        if (planeShield.HasShield)
        {
            shieldState = "YES";
        }

        string text =
            "Score      " + scoreManager.Score + "   (best " + scoreManager.BestScore + ")\n" +
            "Floor      " + floorManager.CurrentFloorName +
            "   monete x" + floorManager.CoinMultiplier + "\n" +
            "Boss tra   " + floorManager.DistanceToBoss.ToString("F0") + "\n" +
            "Altitude   " + floorManager.DisplayedAltitude.ToString("F0") + " m\n" +
            "Speed      " + speedManager.CurrentSpeed.ToString("F1") + "\n" +
            "Distance   " + worldScroller.DistanceTravelled.ToString("F0") + "\n" +
            "V.Speed    " + planeController.VerticalVelocity.ToString("F1") + "\n" +
            "Dodge      " + dodgeState + "\n" +
            "Recharge   " + planeDodge.CooldownNormalized.ToString("F2") + "\n" +
            "FPS        " + fps.ToString("F0") + "\n" +
            "CloseCalls " + closeCallDetector.CloseCallCount +
            "   verso munizione " + closeCallDetector.ProgressToAmmo +
            "/" + closeCallDetector.CloseCallsPerAmmo + "\n" +
            "Shield     " + shieldState + "\n" +
            "Ammo       " + BuildAmmoBar() + "  (" + planeWeapon.Ammo + ")\n" +
            "Coins      " + scoreManager.RunCoins +
            "   (total " + scoreManager.TotalCoins + ")\n" +
            "\n[N] sali di piano";

        GUI.Label(new Rect(24f, 24f, 620f, 400f), text, style);

        DrawCloseCallFlash();
        DrawBossBar();
    }
    private void DrawBossBar()
    {
        if (bossManager.IsFightActive == false)
        {
            return;
        }

        float width = Screen.width * 0.5f;
        float x = Screen.width * 0.5f - width * 0.5f;
        float filled = width * bossManager.CurrentBoss.NormalizedTime;

        DrawRect(new Rect(x, 24f, width, 22f), new Color(0.15f, 0.15f, 0.18f));
        DrawRect(new Rect(x, 24f, filled, 22f), new Color(0.85f, 0.33f, 0.31f));

        GUI.Label(new Rect(x, 50f, width, 40f),
            "BOSS  " + bossManager.CurrentBoss.TimeRemaining.ToString("F1"), style);
    }
    private void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
    private string BuildAmmoBar()
    {
        string bar = "";
        int shown = Mathf.Min(planeWeapon.Ammo, 12);

        for (int i = 0; i < shown; i++)
        {
            bar = bar + "|";
        }
        return bar;
    }
    private void DrawCloseCallFlash()
    {
        if (closeCallDetector.TimeSinceLastCloseCall > closeCallFlashTime)
        {
            return;
        }

        string text = "CLOSE CALL";

        if (closeCallDetector.LastAwardedAmmo)
        {
            text = "CLOSE CALL  +1 MUNIZIONE";
        }
        GUI.Label(new Rect(Screen.width * 0.5f - 200f, 80f, 400f, 80f), text, flashStyle);
    }
    private void DrawGameOver()
    {
        string record = "";

        if (gameManager.IsNewRecord)
        {
            record = "   NUOVO RECORD";
        }
        string prompt = "";

        if (gameManager.CanRestart)
        {
            prompt = "\n\ntap to restart";
        }
        string text =
            "GAME OVER" + record + "\n" +
            "Score " + scoreManager.Score + "\n" +
            "Best  " + scoreManager.BestScore + prompt;

        GUI.Label(new Rect(24f, 24f, 620f, 260f), text, style);
    }
    private void BuildStyles()
    {
        if (style != null)
        {
            return;
        }

        int baseSize = Mathf.Max(16, Mathf.RoundToInt(Screen.height * 0.030f));

        style = new GUIStyle(GUI.skin.label);
        style.fontSize = baseSize;
        style.normal.textColor = Color.white;

        flashStyle = new GUIStyle(GUI.skin.label);
        flashStyle.fontSize = baseSize * 2;
        flashStyle.alignment = TextAnchor.MiddleCenter;
        flashStyle.normal.textColor = Color.yellow;
    }
}