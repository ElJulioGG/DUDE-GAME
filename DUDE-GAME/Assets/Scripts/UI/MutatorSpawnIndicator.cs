using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Obj_Tweens))]
public class MutatorSpawnIndicator : MonoBehaviour
{
    [Header("Pop Settings")]
    public bool playPop = true;
    public float popMultiplier = 1.5f;
    public float popDuration = 1f;
    [Range(0f, 1f)] public float popRatio = 0.5f;
    public Ease popEase = Ease.OutBack;
    public Ease settleEase = Ease.InOutSine;

    [Header("Rotate Settings")]
    public bool playRotate = true;
    public Vector3 rotateAmount = new Vector3(0, 360, 0);
    public float rotateDuration = 2f;
    public Ease rotateEase = Ease.Linear;
    public bool fullRotation = true;
    public float spawnSfxVolume = 0.7f;

    private Obj_Tweens tweens;

    void Start()
    {
        AudioManager.Instance.PlaySound(FMODEvents.Instance.SpawnAreaMutatorAlert, transform.position);
        tweens = GetComponent<Obj_Tweens>();

        if (playPop)
        {
            tweens.PlayPop(
                transform.localScale,
                transform.localScale * popMultiplier,
                transform.localScale,
                popDuration,
                popRatio,
                popEase,
                settleEase
            );
        }

        if (playRotate)
        {
            tweens.PlayRotate(
                transform.eulerAngles,
                transform.eulerAngles + rotateAmount,
                rotateDuration,
                rotateEase,
                fullRotation
            );
        }
        Destroy(gameObject, 1.5f);

    }
    //private void OnEnable()
    //{
    //    SoundFXManager.instance.PlaySoundByName("SpawnMutator", gameObject.transform,1f,1f,false);
        
    //    tweens = GetComponent<Obj_Tweens>();

    //    if (playPop)
    //    {
    //        tweens.PlayPop(
    //            transform.localScale,
    //            transform.localScale * popMultiplier,
    //            transform.localScale,
    //            popDuration,
    //            popRatio,
    //            popEase,
    //            settleEase
    //        );
    //    }

    //    if (playRotate)
    //    {
    //        tweens.PlayRotate(
    //            transform.eulerAngles,
    //            transform.eulerAngles + rotateAmount,
    //            rotateDuration,
    //            rotateEase,
    //            fullRotation
    //        );
    //    }

    //}
}
