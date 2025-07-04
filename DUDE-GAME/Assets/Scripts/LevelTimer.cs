using UnityEngine;
using TMPro;
using DG.Tweening;

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

            // Shake only if timeLeft < shakeStartTime
            if (timeLeft <= shakeStartTime)
            {
                float shakeT = 1f - (timeLeft / shakeStartTime);
                float strength = Mathf.Lerp(minStrength, maxStrength, shakeT);
                int vibrato = Mathf.RoundToInt(Mathf.Lerp(minVibrato, maxVibrato, shakeT));

                timerText.rectTransform.anchoredPosition = originalPosition;

                shakeSequence.Kill();
                shakeSequence = DOTween.Sequence().SetLoops(-1, LoopType.Restart);
                shakeSequence.Append(timerText.rectTransform
                    .DOShakeAnchorPos(shakeDuration, strength, vibrato, randomness, snapBack, fadeOut)
                    .SetEase(Ease.Linear));
            }
            else
            {
                shakeSequence?.Kill();
                timerText.rectTransform.anchoredPosition = originalPosition;
            }
        }
        else
        {
            
            StartCoroutine(gameController.HandleDraw());
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
        timerText.rectTransform.anchoredPosition = originalPosition;
        timerText.color = endColor;
    }

    public void ResetTimer()
    {
        timeLeft = startTime;
        isRunning = false;

        timerText.text = Mathf.CeilToInt(timeLeft).ToString();
        timerText.color = startColor;
        timerText.rectTransform.anchoredPosition = originalPosition;

        shakeSequence?.Kill();
    }
}
