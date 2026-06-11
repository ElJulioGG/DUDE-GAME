using UnityEngine;
using TMPro;
using DG.Tweening;
using FishNet;

public class LevelTimer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameController gameController;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Timer Settings")]
    [SerializeField] private float startTime = 99f;
    public float timeLeft;
    private bool isRunning = false;
    private int lastSecondPlayed = -1;

    // Online: the server broadcasts its remaining time every few seconds so client
    // timers can't drift; clients snap when the difference exceeds a small threshold.
    private const float NetSyncInterval  = 3f;
    private const float NetSnapThreshold = 0.5f;
    private float _netSyncTimer;


    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float minStrength = 0f;
    [SerializeField] private float maxStrength = 40f;
    [SerializeField] private int minVibrato = 10;
    [SerializeField] private int maxVibrato = 50;
    [SerializeField] private float randomness = 90f;
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private bool snapBack = false;
    [SerializeField] private float shakeStartTime = 30f; // ⏳ Starts shaking when timer < 30s

    [Header("Color Transition")]
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = Color.red;

    private Sequence shakeSequence;
    private Vector2 originalPosition;
    private int _lastShakeSecond = -1;

    void Start()
    {
        originalPosition = timerText.rectTransform.anchoredPosition;

        ResetTimer();
        StartTimer(); // Optional: remove if you want to trigger manually
    }

    void Update()
    {
        if (!isRunning) return;

        if (timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;

            // Server: keep client timers locked to ours.
            if (GameSession.IsOnline && InstanceFinder.IsServerStarted)
            {
                _netSyncTimer -= Time.deltaTime;
                if (_netSyncTimer <= 0f)
                {
                    _netSyncTimer = NetSyncInterval;
                    NetworkGameManager.Instance?.ServerBroadcastRoundTimer(timeLeft);
                }
            }

            int currentSecond = Mathf.CeilToInt(timeLeft);

            if (currentSecond != lastSecondPlayed)
            {
                lastSecondPlayed = currentSecond;

                if (currentSecond <= 15 && currentSecond > 0)
                {
                    SoundFXManager.instance.PlaySoundByName("playerCircle", transform, 0.5f, 1f, false);
                }
            }

            timeLeft = Mathf.Max(0f, timeLeft);

            // Update text
            timerText.text = Mathf.CeilToInt(timeLeft).ToString();

            // Color transition
            float t = 1f - (timeLeft / startTime);
            Color yellow = Color.yellow;
            Color orange = new Color(1f, 0.5f, 0f); // RGB orange

            if (t < 0.33f)
            {
                timerText.color = Color.Lerp(startColor, yellow, t / 0.33f);
            }
            else if (t < 0.66f)
            {
                timerText.color = Color.Lerp(yellow, orange, (t - 0.33f) / 0.33f);
            }
            else
            {
                timerText.color = Color.Lerp(orange, endColor, (t - 0.66f) / 0.34f);
            }

            // Shake only if timeLeft < shakeStartTime. The shake intensity only
            // changes meaningfully per second — rebuilding the looping tween every
            // frame (the old behavior) churned allocations for the whole final 30s.
            if (timeLeft <= shakeStartTime)
            {
                if (currentSecond != _lastShakeSecond)
                {
                    _lastShakeSecond = currentSecond;

                    float shakeT = 1f - (timeLeft / shakeStartTime);
                    float strength = Mathf.Lerp(minStrength, maxStrength, shakeT);
                    int vibrato = Mathf.RoundToInt(Mathf.Lerp(minVibrato, maxVibrato, shakeT));

                    timerText.rectTransform.anchoredPosition = originalPosition;

                    shakeSequence?.Kill();
                    shakeSequence = DOTween.Sequence().SetLoops(-1, LoopType.Restart);
                    shakeSequence.Append(timerText.rectTransform
                        .DOShakeAnchorPos(shakeDuration, strength, vibrato, randomness, snapBack, fadeOut)
                        .SetEase(Ease.Linear));
                }
            }
            else if (shakeSequence != null)
            {
                shakeSequence.Kill();
                shakeSequence = null;
                timerText.rectTransform.anchoredPosition = originalPosition;
            }
        }
        else
        {
            // Only the server (or offline play) may end the round on timeout.
            // Clients ending rounds from their own local timer was a major desync source.
            if (!GameSession.IsOnline || InstanceFinder.IsServerStarted)
            {
                if (!gameController.matchEnded)
                    StartCoroutine(gameController.HandleDraw());
            }
            StopTimer();
        }
       
    }

    public void StartTimer()
    {
        if (isRunning) return;

        isRunning = true;
        shakeSequence?.Kill();
        timerText.rectTransform.anchoredPosition = originalPosition;
    }

    public void StopTimer()
    {
        isRunning = false;
        shakeSequence?.Kill();
        shakeSequence = null;
        timerText.rectTransform.anchoredPosition = originalPosition;
        timerText.color = endColor;
    }

    // Client-side: snap to the server's remaining time when we've drifted.
    public void NetworkSyncTime(float serverTimeLeft)
    {
        if (!GameSession.IsOnline || InstanceFinder.IsServerStarted) return;

        if (!isRunning && serverTimeLeft > 0f && !gameController.matchEnded)
            isRunning = true;

        if (Mathf.Abs(timeLeft - serverTimeLeft) > NetSnapThreshold)
            timeLeft = serverTimeLeft;
    }

    public void ResetTimer()
    {
        timeLeft = startTime;
        isRunning = false;
        _lastShakeSecond = -1;

        timerText.text = Mathf.CeilToInt(timeLeft).ToString();
        timerText.color = startColor;
        timerText.rectTransform.anchoredPosition = originalPosition;

        shakeSequence?.Kill();
        shakeSequence = null;
    }
}
