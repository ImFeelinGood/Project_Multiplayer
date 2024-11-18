using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;

public class Init : MonoBehaviour
{
    async void Start()
    {
        // Initialize Unity Services
        await UnityServices.InitializeAsync();

        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            // Check if the player is already signed in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                AuthenticationService.Instance.SignedIn += OnSignedIn;

                // Sign in anonymously
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            // Ensure the player is signed in before proceeding
            if (AuthenticationService.Instance.IsSignedIn)
            {
                // Handle username logic
                string username = PlayerPrefs.GetString(key: "username", defaultValue: "");
                if (string.IsNullOrEmpty(username))
                {
                    username = "player";
                    PlayerPrefs.SetString("username", username);
                }

                Debug.Log($"Username: {username}");
            }
        }
    }

    private void OnSignedIn()
    {
        Debug.Log(message: $"Token: {AuthenticationService.Instance.AccessToken}");
        Debug.Log(message: $"Player Id: {AuthenticationService.Instance.PlayerId}");
    }
}
