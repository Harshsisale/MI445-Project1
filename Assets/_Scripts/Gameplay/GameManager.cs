using UnityEngine;

public class GameManager : MonoBehaviour
{
    public float gameTimeMinutes;
    private float  gameTimeSeconds;
    public GameObject tornado;

    void Start()
    {
        gameTimeSeconds = gameTimeMinutes * 60;
    }
    void Update()
    {
        gameTimeSeconds -= Time.deltaTime;
        //speed run timer
       // gameTimeText.text = gameTimeSeconds.ToString("F1");
        if (gameTimeSeconds <= 0)
        {
            TornadoChase();
        }
    }

    void TornadoChase()
    {
        tornado.SetActive(true);
    }

}
