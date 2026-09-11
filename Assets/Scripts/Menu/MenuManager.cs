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
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
