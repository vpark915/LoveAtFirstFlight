using UnityEngine;
using UnityEngine.Playables;

public class EmptyChair : MonoBehaviour
{
    public Camera playerCamera;
    public Outline outline;
    public PlayableDirector bossPreDialogue;
    public GameObject playerRef;
    public GameObject cushionRef;
    public float interactionDistance = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Make sure outline starts disabled
        if (outline != null)
            outline.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        // Check distance between chair and player
        float distance = Vector3.Distance(cushionRef.transform.position, playerRef.transform.position);
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        
        bool chairHit = false;
        
        // Check if this transform is in the list of hit objects
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform)
            {
                chairHit = true;
                break;
            }
        }
        
        if (chairHit && distance <= interactionDistance)
        {
            outline.enabled = true;
            if (Input.GetMouseButtonDown(0))
            {
                Destroy(GetComponent<Outline>());
                bossPreDialogue.Play();
                this.enabled = false;
            }
        }
        else
        {
            outline.enabled = false;
        }
    }
}