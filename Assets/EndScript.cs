using UnityEngine;
using Unity.VisualScripting;

public class EndScript : MonoBehaviour
{
    private float timerStartTime;

    void Start()
    {
        timerStartTime = Time.time;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            timerStartTime = Time.time;
            var sceneVariables = Variables.Scene(gameObject.scene);
            // Reset lap times and coins
            sceneVariables.Set("p1fl", -1f);
            sceneVariables.Set("p1sl", -1f);
            sceneVariables.Set("p1tl", -1f);
            sceneVariables.Set("p2fl", -1f);
            sceneVariables.Set("p2sl", -1f);
            sceneVariables.Set("p2tl", -1f);
            sceneVariables.Set("Player 1 Coins", 0);
            sceneVariables.Set("Player 2 Coins", 0);
            sceneVariables.Set("Player-AI Penalty", 0);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        GameObject carObj = other.transform.root.gameObject;
        float finishTime = Time.time - timerStartTime;
        var sceneVariables = Variables.Scene(gameObject.scene);

        // Determine player prefix
        string prefix = "";
        if (carObj.name == "Player 1")
            prefix = "p1";
        else if (carObj.name == "AI Car" || carObj.name == "Player 2")
            prefix = "p2";
        else
        {
            Debug.Log("Unknown car name: " + carObj.name);
            return;
        }

        // Update the first unset lap variable for this player
        string[] laps = { "fl", "sl", "tl" };
        foreach (var lap in laps)
        {
            string varName = prefix + lap;
            if ((float)sceneVariables.Get(varName) == -1f)
            {
                int penalty = (int)sceneVariables.Get("Player-AI Penalty");
                if (prefix == "p2")
                {
                    penalty = 0;
                }
                sceneVariables.Set(varName, finishTime + (float)penalty);
                break;
            }
        }

        Debug.Log($"{carObj.name} has completed a lap in {finishTime:F2} seconds");
    }

    void OnTriggerStay(Collider other)
    {
        // Debug.Log("Object is staying in the trigger");
    }

    void OnTriggerExit(Collider other)
    {
        // Debug.Log("Object has exited the trigger");
    }
}
