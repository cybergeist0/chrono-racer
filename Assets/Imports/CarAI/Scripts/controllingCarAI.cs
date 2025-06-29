using UnityEngine;

public class controllingCarAI : MonoBehaviour
{
    //This is an example script. To show you how to control the AI using script(At Runtime)
    private int index;
    public CarAI carAI;
    
    void variables()
    {
        


        //5- Show Gizmos
        carAI.ShowGizmos = true;
        //or hide Gizmos
        carAI.ShowGizmos = false;

        //6- Allow thr car to move
        carAI.move = true;
        //or apply brakes
        carAI.move = false;
    }

    void Methods()
    {

    }
}
