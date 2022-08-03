﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour 
{
    [Header("Main")]
    public static GameManager Instance;
    public PlayerStats DefaultTestPlayer; //The player that is spawned in the game; Will be removed in the future.//

    [Header("Game State")]
    public string[] SceneArray;
    private int CurrentSceneIndex;
    public enum GameState
    {
        InMenu, InGame, InGamePaused
    }
    [SerializeField]
    public GameState GameStatus;
    //Scene keywords are used so the program knows what to do with a scene once it is loaded.//
    public string SceneKeywordInGame = "Game"; //A scene with this word in it's name is automatically loaded as an InGame scene.//
    public string SceneKeywordInMenu = "Menu"; //A scene with this word in it's name is automatically loaded as a Menu scene.//
    public string WaveStartText; //Text that shows when the game starts//
    public bool GameFinished;
    public string GameOverText; //Text that shows when the game is over//
    public string SecondaryGameOverText; //Text that shows when the game is over//

    [Header("Game Time")]
    public bool CountTime;
	private float TotalAppTime;
	private float InitialTime;
    private float InGameTime;
    private float InGameInitialTime;
    public float PlayerJoinDelay = 1f;
    public float GameOverDelay = 5f;

    [Header("Weather")]
    public GameObject Sun;
    public float SunSpeedDivisor = 4f; //InGameTime divided by this number is the speed of the Sun's rotation//

    [Header ("Main References")]
    [Space(50)]
    public UIManager UI;
    public PlayerManager PlayerManagerScript;
    public ControlsManager ControlsManagerScript;
    public WaveSpawner WaveSpawnerScript;
    public LootSpawner LootSpawnerScript;
    public ItemCollection ItemCollectionScript;
    public ZombieTextureCollection ZombieTextureCollectionScript;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("More than one " + this.name + " loaded;");
            return;
        }
    }

    void Start()
    {
        ItemCollectionScript.SetID();
        UI = GetComponent<UIManager>();
        PlayerManagerScript = GetComponent<PlayerManager>();
        ControlsManagerScript = GetComponent<ControlsManager>();
        WaveSpawnerScript = GetComponent<WaveSpawner>();
        LootSpawnerScript = GetComponent<LootSpawner>();
        WaveSpawnerScript.SpawnWaves = false;
        LootSpawnerScript.SpawnLoots = false;

        CurrentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        InitialTime = Time.time;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            SceneArray [i] = SceneUtility.GetScenePathByBuildIndex(i);
        }

        if (SceneArray [CurrentSceneIndex].Contains(SceneKeywordInMenu))
        {
            GameStatus = GameState.InMenu;
        }
        else if (SceneArray [CurrentSceneIndex].Contains(SceneKeywordInGame))
        {
            GameStatus = GameState.InGame;

            PlayerManagerScript.PlayerSpawn(DefaultTestPlayer.transform);
            UI.SCenterTextMessage(WaveStartText, 300f);
        }
        else
        {
            Debug.LogWarning("Game is in an invalid GameState;");
        }
    }
	
	void Update () 
	{
        if (CountTime)
        {
            TotalAppTime = Time.time - InitialTime;
            if (GameStatus == GameState.InGame)
            {
                InGameTime = TotalAppTime - InGameInitialTime;

                if (Sun != null)
                {
                    Quaternion LookRotation = Quaternion.Euler(new Vector3(InGameTime / SunSpeedDivisor, 0f, 0f));
                    //360f multiplied by SunSpeedDivisor is the number of seconds one in-game full day takes//
                
                    Sun.transform.rotation = Quaternion.Slerp(Sun.transform.rotation, LookRotation, Time.deltaTime * 5f);
                }

                if (UI.UseStopWatch)
                {
                    UI.ShowTime(InGameTime);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            WaveSpawnerScript.SpawnWaves = true;
            LootSpawnerScript.SpawnLoots = true;
            UI.SCenterText.enabled = false;
            StopCoroutine(UI.CloseSCenterText(0f));
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            switch (GameStatus)
            {
                case GameState.InGame:
                    if (!GameFinished)
                    {
                        Pause();
                    }
                    else
                    {
                        StartCoroutine(EndGame());
                    }
                break;

                case GameState.InGamePaused:
                    UnPause();
                break;

                case GameState.InMenu:
                    Quit();
                break;

                default:
                    Debug.LogWarning ("Game is in an invalid GameState;");
                break;
            }    
        }
	}

    public void SceneChanger (int SceneIndex)
    {
        if (SceneIndex != CurrentSceneIndex)
        {
            for (int i = 0; i  < SceneArray.Length; i++)
            {
                if (SceneIndex == i)
                {
                    Debug.Log("Changed scene from Scene " + CurrentSceneIndex + " to Scene " + SceneIndex + ";");
                    SceneManager.LoadScene(SceneIndex);

                    if (SceneArray[i].Contains(SceneKeywordInMenu))
                    {
                        if (GameStatus != GameState.InMenu)
                        {
                            GameStatus = GameState.InMenu;
                        }

                        Cursor.lockState = CursorLockMode.None;
                        Cursor.visible = true;
                        UI.Menu(UIManager.MainMenuPanel);
                    }

                    if (SceneArray [i].Contains(SceneKeywordInGame))
                    {
                        UnPause();

                        Cursor.lockState = CursorLockMode.Locked;
                        Cursor.visible = false;
                        UI.SCenterTextMessage(WaveStartText, 300f);

                        InGameInitialTime = TotalAppTime;
                        InGameTime = 0f;

                        StartCoroutine(PlayerJoin());
                    }

                    break;
                }
            }

            CurrentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        }
        else
        {
            Debug.Log("Current scene is already Scene " + SceneIndex + ";");
        }
    }

    public IEnumerator PlayerJoin ()
    {
        yield return new WaitForSeconds(PlayerJoinDelay);

        PlayerManagerScript.PlayerSpawn(DefaultTestPlayer.transform);
    }

    public void GameOver ()
    {
        UI.PCenterTextMessage(GameOverText, 10f);
        UI.SCenterTextMessage(SecondaryGameOverText, 300f);
        GameFinished = true;

        Debug.Log("Game over;");
    }

    public IEnumerator EndGame ()
    {
        yield return new WaitForSeconds(GameOverDelay);
        
        CurrentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneChanger(0);
    }

    public void Pause ()
    {
        Debug.Log("Game paused;");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        //DO NOT USE THIS CODE IN MULTIPLAYER UNLESS ALLOWED//
        if (GameStatus != GameState.InGamePaused)
        {
            GameStatus = GameState.InGamePaused;
        }
        Time.timeScale = 0f;
        //DO NOT USE THIS CODE IN MULTIPLAYER UNLESS ALLOWED//

        UI.CloseMenu(true);
    }

    public void UnPause()
    {
        Debug.Log("Game unpaused;");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        //DO NOT USE THIS CODE IN MULTIPLAYER UNLESS ALLOWED//
        if (GameStatus != GameState.InGame)
        {
            GameStatus = GameState.InGame;
        }
        Time.timeScale = 1f;
        //DO NOT USE THIS CODE IN MULTIPLAYER UNLESS ALLOWED//

        UI.CloseMenu(false);
    }

    public void Quit ()
	{
		Debug.Log("Game is quit;");
        CurrentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        Application.Quit();
	}
}