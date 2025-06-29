using System.Collections.Generic;
using UnityEngine;

public class CarAI : MonoBehaviour
{
    [Header("Car Wheels (Wheel Collider)")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider backLeft;
    public WheelCollider backRight;

    [Header("Car Wheels (Transform)")]
    public Transform wheelFL;
    public Transform wheelFR;
    public Transform wheelBL;
    public Transform wheelBR;

    [Header("Car Front (Transform)")]
    public Transform carFront;

    [Header("General Parameters")]
    [Range(10, 45)]
    public int MaxSteeringAngle = 27;
    [Range(20, 190)]
    public int MaxSpeed = 90;
    [Range(10, 120)]
    public int MaxReverseSpeed = 45;
    [Range(1, 10)]
    public int AccelerationMultiplier = 2;
    [Range(1, 10)]
    public int DecelerationMultiplier = 2;
    [Range(100, 600)]
    public int BrakeForce = 350;

    [Header("AI Parameters")]
    public float WaypointReachRadius = 8f;
    public float SteeringSpeed = 0.5f;
    public float ThrottleAggression = 1.8f;
    public float ThrottleSmooth = 3.0f;
    public float BrakeDistance = 15f;
    public float SlowDownAngle = 15f;

    [Header("Waypoint Container")]
    public GameObject waypointContainer;

    [Header("Debug")]
    public bool ShowGizmos;
    public bool Debugger;

    [Header("Braking Zone Settings")]
    public float brakingZoneSpeed = 25f;

    [Header("Stuck Detection")]
    public float stuckSpeedThreshold = 1.0f; // km/h
    public float stuckTimeThreshold = 2.0f; // seconds being below speed to consider stuck
    public float reverseTime = 1.5f; // seconds to reverse before retrying

    [HideInInspector] public bool move;
    private Vector3 targetWaypoint = Vector3.zero;
    public int currentWayPoint;
    private bool allowMovement;
    private List<Vector3> waypoints = new List<Vector3>();
    private List<Vector3> allWaypoints = new List<Vector3>(); // Store original waypoints for cycling
    private float throttleInput;
    private float steeringAxis;
    private float carSpeed;
    private float localVelocityZ;
    private float localVelocityX;
    private Rigidbody carRigidbody;

    AudioSource aus;
    float audioPitch;
    int SpeedOfWheels;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private bool inBrakingZone = false;

    // Stuck detection variables
    private float stuckTimer = 0f;
    private bool isStuck = false;
    private float reverseTimer = 0f;
    private bool reversing = false;
    private bool ignoreWaypoint = false;

    void Awake()
    {
        currentWayPoint = 0;
        allowMovement = true;
        move = true;
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = new Vector3(0, -0.3f, 0);
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    void Start()
    {
        aus = GetComponent<AudioSource>();

        if (waypointContainer != null)
        {
            foreach (Transform child in waypointContainer.transform)
            {
                waypoints.Add(child.position);
                allWaypoints.Add(child.position); // Save for cycling
            }
        }
        else
        {
            Debug.LogError("No waypoint container assigned to CarAI!");
        }
    }

    void FixedUpdate()
    {
        UpdateCarPhysics();
        UpdateWheels();
        ApplySteering();
        PathProgress();
        HandleStuckLogic();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCarToInitialPosition();
        }
    }

    private void ResetCarToInitialPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        if (carRigidbody != null)
        {
            carRigidbody.linearVelocity = Vector3.zero;
            carRigidbody.angularVelocity = Vector3.zero;
        }
        currentWayPoint = 0;
        steeringAxis = 0f;
        throttleInput = 0f;
        inBrakingZone = false;
        stuckTimer = 0f;
        isStuck = false;
        reverseTimer = 0f;
        reversing = false;
        ignoreWaypoint = false;
    }

    void UpdateCarPhysics()
    {
        carSpeed = carRigidbody.linearVelocity.magnitude * 3.6f; // m/s to km/h
        Vector3 localVel = transform.InverseTransformDirection(carRigidbody.linearVelocity);
        localVelocityX = localVel.x;
        localVelocityZ = localVel.z;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If collided with player, apply bump
        if (collision.gameObject.name == "Player 1")
        {
            var playerController = collision.gameObject.GetComponent<PrometeoCarController>();
            if (playerController != null)
            {
                // Calculate bump direction (from AI to player)
                Vector3 bumpDir = (collision.transform.position - transform.position).normalized;
                playerController.ApplyAIBump(bumpDir, 4000f); // Increase force for a more dramatic effect
            }
        }
    }



