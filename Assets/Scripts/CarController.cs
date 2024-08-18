using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NNet))]
public class CarController : MonoBehaviour
{
    private Vector3 startPosition, startRotation;
    private NNet network;

    //Acceleration and turning value
    [Range(-1f, 1f)]
    public float a, t;

    public float timeSinceStart = 0f;

    [Header("Fitness")]
    public float overallFitness;
    public float distanceMultiplier = 1.4f; // How far it goes 
    public float avgSpeedMultiplier = 0.2f; // How fast it goes 
    public float sensorMultiplier = 0.1f;

    [Header("Network Options")]
    public int LAYERS = 1;
    public int NEURONS = 10;

    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;

    private float aSensor, bSensor, cSensor,dSensor,eSensor;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        network = GetComponent<NNet>();

        int carLayer = LayerMask.NameToLayer("Car");
        Physics.IgnoreLayerCollision(carLayer, carLayer);
    }

    public void ResetWithNetwork(NNet net)
    {
        network = net;
        Reset();
    }

    public void Reset()
    {
        timeSinceStart = 0f;
        totalDistanceTravelled = 0f;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
        gameObject.SetActive(true); // Activate the car
    }

    private void OnCollisionEnter(Collision collision)
    {
        Death();
    }

    private void FixedUpdate()
    {
        InputSensors();
        lastPosition = transform.position;

        //Neural network code
        (a, t) = network.RunNetwork(aSensor, bSensor, cSensor,dSensor,eSensor);

        MoveCar(a, t);

        timeSinceStart += Time.deltaTime;

        CalculateFitness();
    }

    private void Death()
    {
        FindObjectOfType<GeneticManager>().Death(this, overallFitness, network);
    }

    private void CalculateFitness()
    {
        totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        avgSpeed = totalDistanceTravelled / timeSinceStart;

        overallFitness = (totalDistanceTravelled * distanceMultiplier) + (avgSpeed * avgSpeedMultiplier) + (((aSensor + bSensor + cSensor) / 3) * sensorMultiplier);

        if (timeSinceStart > 20 && overallFitness < 40)
        {
            Death();
        }

        if (overallFitness >= 1000)
        {
            Death();
        }
    }

    private void InputSensors()
    {
        Vector3 forwardRight = (transform.forward + transform.right).normalized;
        Vector3 forward = transform.forward;
        Vector3 forwardLeft = (transform.forward - transform.right).normalized;
        Vector3 right45 = (Quaternion.Euler(0, 22.5f, 0) * Vector3.forward).normalized;
        Vector3 left45 = (Quaternion.Euler(0, -22.5f, 0) * Vector3.forward).normalized;

        aSensor = RaySensor(forwardRight, Color.red);
        bSensor = RaySensor(forward, Color.red);
        cSensor = RaySensor(forwardLeft, Color.red);
        dSensor = RaySensor(transform.TransformDirection(right45), Color.blue);
        eSensor = RaySensor(transform.TransformDirection(left45), Color.blue);
    }

    private float RaySensor(Vector3 direction, Color color)
    {
        Ray r = new Ray(transform.position, direction);
        RaycastHit hit;
        if (Physics.Raycast(r, out hit))
        {
            Debug.DrawLine(r.origin, hit.point, color);
            return hit.distance / 20;
        }
        Debug.DrawLine(r.origin, r.origin + direction * 20, color); // Draw the ray when no hit
        return 1; // Max distance if no hit
    }


    private Vector3 input;
    public void MoveCar(float v, float h)
    {
        input = Vector3.Lerp(Vector3.zero, new Vector3(0, 0, v * 11.4f), 0.02f);
        input = transform.TransformDirection(input);

        transform.position += input;

        transform.eulerAngles += new Vector3(0, (h * 90) * 0.02f, 0);
    }
}