using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] Character player;
    public BloodSplasher bloodSplasher;
    public EnemySpawner enemySpawner;
    [SerializeField] Text scoreText;
    [SerializeField] RectTransform losePanel;
    [SerializeField] Button toMainMenu;
    [SerializeField] Button restartLevel;
    [SerializeField] Text endScore;
    [SerializeField] Text maxScoreText;
    [SerializeField] float forceToScore = 0.01f;
    int score = 0;

    public Character Player => player;
    
    static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if(instance == null)
            {
                instance = FindObjectOfType<GameManager>();
                instance.Initialize();
            }

            return instance;
        }
    }

    void Awake()
    {
        if(instance == null)
        {
            instance = this;
            Initialize();
        }
    }

    void Initialize()
    {
        if (toMainMenu != null)
        {
            toMainMenu.onClick.AddListener(LevelManager.Instance.LoadMainMenu);
        }
        if (restartLevel != null)
        {
            restartLevel.onClick.AddListener(LevelManager.Instance.RestartLevel);
        }
    }

    

    public void CharacterDied(Character character, float smashForce)
    {
        if(character == player)
        {
            StartCoroutine(EndGame());
        }
        else
        {
            score += Mathf.Abs((int)(forceToScore * smashForce));
            this.scoreText.text = score.ToString();
        }
    }



    IEnumerator EndGame()
    {
        yield return new WaitForSeconds(3);
        Time.timeScale = 0;
        losePanel.gameObject.SetActive(true);
        endScore.text = this.scoreText.text;
        if (PlayerPrefs.HasKey("Max Score"))
        {
            int maxScore = PlayerPrefs.GetInt("Max Score");
            maxScore = Mathf.Max(maxScore, score);
            PlayerPrefs.SetInt("Max Score", maxScore);
            maxScoreText.text = maxScore.ToString();
        }
        else
        {
            maxScoreText.text = endScore.text;
            PlayerPrefs.SetInt("Max Score", score);
        }
    }

    private void OnDestroy()
    {
        if(this == instance)
        {
            instance = null;
        }
    }
}
