using UnityEngine;

public class ExplosiveItem : MonoBehaviour
{
    [SerializeField] private float explosiveDuration = 5f;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material explosiveMaterial;

    private bool isExplosive = false;
    private float timer = 0f;
    private Renderer[] renderers;
    private Color[] originalColors;

    [SerializeField] private MultiParticlePool particlePool;

    public bool IsExplosive => isExplosive;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
        }
    }

    private void Update()
    {
        if (isExplosive)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                DeactivateExplosion();
            }
        }
    }

    public void ActivateExplosion()
    {
        if (isExplosive) return;

        isExplosive = true;
        timer = explosiveDuration;

        foreach (var r in renderers)
            r.material = explosiveMaterial;
    }

    private void DeactivateExplosion()
    {
        isExplosive = false;
        foreach (var r in renderers)
            r.material = normalMaterial;
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!isExplosive) return;

        if (other.CompareTag("Player"))
        {
            particlePool?.PlayParticle("coinExplosion", transform.position, Quaternion.identity);
            other.GetComponent<PlayerDeathHandler>()?.Die();
            Destroy(gameObject);
        }
    }

    // Este método será llamado por el trigger hijo (ver más abajo)
    public void OnSpiderRangeEntered()
    {
        if (!isExplosive)
            ActivateExplosion();
    }
}
