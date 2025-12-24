using UnityEngine;

public class flameThrowerDamage : MonoBehaviour
{
    [Range(1, 10)]
    [SerializeField] private float flameDamage;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        EnemyHealth enemy = collision.GetComponent<EnemyHealth>();

        if (enemy != null)
        {
            enemy.TakeDamage(flameDamage * Time.deltaTime);  // deal damage
        }

    }
}
