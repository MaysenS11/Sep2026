using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Level";

    private void OnEnable()
    {
        EventBus<CharacterSelectedEvent>.Subscribe(OnCharacterSelected);
    }

    private void OnDisable()
    {
        EventBus<CharacterSelectedEvent>.Unsubscribe(OnCharacterSelected);
    }

    private void OnCharacterSelected(CharacterSelectedEvent evt)
    {
        if (evt.Character != null)
        {
            CharacterSelectData.SelectedCharacter = evt.Character;
            SceneManager.LoadScene(gameSceneName);
        }
    }

    [SerializeField] private UI.OptionsMenuUI optionsMenu;
    [SerializeField] private GameObject startMenuPanel;

    private void Awake()
    {
        if (optionsMenu == null)
        {
            optionsMenu = FindAnyObjectByType<UI.OptionsMenuUI>(FindObjectsInactive.Include);
        }
    }

    public void PlayGame()
    {
        if (CharacterSelectData.SelectedCharacter != null)
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // If character wheel active, select top slot, or load default level
            var wheel = FindAnyObjectByType<CharacterWheelController>();
            if (wheel != null)
            {
                // Wait for player to choose or start
            }
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OpenOptions()
    {
        if (optionsMenu != null)
        {
            optionsMenu.OpenOptions();
        }
    }

    public void CloseOptions()
    {
        if (optionsMenu != null)
        {
            optionsMenu.CloseOptions();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
