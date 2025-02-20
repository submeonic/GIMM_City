using UnityEngine;

public class ActivateArcade : MonoBehaviour
{
    public GameObject Arcade;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Arcade.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
