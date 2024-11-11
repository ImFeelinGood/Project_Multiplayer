using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
public class Init : MonoBehaviour
{
    async void Start()
    {
        await UnityServices.InitializeAsync();
        
        if(UnityServices.State == ServicesInitializationState.Initialized)
        {
            AuthenticationService.Instance.SignedIn += OnSignedIn;

            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if(AuthenticationService.Instance.IsSignedIn)
            {
                string username = PlayerPrefs.GetString(key:"username");
                if(username == "")
                {
                    username = "player";
                    PlayerPrefs.SetString("username", username);
                }
            }
        }
    }

    private void OnSignedIn()
    {
        Debug.Log(message:$"Token: {AuthenticationService.Instance.AccessToken}");
        Debug.Log(message:$"Player Id: {AuthenticationService.Instance.PlayerId}");
    }
}
