using UnityEngine;
using UnityEngine.UI; // Necesario para controlar la UI

public class BossHealth : MonoBehaviour
{
    [Header("Configuración de Vida")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Referencias UI")]
    public Slider healthBarSlider; // Arrastra aquí el Slider de la barra de vida
    public GameObject healthBarObject; // El objeto padre del Canvas (para ocultarlo al morir)

    [Header("Referencias Jefe")]
    public Animator animator;
    public BossAI bossAI;   // Referencia al cerebro para detener ataques al morir
    public Collider bossCollider; // Referencia al collider para apagarlo al morir
    public BossShootingAttack attack;


    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        // --- CAMBIO: OCULTAR AL INICIO ---
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(false); // La apagamos al empezar el juego
        }
    }

    // --- NUEVA FUNCIÓN ---
    public void ShowHealthBar()
    {
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(true); // La prendemos cuando el Trigger lo ordene
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (currentHealth <= 0) return; // Ya está muerto

        currentHealth -= damageAmount;

        // Evitamos números negativos
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthBar();

        // Chequeo de Muerte
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Opcional: Animación de recibir daño
            // if(animator) animator.SetTrigger("Hit");
        }
    }

    void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            // Convertimos a porcentaje (0 a 1)
            healthBarSlider.value = (float)currentHealth / maxHealth;
        }
    }

    public void HideHealthBar()
    {
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(false);
        }

        // Opcional: ¿Quieres que el jefe recupere vida si te vas?
        // ResetHealth(); 
    }

    void Die()
    {
        Debug.Log("¡Jefe Derrotado!");

        // 1. Apagar la IA (Cerebro)
        if (bossAI != null)
        {
            bossAI.StopAllCoroutines();
            bossAI.enabled = false;
        }

        // 2. Apagar Físicas
        //if (bossCollider != null) bossCollider.enabled = false;

        // --- AQUÍ ESTÁ EL ARREGLO ---
        if (attack != null)
        {
            // PRIMERO: Matamos la corrutina de los disparos fantasmas
            attack.StopAttack();
            // SEGUNDO: Apagamos el componente
            attack.enabled = false;
        }
        // ----------------------------

        // 3. Ocultar barra vida
        if (healthBarObject != null) healthBarObject.SetActive(false);

        // 4. Animación muerte
        if (animator != null) animator.SetTrigger("Die");

        // 5. Fade Out
        BossDeathFade fader = GetComponent<BossDeathFade>();
        if (fader != null)
        {
            fader.StartFadeOut();
        }
        else
        {
            Destroy(gameObject, 1f);
        }
    }
}