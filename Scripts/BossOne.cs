using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class BossOne : MonoBehaviour
{
    public GameObject[] butterflies = new GameObject[5];
    public PlayableDirector DialogueTwo;

    private bool hasPlayedTimeline = false;

    void Update()
    {
        if (!hasPlayedTimeline && CheckAllDead())
        {
            hasPlayedTimeline = true;
            DialogueTwo.stopped += OnTimelineFinished;
            DialogueTwo.Play();
        }
    }

    bool CheckAllDead()
    {
        for (int i = 0; i < butterflies.Length; i++)
        {
            if (!butterflies[i].GetComponent<ButterflyBehavior>().isDead)
            {
                return false;
            }
        }
        return true;
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        SceneManager.LoadScene("CafeScene");
    }
}