using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform carTransform;
    [Range(1, 10)]
    public float followSpeed = 2f;
    [Range(1, 10)]
    public float lookSpeed = 5f;

    // Camera offset presets
    private static readonly Vector3[] cameraOffsets = new Vector3[]
    {
        new Vector3(0, 10, -20),
        new Vector3(0, 100, -20),
        new Vector3(0, 6, -20),
        new Vector3(0, 10, 60),
        new Vector3(30, 10, -20),
        new Vector3(-30, 10, -20)
    };

    private int player1Counter = 0;
    private int player2Counter = 0;

    public Vector3 cameraOffset = new Vector3(0f, 10f, -20f);
    public float lookAheadDistance = 6f;

    private void FixedUpdate()
    {
        FollowCar();
        LookAhead();
    }

    private void Update()
    {
        HandleInput();
    }

    private void FollowCar()
    {
        Vector3 desiredPosition = carTransform.position
                                + carTransform.up * cameraOffset.y
                                + carTransform.forward * cameraOffset.z
                                + carTransform.right * cameraOffset.x;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }

    private void LookAhead()
    {
        Vector3 lookAtPoint = carTransform.position + carTransform.forward * lookAheadDistance;
        Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, lookSpeed * Time.deltaTime);
    }

    private void HandleInput()
    {
        bool isPlayer1 = carTransform != null && carTransform.name == "Player 1";
        bool isPlayer2 = carTransform != null && carTransform.name == "Player 2";

        if (isPlayer1)
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.T))
            {
                player1Counter = (player1Counter + 1) % 6;
                cameraOffset = cameraOffsets[player1Counter];
            }
        }
        else if (isPlayer2)
        {
            if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.Y))
            {
                player2Counter = (player2Counter + 1) % 6;
                cameraOffset = cameraOffsets[player2Counter];
            }
        }
    }
}
