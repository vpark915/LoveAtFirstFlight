using UnityEngine;
using UnityEngine.Playables;

public class GoodbyePartTwo : MonoBehaviour
{
    public PlayableDirector partthree;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            partthree.Play();
        }
    }
}
