using UnityEngine;

public class pickupitem : MonoBehaviour
{
    public enum PickupType { Health, Flow } 

    [SerializeField] PickupType type;
    [SerializeField] int amount = 0; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerController player = other.GetComponent<playerController>();
            if (player != null)
            {
                if (type == PickupType.Health)
                {
                    player.HP += amount; 
                }
                else if (type == PickupType.Flow)
                {
                    player.addFlow(amount); 
                }
            }
            Destroy(gameObject); 
        }
    }

}
