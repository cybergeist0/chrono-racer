using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

/// <summary>
/// Manages all coins in the scene and handles resetting them.
/// </summary>
public class CoinManager : MonoBehaviour
{
    private List<Vector3> initialPositions = new List<Vector3>();
    private List<Quaternion> initialRotations = new List<Quaternion>();
    private List<GameObject> coins = new List<GameObject>();

    void Start()
    {
        // Find all coins at start and record their positions/rotations
        foreach (var coin in FindObjectsByType<CoinScript>(FindObjectsSortMode.None))
        {
            coins.Add(coin.gameObject);
            initialPositions.Add(coin.transform.position);
            initialRotations.Add(coin.transform.rotation);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllCoins();
        }
    }

    public void ResetAllCoins()
    {
        var sceneVariables = Variables.Scene(gameObject.scene);
        sceneVariables.Set("Player 1 Coins", 0);
        sceneVariables.Set("Player 2 Coins", 0);
        for (int i = 0; i < coins.Count; i++)
        {
            var coin = coins[i];
            coin.transform.position = initialPositions[i];
            coin.transform.rotation = initialRotations[i];
            coin.SetActive(true);

            // Re-enable collider if it was disabled
            var collider = coin.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = true;
        }
    }

    public void HideCoin(GameObject coin)
    {
        // Hide the coin and disable its collider
        coin.SetActive(false);
    }
}
