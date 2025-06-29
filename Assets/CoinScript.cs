using UnityEngine;
using Unity.VisualScripting;

public class CoinScript : MonoBehaviour
{
    public float spinSpeed = 90f; // degrees per second

    void Update()
    {
        // Spin the coin smoothly
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject carObj = other.transform.root.gameObject;
        var sceneVariables = Variables.Scene(gameObject.scene);
        
        if (carObj.name == "Player 1")
        {
            int score = (int)sceneVariables.Get("Player 1 Coins");
            sceneVariables.Set("Player 1 Coins", score + 1);
            Debug.Log("Player 1 collected a coin! Score: " + (score + 1));
            FindFirstObjectByType<CoinManager>().HideCoin(gameObject);
        }
        else if (carObj.name == "AI Car" || carObj.name == "Player 2")
        {
            int score = (int)sceneVariables.Get("Player 2 Coins");
            sceneVariables.Set("Player 2 Coins", score + 1);
            Debug.Log(carObj.name + " collected a coin! Score: " + (score + 1));
            FindFirstObjectByType<CoinManager>().HideCoin(gameObject);
        }
    }


}
