using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    const int boardLength = 8;
    int turnCount = 1;
    int currentTurn = 0;
    public GameObject moveHighlight;
    public GameObject tileHighlight;
    public GameObject camPoint1;
    public GameObject camPoint2;
    public GameObject cam;
    public Mesh whiteQueen;
    public Mesh blackQueen;
    public Text turnText;
    GameObject winText;
    Vector3 targetPosition;
    Quaternion targetRotation;
    [SerializeField] List<PotentialMove> potentialMoves = new List<PotentialMove>();
    List<GameObject> movePrefabs = new List<GameObject>();
    bool gameOver;
    [SerializeField] int whiteTurn;
    [SerializeField] int blackTurn;

    enum TurnState
    {
        PASSIVE,
        DECIDING,
        CHOSEN
    }
    TurnState turnState;

    [System.Serializable]
    enum AITurnState
    {
        PASSIVE,
        DECIDING,
        CHOSEN
    }
    [SerializeField]AITurnState aiTurnState;

    enum GameType
    {
        PLAYERONLY,
        AIONLY,
        PLAYERAI
    }

    [SerializeField]GameType gameType;

    int step = 2;
    [System.Serializable]
    public class Board
    {
        [SerializeField]public Tile[,] tiles = new Tile[boardLength, boardLength];
        [SerializeField] public string[,] boardState = new string[boardLength,boardLength];
        public int totalBoardValue;
        public List<GameObject> white = new List<GameObject>();
        public List<GameObject> black = new List<GameObject>();
        public void GetBoardValue(int colour)
        {
            totalBoardValue = 0;
            if (colour == 0)
            {
                foreach (GameObject piece in white)
                {
                    totalBoardValue += piece.GetComponent<UnitInfo>().basePower;
                }
                foreach (GameObject piece in black)
                {
                    totalBoardValue -= piece.GetComponent<UnitInfo>().basePower;
                }
            } else if (colour == 1)
            {
                foreach (GameObject piece in white)
                {
                    totalBoardValue -= piece.GetComponent<UnitInfo>().basePower;
                }
                foreach (GameObject piece in black)
                {
                    totalBoardValue += piece.GetComponent<UnitInfo>().basePower;
                }
            }
        }
    }
    public Board gameBoard;
    [System.Serializable]
    public class Tile
    {
        public Vector3Int position;
        //Eventually this should not be necessary
        public GameObject piece;
        public int xPos;
        public int yPos;
    }

    [System.Serializable]
    public struct PotentialMove
    {
        public Tile tile;
        public GameObject piece;
        public Board ResultingBoard;
    }

    private void Awake()
    {
        if(whiteTurn == 0 && blackTurn == 0)
        {
            gameType = GameType.PLAYERONLY;
        } else if (whiteTurn == 1 && blackTurn == 1)
        {
            gameType = GameType.AIONLY;
        } else
        {
            gameType = GameType.PLAYERAI;
        }
        for(int x = 0; x < 8; x++)
        {
            for(int y = 0; y < 8; y++) 
            {
                Tile newTile = new Tile();
                newTile.position = new Vector3Int(x * 2, 0, y * 2);
                newTile.xPos = x;
                newTile.yPos = y;
                gameBoard.tiles[x,y] = newTile;
            }
        }
        CheckPiece();
    }

    void Start()
    {
        winText = GameObject.Find("WinText");
        winText.SetActive(false);
        cam.transform.position = camPoint1.transform.position;
        cam.transform.rotation = camPoint1.transform.rotation;
        if (gameType == GameType.PLAYERAI)
        {
            if(blackTurn == 0)
            {
                cam.transform.position = camPoint2.transform.position;
                cam.transform.rotation = camPoint2.transform.rotation;
            }
        }
        turnState = TurnState.PASSIVE;
    }
    
    void CheckPiece()
    {
        for (int x = 0; x < boardLength; x++)
        {
            for (int y = 0; y < boardLength; y++)
            {
                gameBoard.tiles[x, y].piece = null;
                Ray ray = new Ray(gameBoard.tiles[x, y].position, Vector3.up);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject.tag == "Piece")
                    {
                        gameBoard.tiles[x, y].piece = hit.collider.gameObject;
                        gameBoard.tiles[x, y].piece.GetComponent<UnitInfo>().returnPosition = targetPosition;
                        gameBoard.boardState[x, y] = hit.collider.gameObject.GetComponent<UnitInfo>().rep;
                    }
                }
            }
        }
    }

    void NewMove(int x, int y, GameObject piece,Board previousGameBoard)
    {
        Tile potentialMoveTile = gameBoard.tiles[x, y];
        PotentialMove newMove = new PotentialMove();
        newMove.tile = potentialMoveTile;
        newMove.piece = piece;

        Board potentialBoard = new Board();
        foreach(GameObject whiteAdd in previousGameBoard.white)
        {
            potentialBoard.white.Add(whiteAdd);
        }
        foreach(GameObject blackAdd in previousGameBoard.black)
        {
            potentialBoard.black.Add(blackAdd);
        }
        if (newMove.tile.piece != null)
        {
            if (newMove.tile.piece.GetComponent<UnitInfo>().colour == 0)
            {
                potentialBoard.white.Remove(newMove.tile.piece);
            }
            else
            {
                potentialBoard.black.Remove(newMove.tile.piece);
            }
        }
        potentialBoard.GetBoardValue(currentTurn);
        newMove.ResultingBoard = potentialBoard;
        potentialMoves.Add(newMove);
    }

    void Move(GameObject piece,Tile startTile, int distance, int xDir, int yDir)
    {
        for(int i = 1; i <= distance; i++)
        {
            int xOffset = startTile.xPos + xDir * i;
            int yOffset = startTile.yPos + yDir * i;
            if (xOffset < boardLength && xOffset >= 0)
            {
                if(yOffset < boardLength && yOffset >= 0)
                {
                    if(gameBoard.tiles[xOffset, yOffset].piece != null)
                    {
                        if(gameBoard.tiles[xOffset, yOffset].piece.GetComponent<UnitInfo>().colour != currentTurn)
                        {
                            NewMove(xOffset, yOffset, piece,gameBoard);
                            break;
                        } else
                        {
                            break;
                        }
                    } else
                    {
                        NewMove(xOffset, yOffset, piece,gameBoard);
                    }
                } else
                {
                    break;
                }
            } else
            {
                break;
            }
        }
    }

    void IndividualKnightMove(GameObject piece,Tile startTile,int x, int y)
    {
        int xPos = startTile.xPos + x;
        int xPos2 = startTile.xPos - x;
        int yPos = startTile.yPos + y;
        if (yPos < boardLength && yPos >= 0)
        {
            if (xPos >= 0 && xPos < boardLength)
            {
                if (gameBoard.tiles[xPos, yPos].piece == null)
                {
                    NewMove(xPos, yPos, piece,gameBoard);
                }
                else if (gameBoard.tiles[xPos, yPos].piece.GetComponent<UnitInfo>().colour != currentTurn)
                {
                    NewMove(xPos, yPos, piece,gameBoard);
                }
            }
            if (xPos2 < boardLength && xPos2 >= 0)
            {
                if (gameBoard.tiles[xPos2, yPos].piece == null)
                {
                    NewMove(xPos2, yPos, piece,gameBoard);
                }
                else if (gameBoard.tiles[xPos2, yPos].piece.GetComponent<UnitInfo>().colour != currentTurn)
                {
                    NewMove(xPos2, yPos, piece,gameBoard);
                }
            }
        }
    }

    void KnightMove(GameObject piece,Tile startTile)
    {
        IndividualKnightMove(piece,startTile,1, 2);
        IndividualKnightMove(piece,startTile, 2, 1);
        IndividualKnightMove(piece,startTile, -2, -1);
        IndividualKnightMove(piece,startTile, -1, -2);
    }

    void PawnMove(GameObject piece,Tile startTile)
    {
        UnitInfo info = startTile.piece.GetComponent<UnitInfo>();
        int dir;
        int distance;
        if(info.colour == 0)
        {
            dir = 1;
        } else
        {
            dir = -1;
        }
        if (info.moved)
        {
            distance = 1;
        } else
        {
            distance = 2;
        }

        for (int i = 1; i <= distance; i++)
        {
            int xOffset = startTile.xPos + 0 * i;
            int yOffset = startTile.yPos + dir * i;
            if (yOffset < boardLength && yOffset >= 0)
            {
                if (gameBoard.tiles[xOffset, yOffset].piece != null)
                {
                    break;
                }
                else
                {
                    NewMove(xOffset, yOffset, piece,gameBoard);
                }
            }
            else
            {
                break;
            }
        }
        if (startTile.xPos + 1 < boardLength)
        {
            if(startTile.yPos + dir < boardLength && startTile.yPos + dir >= 0)
            {
                if(gameBoard.tiles[startTile.xPos+1, startTile.yPos+dir].piece != null)
                {
                    if (gameBoard.tiles[startTile.xPos + 1, startTile.yPos + dir].piece.GetComponent<UnitInfo>().colour != currentTurn)
                    {
                        NewMove(startTile.xPos + 1, startTile.yPos + dir, piece,gameBoard);
                    }
                }
            }
        }
        if (startTile.xPos - 1 >= 0)
        {
            if (startTile.yPos + dir < boardLength && startTile.yPos + dir >= 0)
            {
                if (gameBoard.tiles[startTile.xPos - 1, startTile.yPos + dir].piece != null)
                {
                    if (gameBoard.tiles[startTile.xPos - 1, startTile.yPos + dir].piece.GetComponent<UnitInfo>().colour != currentTurn)
                    {
                        NewMove(startTile.xPos - 1, startTile.yPos + dir, piece,gameBoard);
                    }
                }
            }
        }
    }

    void SwitchTurn()
    {
        bool blackFound = false;
        bool whiteFound = false;
        foreach(GameObject piece in gameBoard.black)
        {
            UnitInfo info = piece.GetComponent<UnitInfo>();
            if(info.type == UnitInfo.unitType.KING)
            {
                blackFound = true;
                break;
            }
        }
        foreach (GameObject piece in gameBoard.white)
        {
            UnitInfo info = piece.GetComponent<UnitInfo>();
            if (info.type == UnitInfo.unitType.KING)
            {
                whiteFound = true;
                break;
            }
        }
        if (!blackFound)
        {
            gameOver = true;
            winText.SetActive(true);
            winText.GetComponent<Text>().text = "White win!";
        } else if (!whiteFound)
        {
            gameOver = true;
            winText.SetActive(true);
            winText.GetComponent<Text>().text = "Black win!";
        }
        CheckPawn();
        turnCount++;
        currentlySelectedPiece = null;
        turnState = TurnState.PASSIVE;
        aiTurnState = AITurnState.PASSIVE;
        CheckPiece();
    }

    [SerializeField] Tile currentlyHighlighted;
    [SerializeField] GameObject currentlySelectedPiece;

    void TileHighlight()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask layer = LayerMask.GetMask("Board");
        if (Physics.Raycast(ray, out hit,100f,layer))
        {
            Vector3 point = hit.point;
            Vector3Int intPoint = new Vector3Int((int)point.x, 0, (int)point.z);
            if (intPoint.x % 2 != 0)
            {
                intPoint.x++;
            }
            if (intPoint.z % 2 != 0)
            {
                intPoint.z++;
            }

            for(int x = 0; x < boardLength; x++)
            {
                for(int y=0; y < boardLength; y++)
                {
                    if(gameBoard.tiles[x,y].position == intPoint)
                    {
                        currentlyHighlighted = gameBoard.tiles[x,y];
                        break;
                    }
                }
            }
            tileHighlight.SetActive(true);
            tileHighlight.transform.position = currentlyHighlighted.position;
        }
        else
        {
            tileHighlight.SetActive(false);
        }
    }

    void TakePiece(GameObject takenPiece)
    {
        UnitInfo info = takenPiece.GetComponent<UnitInfo>();
        if(info.colour == 0)
        {
            gameBoard.white.Remove(takenPiece);
        } else if(info.colour == 1)
        {
            gameBoard.black.Remove(takenPiece);
        }
        Destroy(takenPiece);
    }

    void CheckPawn()
    {
        CheckPiece();
        for(int x = 0; x < boardLength; x++)
        {
            if(gameBoard.tiles[x,0].piece != null)
            {
                UnitInfo info = gameBoard.tiles[x, 0].piece.GetComponent<UnitInfo>();
                if (info.type == UnitInfo.unitType.PAWN && info.colour == 1)
                {
                    info.type = UnitInfo.unitType.QUEEN;
                    info.basePower = 1000;
                    info.gameObject.GetComponent<MeshFilter>().mesh = blackQueen;
                }
            } if (gameBoard.tiles[x, boardLength-1].piece != null){
                UnitInfo info = gameBoard.tiles[x, boardLength-1].piece.GetComponent<UnitInfo>();
                if (info.type == UnitInfo.unitType.PAWN && info.colour == 0)
                {
                    info.type = UnitInfo.unitType.QUEEN;
                    info.basePower = 1000;
                    info.gameObject.GetComponent<MeshFilter>().mesh = whiteQueen;
                }
            }
        }
    }

    void Deciding()
    {
        if (Input.GetMouseButtonDown(1))
        {
            UnitInfo info = currentlySelectedPiece.GetComponent<UnitInfo>();
            info.targetPosition = info.returnPosition;
            currentlySelectedPiece = null;
            turnState = TurnState.PASSIVE;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            foreach(PotentialMove move in potentialMoves)
            {
                if (move.tile == currentlyHighlighted)
                {
                    if(move.tile.piece != null)
                    {
                        TakePiece(currentlyHighlighted.piece);
                    }
                    UnitInfo info = currentlySelectedPiece.GetComponent<UnitInfo>();
                    info.targetPosition = currentlyHighlighted.position;
                    info.moved = true;
                    turnState = TurnState.CHOSEN;
                    break;
                }
            }
        }
    }

    void SelectUnit()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (currentlyHighlighted != null)
            {
                if (currentlyHighlighted.piece != null)
                {
                    UnitInfo info = currentlyHighlighted.piece.GetComponent<UnitInfo>();
                    if (info.colour == currentTurn)
                    {
                        currentlySelectedPiece = currentlyHighlighted.piece;
                        info.returnPosition = new Vector3(currentlySelectedPiece.transform.position.x,0.2f, currentlySelectedPiece.transform.position.z);
                        info.targetPosition = new Vector3(currentlySelectedPiece.transform.position.x, currentlySelectedPiece.transform.position.y + 3, currentlySelectedPiece.transform.position.z);
                        switch (info.type)
                        {
                            case UnitInfo.unitType.QUEEN:
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 1, 1);
                                Move(currentlySelectedPiece, currentlyHighlighted, boardLength, 0, 1);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 1, 0);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, -1, -1);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 0, -1);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, -1, 0);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, -1, 1);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 1, -1);
                                break;
                            case UnitInfo.unitType.KING:
                                Move(currentlySelectedPiece,currentlyHighlighted, 1, 1, 1);
                                Move(currentlySelectedPiece,currentlyHighlighted, 1, 0, 1);
                                Move(currentlySelectedPiece,currentlyHighlighted, 1, 1, 0);
                                Move(currentlySelectedPiece,currentlyHighlighted, 1, -1, -1);
                                Move(currentlySelectedPiece,currentlyHighlighted, 1, 0, -1);
                                Move(currentlySelectedPiece,currentlyHighlighted, 1, -1, 0);
                                Move(currentlySelectedPiece,currentlyHighlighted, 1, -1, 1);
                                Move(currentlySelectedPiece,currentlyHighlighted, 1, 1, -1);
                                break;
                            case UnitInfo.unitType.BISHOP:
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 1, 1);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, -1, -1);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, -1, 1);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 1, -1);
                                break;
                            case UnitInfo.unitType.ROOK:
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 0, 1);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 1, 0);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, -1, 0);
                                Move(currentlySelectedPiece,currentlyHighlighted, boardLength, 0, -1);
                                break;
                            case UnitInfo.unitType.PAWN:
                                PawnMove(currentlySelectedPiece,currentlyHighlighted);
                                break;
                            case UnitInfo.unitType.KNIGHT:
                                KnightMove(currentlySelectedPiece,currentlyHighlighted);
                                break;
                            default:
                                print("Something is broken");
                                break;
                        }
                        turnState = TurnState.DECIDING;
                    }
                }
            }
        }
        foreach(PotentialMove move in potentialMoves)
        {
            GameObject newMoveTile = Instantiate(moveHighlight);
            newMoveTile.transform.position = move.tile.position;
            movePrefabs.Add(newMoveTile);
        }
    }

    void ClearMoves()
    {
        potentialMoves.Clear();
        foreach(GameObject move in movePrefabs)
        {
            Destroy(move);
        }
    }

    void AIFindMoves()
    {
        List<GameObject> piecesToUse = new List<GameObject>();
        if(currentTurn == 0)
        {
            piecesToUse = gameBoard.white;
        } else
        {
            piecesToUse = gameBoard.black;
        }
        foreach(GameObject piece in piecesToUse)
        {
            UnitInfo info = piece.GetComponent<UnitInfo>();
            Tile tile = new Tile();
            for (int x = 0; x < boardLength; x++)
            {
                for (int y = 0; y < boardLength; y++)
                {
                    if(gameBoard.tiles[x,y].piece == piece)
                    {
                        tile = gameBoard.tiles[x, y];
                    }
                }
            }

            switch (info.type)
            {
                case UnitInfo.unitType.PAWN:
                    PawnMove(piece,tile);
                    break;
                case UnitInfo.unitType.KNIGHT:
                    KnightMove(piece,tile);
                    break;
                case UnitInfo.unitType.QUEEN:
                    Move(piece,tile, boardLength, 1, 1);
                    Move(piece,tile, boardLength, 0, 1);
                    Move(piece, tile, boardLength, 1, 0);
                    Move(piece, tile, boardLength, -1, -1);
                    Move(piece, tile, boardLength, 0, -1);
                    Move(piece, tile, boardLength, -1, 0);
                    Move(piece, tile, boardLength, -1, 1);
                    Move(piece, tile, boardLength, 1, -1);
                    break;
                case UnitInfo.unitType.KING:
                    Move(piece, tile, 1, 1, 1);
                    Move(piece, tile, 1, 0, 1);
                    Move(piece, tile, 1, 1, 0);
                    Move(piece, tile, 1, -1, -1);
                    Move(piece, tile, 1, 0, -1);
                    Move(piece, tile, 1, -1, 0);
                    Move(piece, tile, 1, -1, 1);
                    Move(piece, tile, 1, 1, -1);
                    break;
                case UnitInfo.unitType.BISHOP:
                    Move(piece, tile, boardLength, 1, 1);
                    Move(piece, tile, boardLength, -1, -1);
                    Move(piece, tile, boardLength, -1, 1);
                    Move(piece, tile, boardLength, 1, -1);
                    break;
                case UnitInfo.unitType.ROOK:
                    Move(piece, tile, boardLength, 0, 1);
                    Move(piece, tile, boardLength, 1, 0);
                    Move(piece, tile, boardLength, -1, 0);
                    Move(piece, tile, boardLength, 0, -1);
                    break;
                default:
                    break;
            }
        }
        aiTurnState = AITurnState.DECIDING;
    }

    GameObject AIChosenPiece;

    void AIChooseMove()
    {
        int highest = -999999;
        List<PotentialMove> chosenMoves = new List<PotentialMove>();
        foreach(PotentialMove move in potentialMoves)
        {
            if(move.ResultingBoard.totalBoardValue > highest)
            {
                highest = move.ResultingBoard.totalBoardValue;
            }
        }
        foreach(PotentialMove move in potentialMoves)
        {
            if(move.ResultingBoard.totalBoardValue == highest)
            {
                chosenMoves.Add(move);
            }
        }
        int num = Random.Range(0, chosenMoves.Count-1);
        if (chosenMoves[num].tile.piece != null)
        {
            TakePiece(chosenMoves[num].tile.piece);
        }
        UnitInfo info = chosenMoves[num].piece.GetComponent<UnitInfo>();
        info.targetPosition = chosenMoves[num].tile.position;
        info.moved = true;
        AIChosenPiece = chosenMoves[num].piece;
        chosenMoves.Clear();
        potentialMoves.Clear();
        aiTurnState = AITurnState.CHOSEN;
    }

    void PlayerMoveSequence()
    {
        TileHighlight();
        switch (turnState)
        {
            case TurnState.PASSIVE:
                ClearMoves();
                SelectUnit();
                break;
            case TurnState.DECIDING:
                currentlySelectedPiece.GetComponent<UnitInfo>().targetPosition = new Vector3Int(currentlyHighlighted.position.x, currentlyHighlighted.position.y + 3, currentlyHighlighted.position.z);
                Deciding();
                break;
            case TurnState.CHOSEN:
                ClearMoves();
                if (Vector3.Distance(currentlySelectedPiece.transform.position, currentlySelectedPiece.GetComponent<UnitInfo>().targetPosition) <= 0.1)
                {
                    SwitchTurn();
                }
                break;
            default:
                print("Something has broken");
                break;
        }
    }

    void AITurnSequence()
    {
        tileHighlight.SetActive(false);
        switch (aiTurnState)
        {
            case AITurnState.PASSIVE:
                AIFindMoves();
                break;
            case AITurnState.DECIDING:
                AIChooseMove();
                break;
            case AITurnState.CHOSEN:
                if (Vector3.Distance(AIChosenPiece.transform.position, AIChosenPiece.GetComponent<UnitInfo>().targetPosition) <= 0.1)
                {
                    SwitchTurn();
                }
                break;
            default:
                print("Something has broken");
                break;
        }
    }

    void Update()
    {
        turnText.text = "Turn: " + turnCount;
        if (gameType == GameType.PLAYERONLY)
        {
            cam.transform.position = Vector3.Lerp(cam.transform.position, targetPosition, step * Time.deltaTime);
            cam.transform.rotation = Quaternion.Lerp(cam.transform.rotation, targetRotation, step * Time.deltaTime);
        }
        if (!gameOver)
        {
            if (turnCount % 2 == 0)
            {
                currentTurn = 1;
                targetPosition = camPoint2.transform.position;
                targetRotation = camPoint2.transform.rotation;
            }
            else
            {
                currentTurn = 0;
                targetPosition = camPoint1.transform.position;
                targetRotation = camPoint1.transform.rotation;
            }
            switch (gameType)
            {
                case GameType.PLAYERONLY:
                    PlayerMoveSequence();
                    break;
                case GameType.AIONLY:
                    AITurnSequence();
                    break;
                case GameType.PLAYERAI:
                    if(currentTurn == 0)
                    {
                        if(whiteTurn == 0)
                        {
                            PlayerMoveSequence();
                        } else
                        {
                            AITurnSequence();
                        }
                    } else if (currentTurn == 1)
                    {
                        if(blackTurn == 0)
                        {
                            PlayerMoveSequence();
                        } else
                        {
                            AITurnSequence();
                        }
                    }
                    break;
                default:
                    print("Something has broken");
                    break;
            }
            //if (currentTurn == 0)
            //{
            //    PlayerMoveSequence();
            //}
            //else
            //{
            //    if (gameType == GameType.PLAYERAI)
            //    {
            //        AITurnSequence();
            //    }
            //}
        }
    }
}
