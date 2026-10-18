using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Player hull bar and sub-light charge bar. Built by Tools > Enemy Spawning > Steps > 8 Player HUD,
// after that restyle freely in the scene. Finds the player itself if nothing is assigned.
public class PlayerHUD : MonoBehaviour
{
    [Header("Player (found automatically)")]
    [SerializeField] private UnitManager player;
    [SerializeField] private SubLightDrive drive;

    [Header("Hull")]
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Gradient healthColors = DefaultHealthColors();
    [Tooltip("How fast the bar catches up with the real value (fraction per second).")]
    [SerializeField] private float healthBarSpeed = 2f;

    [Header("Sub-light")]
    [SerializeField] private Image subLightFill;
    [SerializeField] private TMP_Text subLightText;
    [SerializeField] private CanvasGroup subLightGroup;
    [SerializeField] private string keyHint = "SHIFT";
    [SerializeField] private Color chargeColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] private Color cruiseColor = Color.white;
    [Tooltip("Flash colour when a hit knocks charge off.")]
    [SerializeField] private Color hitColor = new Color(1f, 0.3f, 0.25f);
    [Tooltip("Bar opacity while the drive is idle.")]
    [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.5f;
    [SerializeField] private float dropOutMessageTime = 2.5f;

    private float shownHealth = 1f;
    private float lastCharge;
    private float hitFlashUntil;

    private void Update()
    {
        if (player == null) FindPlayer();

        UpdateHealth();
        UpdateSubLight();
    }

    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerManager>();
        if (player != null && drive == null) drive = player.GetComponentInChildren<SubLightDrive>();
    }

    private void UpdateHealth()
    {
        if (healthFill == null) return;

        float health = player != null && !player.IsDead ? Mathf.Clamp01(player.HealthNormalized) : 0f;
        shownHealth = Mathf.MoveTowards(shownHealth, health, healthBarSpeed * Time.deltaTime);

        healthFill.fillAmount = shownHealth;
        healthFill.color = healthColors.Evaluate(shownHealth);

        if (healthText != null && player != null)
        {
            healthText.text = $"HULL  {Mathf.Max(0, player.Health)} / {player.MaxHealth}";
        }
    }

    private void UpdateSubLight()
    {
        if (subLightFill == null) return;

        if (drive == null)
        {
            if (subLightGroup != null) subLightGroup.alpha = 0f;
            return;
        }

        float alpha = 1f;
        string label;

        switch (drive.State)
        {
            case SubLightState.Charging:
                {
                    float charge = drive.ChargeProgress;
                    if (charge < lastCharge - 0.001f) hitFlashUntil = Time.time + 0.25f;
                    lastCharge = charge;

                    subLightFill.fillAmount = charge;
                    subLightFill.color = Time.time < hitFlashUntil ? hitColor : chargeColor;
                    label = $"CHARGING {Mathf.FloorToInt(charge * 100f)}%   [{keyHint}] ABORT";
                    break;
                }

            case SubLightState.Cruising:
                subLightFill.fillAmount = 1f;
                subLightFill.color = cruiseColor;
                label = $"SUB-LIGHT   [{keyHint}] DROP OUT";
                break;

            case SubLightState.Exiting:
                subLightFill.fillAmount = 1f;
                subLightFill.color = cruiseColor;
                label = "DROPPING OUT";
                break;

            default:
                lastCharge = 0f;
                subLightFill.fillAmount = 0f;
                alpha = idleAlpha;
                bool recentDropOut = Time.time - drive.LastDropOutTime < dropOutMessageTime && drive.LastDropOutReason != "Manual";
                label = recentDropOut
                    ? $"DROPPED OUT: {drive.LastDropOutReason.ToUpperInvariant()}"
                    : $"SUB-LIGHT READY   [{keyHint}]";
                if (recentDropOut) alpha = 1f;
                break;
        }

        if (subLightText != null) subLightText.text = label;
        if (subLightGroup != null) subLightGroup.alpha = alpha;
    }

    private static Gradient DefaultHealthColors()
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.9f, 0.2f, 0.15f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return gradient;
    }
}
