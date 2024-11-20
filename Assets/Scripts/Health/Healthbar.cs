using UnityEngine;

public class Healthbar : MonoBehaviour
{
    [SerializeField] private Health playerHealth;
    [SerializeField] private UnityEngine.UI.Image totalhealthBar;
    [SerializeField] private UnityEngine.UI.Image currenthealthBar;
    private void Start()
    {
        totalhealthBar.fillAmount = playerHealth.currentHealth / 3;

    }


    private void Update()
    {
        currenthealthBar.fillAmount = playerHealth.currentHealth / 3;



    }
}
