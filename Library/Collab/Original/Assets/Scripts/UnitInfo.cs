using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitInfo : MonoBehaviour
{
    public enum unitType
    {
        PAWN,
        ROOK,
        KNIGHT,
        BISHOP,
        QUEEN,
        KING
    }
    public unitType type;
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
        if(colour == 0)
        {
            controller.gameBoard.white.Add(gameObject);
        } else if(colour == 1)
        {
            controller.gameBoard.black.Add(gameObject);
        }
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
