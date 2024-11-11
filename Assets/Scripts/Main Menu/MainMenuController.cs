using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace game
{
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button submitButton;
    [SerializeField] private Button backButton;
    
    [SerializeField] private GameObject buttonsObject;
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject joinPanel;

    [SerializeField] private TMP_Text codeText;

    void Start()
    {
        createButton.onClick.AddListener(CreateLobby);
        joinButton.onClick.AddListener(() => StartCoroutine(ActivateLobby(1.5f)));
        submitButton.onClick.AddListener(SubmitCode);
        backButton.onClick.AddListener(() => StartCoroutine(BackToMainMenu(1.5f)));
    }

    void OnDisable()
    {
        createButton.onClick.RemoveAllListeners();
        joinButton.onClick.RemoveAllListeners();
        submitButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    private async void CreateLobby()
    {
        bool succeeded = await GameLobbyManager.instance.CreateLobby();
        if(succeeded)
        {
            StartCoroutine(CreateLobbyCoroutine(1f));
        }
    }

    private IEnumerator CreateLobbyCoroutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        buttonsObject.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private async void SubmitCode()
    {
        string code = codeText.text;
        code = code.Substring(0, code.Length - 1);

        bool succeeded = await GameLobbyManager.instance.JoinLobby(code);
        if(succeeded)
        {
            StartCoroutine(JoinLobbyAsClient(1.5f));
        }
    }

    private IEnumerator JoinLobbyAsClient(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        joinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    private IEnumerator ActivateLobby(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        joinPanel.SetActive(true);
        buttonsObject.SetActive(false);
    }

    public IEnumerator BackToMainMenu(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        joinPanel.SetActive(false);
        buttonsObject.SetActive(true);
    }
}
}