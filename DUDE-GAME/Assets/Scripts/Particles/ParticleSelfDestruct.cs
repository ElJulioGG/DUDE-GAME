using System.Collections;
using UnityEngine;

// Va en un sistema de particulas ya SUELTO en la escena (lo anaden por codigo
// PowerUpTrigger y DetachParticlePlayer): espera lingerDuration, luego a que
// mueran todas las particulas (incluidos sub-emisores hijos) y destruye el
// objeto. Existe porque el dueno original se desactiva o destruye, y la espera
// tiene que vivir en el objeto suelto.
[RequireComponent(typeof(ParticleSystem))]
public class ParticleSelfDestruct : MonoBehaviour
{
    [SerializeField] private float lingerDuration = 0.5f;

    private IEnumerator Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        yield return new WaitForSeconds(lingerDuration);
        yield return new WaitWhile(() => ps != null && ps.IsAlive(true));
        Destroy(gameObject);
    }
}
