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

    private float aSensor, bSensor, cSensor;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        network = GetComponent<NNet>();



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
        (a, t) = network.RunNetwork(aSensor, bSensor, cSensor);

        MoveCar(a, t);

        timeSinceStart += Time.deltaTime;

        CalculateFitness();
        //a = 0;
        //t = 0;
    }
    private void Death()
    {
        GameObject.FindObjectOfType<GeneticManager>().Death(overallFitness,network);
    }
    private void CalculateFitness()
    {
        totalDistanceTravelled += Vector3.Distance(transform.position, lastPosition);
        avgSpeed = totalDistanceTravelled / timeSinceStart;

        overallFitness = (totalDistanceTravelled * distanceMultiplier) + (avgSpeed * avgSpeedMultiplier) + (((
            aSensor + bSensor + cSensor) / 3) * sensorMultiplier);

        if(timeSinceStart > 20 && overallFitness < 40)
        {
            Death();
        }

        if(overallFitness >= 1000)
        {
            //SavesNetwork to a Json
            Death();
        }
    }
    private void InputSensors()
    {
        Vector3 a = (transform.forward + transform.right);
        Vector3 b = (transform.forward);
        Vector3 c = (transform.forward - transform.right);

        Ray r = new Ray(transform.position, a);
        RaycastHit hit;
        if(Physics.Raycast(r, out hit))
        {
            aSensor = hit.distance / 20;
           
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }

        r.direction = b;
        if (Physics.Raycast(r, out hit))
        {
            bSensor = hit.distance / 20;
          
            Debug.DrawLine(r.origin, hit.point, Color.red);
        }

        r.direction = c;
        if (Physics.Raycast(r, out hit))
        {
            cSensor = hit.distance / 20;
          
            Debug.DrawLine(r.origin, hit.point, Color.red);

        }
    }

    private Vector3 input;
    public void MoveCar(float v, float h)
    {
        input = Vector3.Lerp(Vector3.zero, new Vector3(0,0, v * 11.4f), 0.02f);
        input = transform.TransformDirection(input);

        transform.position += input;

        transform.eulerAngles += new Vector3(0,(h*90)*0.02f,0);
    }
}