    private void PathProgress()
    {
        // If finished, restart with the original waypoints to cycle
        if (currentWayPoint >= waypoints.Count)
        {
            // Reset to the original full list for cycling
            waypoints.Clear();
            waypoints.AddRange(allWaypoints);
            currentWayPoint = 0;
        }

        // If ignoring waypoint, do not update targetWaypoint or check for progress
        if (ignoreWaypoint)
        {
            Movement();
            return;
        }

        targetWaypoint = waypoints[currentWayPoint];
        allowMovement = true;
        float dist = Vector3.Distance(carFront.position, targetWaypoint);
        if (dist < WaypointReachRadius)
            currentWayPoint++;

        Movement();
    }

    void ApplySteering()
    {
        if (!allowMovement) return;

        // If ignoring waypoint (stuck), keep wheels straight
        if (ignoreWaypoint)
        {
            steeringAxis = Mathf.MoveTowards(steeringAxis, 0f, Time.fixedDeltaTime * 10f * SteeringSpeed);
        }
        else
        {
            Vector3 relativeVector = transform.InverseTransformPoint(targetWaypoint);
            float targetAngle = Mathf.Atan2(relativeVector.x, relativeVector.z) * Mathf.Rad2Deg;
            float steer = Mathf.Clamp(targetAngle / MaxSteeringAngle, -1f, 1f);
            steeringAxis = Mathf.MoveTowards(steeringAxis, steer, Time.fixedDeltaTime * 10f * SteeringSpeed);
        }

        float steeringAngle = steeringAxis * MaxSteeringAngle;
        frontLeft.steerAngle = Mathf.Lerp(frontLeft.steerAngle, steeringAngle, SteeringSpeed);
        frontRight.steerAngle = Mathf.Lerp(frontRight.steerAngle, steeringAngle, SteeringSpeed);
    }

    void Movement()
    {
        if (!allowMovement)
        {
            ApplyBrakes();
            return;
        }

        // If ignoring waypoint (stuck), just reverse
        if (ignoreWaypoint) // When stuck, reverse
        {
            // Smoothly set throttle axis to -1
            throttleInput = Mathf.MoveTowards(throttleInput, -1f, Time.fixedDeltaTime * ThrottleSmooth * 2f);
            float reverseTorque = (AccelerationMultiplier * 50f) * throttleInput;

            // NO BRAKING WHILE REVERSING!
            frontLeft.brakeTorque = 0f;
            frontRight.brakeTorque = 0f;
            backLeft.brakeTorque = 0f;
            backRight.brakeTorque = 0f;

            // Apply negative torque to all wheels (or just rear wheels, if RWD)
            frontLeft.motorTorque = reverseTorque;
            frontRight.motorTorque = reverseTorque;
            backLeft.motorTorque = reverseTorque;
            backRight.motorTorque = reverseTorque;

            // Optionally, keep steering axis at 0 for straight backup
            steeringAxis = Mathf.MoveTowards(steeringAxis, 0f, Time.fixedDeltaTime * 10f * SteeringSpeed);

            if (aus != null)
            {
                float pitch = 0.7f + Mathf.Abs(carSpeed) / (MaxSpeed * 1.2f);
                aus.pitch = Mathf.Clamp(pitch, 0.7f, 1.7f);
            }
            return;
        }

        // HARSH BRAKING ZONE LOGIC WITH REVERSE THROTTLE
        if (inBrakingZone && carSpeed > brakingZoneSpeed + 2f)
        {
            // Apply strong brakes and reverse throttle for ultra-rapid deceleration
            throttleInput = Mathf.MoveTowards(throttleInput, -1f, Time.fixedDeltaTime * ThrottleSmooth * 2f);

            float torque = (AccelerationMultiplier * 50f) * throttleInput;
            float strongBrake = BrakeForce * 4f;
            frontLeft.motorTorque = torque;
            frontRight.motorTorque = torque;
            backLeft.motorTorque = torque;
            backRight.motorTorque = torque;

            frontLeft.brakeTorque = strongBrake;
            frontRight.brakeTorque = strongBrake;
            backLeft.brakeTorque = strongBrake;
            backRight.brakeTorque = strongBrake;
        }
        else
        {
            float desiredSpeed = MaxSpeed;
            if (inBrakingZone)
            {
                desiredSpeed = brakingZoneSpeed;
            }
            else
            {
                float turnAngle = GetUpcomingTurnAngle();
                if (Mathf.Abs(turnAngle) > SlowDownAngle)
                {
                    float t = Mathf.InverseLerp(SlowDownAngle, 80f, Mathf.Abs(turnAngle));
                    desiredSpeed = Mathf.Lerp(MaxSpeed * 0.5f, MaxSpeed * 0.1f, t);
                }
            }

            float targetThrottle = 0f;
            if (carSpeed < desiredSpeed - 2f)
                targetThrottle = ThrottleAggression;
            else if (carSpeed > desiredSpeed + 2f)
                targetThrottle = -0.5f;

            throttleInput = Mathf.MoveTowards(throttleInput, targetThrottle, Time.fixedDeltaTime * ThrottleSmooth);

            float torque = (AccelerationMultiplier * 50f) * throttleInput;
            float brake = 0f;

            if (throttleInput > 0f)
            {
                frontLeft.brakeTorque = 0;
                frontRight.brakeTorque = 0;
                backLeft.brakeTorque = 0;
                backRight.brakeTorque = 0;

                frontLeft.motorTorque = torque;
                frontRight.motorTorque = torque;
                backLeft.motorTorque = torque;
                backRight.motorTorque = torque;
            }
            else if (throttleInput < 0f)
            {
                brake = BrakeForce;
                frontLeft.brakeTorque = brake;
                frontRight.brakeTorque = brake;
                backLeft.brakeTorque = brake;
                backRight.brakeTorque = brake;

                frontLeft.motorTorque = 0;
                frontRight.motorTorque = 0;
                backLeft.motorTorque = 0;
                backRight.motorTorque = 0;
            }
            else
            {
                frontLeft.motorTorque = 0;
                frontRight.motorTorque = 0;
                backLeft.motorTorque = 0;
                backRight.motorTorque = 0;

                frontLeft.brakeTorque = BrakeForce * 0.1f;
                frontRight.brakeTorque = BrakeForce * 0.1f;
                backLeft.brakeTorque = BrakeForce * 0.1f;
                backRight.brakeTorque = BrakeForce * 0.1f;
            }
        }

        if (aus != null)
        {
            float pitch = 0.7f + carSpeed / (MaxSpeed * 1.2f);
            aus.pitch = Mathf.Clamp(pitch, 0.7f, 1.7f);
        }
    }

