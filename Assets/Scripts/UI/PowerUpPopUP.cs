using UnityEngine;
using DG.Tweening;
using UnityEngine.Rendering;
public class PowerUpPopUP : MonoBehaviour
{
    private UI_Tweens ui_Tweens;
    [SerializeField]private float scaleSize = 1.5f;
    [SerializeField] private float scaleDuration = 2f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {

        ui_Tweens = GetComponent<UI_Tweens>();
    }

    
    private void OnEnable()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        cg.alpha = 1;
        transform.localScale  = Vector3.one;
        transform.DOScale(scaleSize, scaleDuration).SetEase(Ease.OutExpo);
        Invoke("FadeOut", scaleDuration /2f);
    }
    private void FadeOut()
    {
        ui_Tweens.PlayFade(1/*second*/, 0, scaleDuration, Ease.Linear, () => gameObject.SetActive(false));
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
