using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using TMPro;

public class Menu : MonoBehaviour
{
  public string GameScene;
  public TextMeshProUGUI MusicValue;
  public AudioMixer MusicMixer;

  public TextMeshProUGUI SoundValue;
  public AudioMixer SoundMixer;
  private Animator animator;
  private int _window = 0;

  public void Start()
  {
    animator = GetComponent<Animator>();
  }

  public void Update()
  {
    if(Input.GetKeyDown(KeyCode.Escape) && _window == 1)
    {
      animator.SetTrigger("HideOptions");
      _window = 0;
    }
  }
  public void NewGame()
  {
    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex+1); //LoadSceneSingle will have new scene take place of old -taking up less memory
  }

  public void ShowOptions()
  {
    animator.SetTrigger("ShowOptions");
    _window = 1;
  }

  public void Quit()
  {
    Application.Quit();
  }

  public void OnMusicChanged(float value)
  {
    MusicValue.SetText(value + "%");
    MusicMixer.SetFloat("Volume", -50 + value/2);
  }

  public void OnSoundChanged(float value)
  {
    SoundValue.SetText(value + "%");
    SoundMixer.SetFloat("Volume", -50 + value/2);
  }
}