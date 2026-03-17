using UnityEngine;

public class OnTriggerSpawn : MonoBehaviour
{
    public GameObject EndroitOuSpawner;
    public GameObject ObjetACreer;
    public string TagDeObjetADetecter;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == TagDeObjetADetecter)
        {
           Instantiate(ObjetACreer, EndroitOuSpawner.transform.position, EndroitOuSpawner.transform.rotation);
        }
    }
}