    void HandleStuckLogic()
    {
        // If not already reversing, check for being stuck
        if (!ignoreWaypoint)
        {
            if (Mathf.Abs(carSpeed) < stuckSpeedThreshold)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer > stuckTimeThreshold)
                {
                    // Become "unaware" of waypoint and start reversing
                    ignoreWaypoint = true;
                    reverseTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        else
        {
            // While reversing, count time
            reverseTimer += Time.fixedDeltaTime;
            if (reverseTimer > reverseTime)
            {
                // Become sentient again and try moving toward waypoint
                ignoreWaypoint = false;
                stuckTimer = 0f;
                reverseTimer = 0f;
            }
        }
    }

    float GetUpcomingTurnAngle()
    {
        if (currentWayPoint + 1 < waypoints.Count)
        {
            Vector3 curr = waypoints[currentWayPoint];
            Vector3 next = waypoints[currentWayPoint + 1];
            Vector3 toCurr = (curr - transform.position).normalized;
            Vector3 toNext = (next - curr).normalized;
            float angle = Vector3.Angle(toCurr, toNext);
            return angle;
        }
        else
        {
            return 0f;
        }
    }

    private void ApplyBrakes()
    {
        frontLeft.brakeTorque = BrakeForce;
        frontRight.brakeTorque = BrakeForce;
        backLeft.brakeTorque = BrakeForce;
        backRight.brakeTorque = BrakeForce;

        frontLeft.motorTorque = 0;
        frontRight.motorTorque = 0;
        backLeft.motorTorque = 0;
        backRight.motorTorque = 0;
    }

    private void UpdateWheels()
    {
        ApplyRotationAndPosition(frontLeft, wheelFL);
        ApplyRotationAndPosition(frontRight, wheelFR);
        ApplyRotationAndPosition(backLeft, wheelBL);
        ApplyRotationAndPosition(backRight, wheelBR);
    }

    private void ApplyRotationAndPosition(WheelCollider targetWheel, Transform wheel)
    {
        targetWheel.ConfigureVehicleSubsteps(5, 12, 15);
        Vector3 pos;
        Quaternion rot;
        targetWheel.GetWorldPose(out pos, out rot);
        wheel.position = pos;
        wheel.rotation = rot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("BrakingPoint"))
        {
            inBrakingZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("BrakingPoint"))
        {
            inBrakingZone = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (ShowGizmos && waypoints != null)
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (i == currentWayPoint)
                    Gizmos.color = Color.blue;
                else if (i > currentWayPoint)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(waypoints[i], 2f);
            }
        }
    }
}