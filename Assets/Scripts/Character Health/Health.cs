// PlayerHealth.cs
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }
    private Animator anim;
    private bool dead;

    private void Awake()
    {
        currentHealth = startingHealth;
        anim = GetComponent<Animator>();
    }

    public void TakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);
        //Debug.Log("Player Health: " + currentHealth);

        if (currentHealth > 0)
        {
            //player hurt

            anim.SetTrigger("Hurt");

        }

        else
        { //player is dead

            if (!dead)
            {

                anim.SetTrigger("die");
                GetComponent<PlayerMovement>().enabled = false;
                dead = true;


            }
        }



    }
}