using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(PlayerAnimationController))]
public class PlayerAnimationEvents : MonoBehaviour
{
    [System.Serializable]
    public class AnimationEffects
    {
        [Header("Visual Feedback")]
        public ParticleSystem walkDustParticles;
        public ParticleSystem jumpParticles;
        public ParticleSystem landingParticles;
        
        [Header("Screen Shake")]
        public float landingShakeIntensity = 0.2f;
        public float landingShakeDuration = 0.1f;
    }

    [Header("Effects Configuration")]
    [SerializeField] private AnimationEffects effects;
    
    [Header("Events")]
    public UnityEvent onFootstep;
    public UnityEvent onJump;
    public UnityEvent onLanding;
    
    private PlayerMovement playerMovement;
    private PlayerAnimationController animationController;
    
    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animationController = GetComponent<PlayerAnimationController>();
    }

    // Animation Events - Called from animation frames
    public void OnFootstep()
    {
        if (effects.walkDustParticles != null)
        {
            effects.walkDustParticles.Play();
        }
        onFootstep?.Invoke();
    }

    public void OnJump()
    {
        if (effects.jumpParticles != null)
        {
            effects.jumpParticles.Play();
        }
        onJump?.Invoke();
    }

    public void OnLanding()
    {
        if (effects.landingParticles != null)
        {
            effects.landingParticles.Play();
        }

        // Optional camera shake
        if (effects.landingShakeIntensity > 0)
        {
            CameraShake();
        }

        onLanding?.Invoke();
    }

    private void CameraShake()
    {
        if (Camera.main != null)
        {
            StartCoroutine(CameraShakeCoroutine());
        }
    }

    private System.Collections.IEnumerator CameraShakeCoroutine()
    {
        Vector3 originalPosition = Camera.main.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < effects.landingShakeDuration)
        {
            float x = Random.Range(-1f, 1f) * effects.landingShakeIntensity;
            float y = Random.Range(-1f, 1f) * effects.landingShakeIntensity;

            Camera.main.transform.localPosition = originalPosition + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.localPosition = originalPosition;
    }
}