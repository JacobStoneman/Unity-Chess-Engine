using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitInfo : MonoBehaviour
{
    //Script holds the info for the given piece and moves it to the correct position
    public Controller.unitType type;
    public int basePower;
    public int colour;
    public Vector3 targetPosition;
    public Vector3 returnPosition;
    int step = 3;
    public bool moved;
    public string rep;
    Controller controller;

    // Start is called before the first frame update
    void Start()
    {
        controller = GameObject.Find("Board").GetComponent<Controller>();
        targetPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position != targetPosition)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, step * Time.deltaTime);
        } else
        {
            transform.position = targetPosition;
        }
    }
}
