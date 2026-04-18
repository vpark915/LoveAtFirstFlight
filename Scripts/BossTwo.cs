using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

public class BossTwo : MonoBehaviour
{
    public GameObject coffeeBoss;
    public PlayableDirector DialogueThree;

    private bool hasPlayedTimeline = false;
    public float delayBeforeTimeline = 2f; // seconds to wait after death

    void Update()
    {
        if (!hasPlayedTimeline && coffeeBoss.GetComponent<CoffeeMonster>().isDead)
        {
            hasPlayedTimeline = true;
            StartCoroutine(PlayTimelineWithDelay());
        }
    }

    IEnumerator PlayTimelineWithDelay()
    {
        yield return new WaitForSeconds(delayBeforeTimeline);
        DialogueThree.stopped += OnTimelineFinished;
        DialogueThree.Play();
    }

    void OnTimelineFinished(PlayableDirector director)
    {
        SceneManager.LoadScene("ShoppingScene");
    }
}