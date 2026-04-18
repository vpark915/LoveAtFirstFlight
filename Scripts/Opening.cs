using UnityEngine;
using UnityEngine.Playables;

public class Opening : MonoBehaviour
{
    public PlayableDirector director;

    void OnEnable()
    {
        if (director != null)
            director.stopped += OnTimelineFinished;
    }

    void OnDisable()
    {
        if (director != null)
            director.stopped -= OnTimelineFinished;
    }

    void OnTimelineFinished(PlayableDirector pd)
    {
        Debug.Log("Timeline finished!");
        // Your logic here:
        // e.g., enable a UI panel, switch camera, etc.
    }
}
