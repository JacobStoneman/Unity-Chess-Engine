using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class Controller : MonoBehaviour
{
    const int boardLength = 8;
    int turnCount = 1;
    int currentTurn = 0;
    public int maxDepth;
    int whiteDepth;
    int blackDepth;
    public GameObject moveHighlight;
    public GameObject tileHighlight;
    public GameObject camPoint1;
    public GameObject camPoint2;
    public GameObject cam;
    public Mesh whiteQueen;
    public Mesh blackQueen;
    public Text turnText;
    GameObject winText;
    GameObject backPanel;
    GameObject calcText;
    GameObject valText;
    GameObject kingText;
    Vector3 targetPosition;
    Quaternion targetRotation;
    int numberOfCalcs;
    int AIKingX;
    int AIKingY;
    bool kingSafe = true;
    int turnsSinceTake;

    List<GameObject> movePrefabs = new List<GameObject>();
    bool gameOver;
    bool midGame;
    [SerializeField] bool randomizeStart;
    [SerializeField] bool endlessDebug;
    [SerializeField] int whiteTurn;
    [SerializeField] int blackTurn;
    [SerializeField] public Tile[,] tiles = new Tile[boardLength, boardLength];

    //enum of all available pieces
    public enum unitType
    {
        WHITEPAWN,
        WHITEROOK,
        WHITEKNIGHT,
        WHITEBISHOP,
        WHITEQUEEN,
        WHITEKING,
        BLACKPAWN,
        BLACKROOK,
        BLACKKNIGHT,
        BLACKBISHOP,
        BLACKQUEEN,
        BLACKKING,
        NULL
    }
    //States to control ai and player turns
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
    //Holds all info for a board state
    public class Board
    {
        [SerializeField] public unitType[,] boardState = new unitType[boardLength,boardLength];
        [SerializeField] public List<PotentialMove> potentialMoves = new List<PotentialMove>();
        public bool isMax;
        public int depth;
        public int totalBoardValue;
        public bool evaluated;
        int pawnVal = 100;
        int rookVal = 525;
        int knightVal = 350;
        int bishopVal = 350;
        int queenVal = 1000;
        int kingVal = 10000;
        //Sets the value of the board dependent on which colour is evaluating it
        public void GetBoardValue(int colour)
        {
            for (int x = 0; x < boardLength; x++)
            {
                for (int y = 0; y < boardLength; y++)
                {
                    if (boardState[x, y] != unitType.NULL)
                    {
                        if (colour == 0)
                        {
                            switch (boardState[x, y])
                            {
                                case unitType.NULL:
                                    break;
                                case unitType.WHITEPAWN:
                                    totalBoardValue += pawnVal;
                                    break;
                                case unitType.BLACKPAWN:
                                    totalBoardValue -= pawnVal;
                                    break;
                                case unitType.WHITEKNIGHT:
                                    totalBoardValue += knightVal;
                                    break;
                                case unitType.BLACKKNIGHT:
                                    totalBoardValue -= knightVal;
                                    break;
                                case unitType.WHITEBISHOP:
                                    totalBoardValue += bishopVal;
                                    break;
                                case unitType.BLACKBISHOP:
                                    totalBoardValue -= bishopVal;
                                    break;
                                case unitType.WHITEROOK:
                                    totalBoardValue += rookVal;
                                    break;
                                case unitType.BLACKROOK:
                                    totalBoardValue -= rookVal;
                                    break;
                                case unitType.WHITEQUEEN:
                                    totalBoardValue += queenVal;
                                    break;
                                case unitType.BLACKQUEEN:
                                    totalBoardValue -= queenVal;
                                    break;
                                case unitType.WHITEKING:
                                    totalBoardValue += kingVal;
                                    break;
                                case unitType.BLACKKING:
                                    totalBoardValue -= kingVal;
                                    break;
                                default:
                                    print("Something has broken with case: " + boardState[x, y]);
                                    break;
                            }
                        } else
                        {
                            switch (boardState[x, y])
                            {
                                case unitType.NULL:
                                    break;
                                case unitType.WHITEPAWN:
                                    totalBoardValue -= pawnVal;
                                    break;
                                case unitType.BLACKPAWN:
                                    totalBoardValue += pawnVal;
                                    break;
                                case unitType.WHITEKNIGHT:
                                    totalBoardValue -= knightVal;
                                    break;
                                case unitType.BLACKKNIGHT:
                                    totalBoardValue += knightVal;
                                    break;
                                case unitType.WHITEBISHOP:
                                    totalBoardValue -= bishopVal;
                                    break;
                                case unitType.BLACKBISHOP:
                                    totalBoardValue += bishopVal;
                                    break;
                                case unitType.WHITEROOK:
                                    totalBoardValue -= rookVal;
                                    break;
                                case unitType.BLACKROOK:
                                    totalBoardValue += rookVal;
                                    break;
                                case unitType.WHITEQUEEN:
                                    totalBoardValue -= queenVal;
                                    break;
                                case unitType.BLACKQUEEN:
                                    totalBoardValue += queenVal;
                                    break;
                                case unitType.WHITEKING:
                                    totalBoardValue -= kingVal;
                                    break;
                                case unitType.BLACKKING:
                                    totalBoardValue += kingVal;
                                    break;
                                default:
                                    print("Something has broken with case: " + boardState[x, y]);
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
    public Board gameBoard;
    [System.Serializable]
    public class Tile
    {
        public Vector3Int position;
        public int xPos;
        public int yPos;
    }

    [System.Serializable]
    //Holds all info a potential move that can be made from a given board state
    //As well as the resulting board state of that move
    public struct PotentialMove
    {
        public unitType unitMoved;
        public int newTileX;
        public int newTileY;
        public int prevTileX;
        public int prevTileY;
        public bool pieceTaken;
        public Board ResultingBoard;
    }

    //Initializes board state and determines which game type is being played
    private void Awake()
    {
        MenuController menu = GameObject.Find("MenuController").GetComponent<MenuController>();
        if(menu.mode == 0)
        {
            gameType = GameType.PLAYERONLY;
        } else if (menu.mode == 1)
        {
            gameType = GameType.AIONLY;
        } else
        {
            gameType = GameType.PLAYERAI;
            if (menu.col == 0)
            {
                whiteTurn = 0;
                blackTurn = 1;
            }
            else
            {
                blackTurn = 0;
                whiteTurn = 1;
            }
        }
        maxDepth = menu.diffVal;
        whiteDepth = menu.whiteDiffVal;
        blackDepth = menu.blackDiffVal;
        //Cycles through each tile on the board
        for(int x = 0; x < 8; x++)
        {
            for(int y = 0; y < 8; y++) 
            {
                Tile newTile = new Tile();
                newTile.position = new Vector3Int(x * 2, 0, y * 2);
                newTile.xPos = x;
                newTile.yPos = y;
                tiles[x,y] = newTile;
            }
        }
        CheckPiece();
    }

    void Start()
    {
        if (!randomizeStart)
        {
            midGame = true;
        }
        winText = GameObject.Find("WinText");
        backPanel = GameObject.Find("BackPanel");
        calcText = GameObject.Find("CalcText");
        valText = GameObject.Find("ValueText");
        kingText = GameObject.Find("KingText");
        winText.SetActive(false);
        backPanel.SetActive(false);
        if (gameType == GameType.PLAYERONLY)
        {
            calcText.SetActive(false);
            valText.SetActive(false);
        }
        kingText.SetActive(false);
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
    
    //Fires a raycast from the centre of each tile to determine where the pieces are initially
    void CheckPiece()
    {
        for (int x = 0; x < boardLength; x++)
        {
            for (int y = 0; y < boardLength; y++)
            {
                Ray ray = new Ray(tiles[x, y].position, Vector3.up);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject.tag == "Piece")
                    {
                        gameBoard.boardState[x, y] = hit.collider.gameObject.GetComponent<UnitInfo>().type;
                    }
                } else
                {
                    gameBoard.boardState[x, y] = unitType.NULL;
                }
            }
        }
    }

    //Info on the two tiles the piece is moving between is passed in as well as the board state
    void NewMove(int x, int y, int prevX, int prevY, Board previousGameBoard)
    {
        //Creates a new Move object with this data
        Tile potentialMoveTile = tiles[x, y];
        PotentialMove newMove = new PotentialMove();
        newMove.newTileX = x;
        newMove.newTileY = y;
        newMove.prevTileX = prevX;
        newMove.prevTileY = prevY;

        //Creates the new resulting board state from that move
        Board potentialBoard = new Board();
        for (int x2 = 0; x2 < boardLength; x2++)
        {
            for (int y2 = 0; y2 < boardLength; y2++)
            {
                potentialBoard.boardState[x2, y2] = previousGameBoard.boardState[x2, y2];
            }
        }
        //If a piece is on the tile being moved to then remove it
        if(potentialBoard.boardState[x,y] != unitType.NULL)
        {
            newMove.pieceTaken = true;
        }
        potentialBoard.boardState[prevX, prevY] = unitType.NULL;
        potentialBoard.boardState[x, y] = previousGameBoard.boardState[prevX, prevY];
        newMove.unitMoved = previousGameBoard.boardState[prevX, prevY];
        newMove.ResultingBoard = potentialBoard;
        newMove.ResultingBoard.depth = previousGameBoard.depth + 1;
        newMove.ResultingBoard.isMax = !previousGameBoard.isMax;
        numberOfCalcs++;
        //Add the new move to the passed in boards list of potential moves
        previousGameBoard.potentialMoves.Add(newMove);
    }

    //Calculates if a move is legal based on the given distance and direction
    void Move(int prevX, int prevY, int distance, int xDir, int yDir,Board previousBoard, int colToUse)
    {
        //Checks each tile in a given distance and direction
        for (int i = 1; i <= distance; i++)
        {
            int xOffset = prevX + xDir * i;
            int yOffset = prevY + yDir * i;
            if (xOffset < boardLength && xOffset >= 0)
            {
                if (yOffset < boardLength && yOffset >= 0)
                {
                    //If a piece is found on the tile determine whether it can be taken
                    if (previousBoard.boardState[xOffset,yOffset] != unitType.NULL)
                    {
                        unitType t = previousBoard.boardState[xOffset, yOffset];
                        if (colToUse == 0)
                        {
                            if (t == unitType.BLACKBISHOP || t == unitType.BLACKKING || t == unitType.BLACKKNIGHT || t == unitType.BLACKPAWN || t == unitType.BLACKQUEEN || t == unitType.BLACKROOK)
                            {
                                NewMove(xOffset, yOffset, prevX, prevY, previousBoard);
                                break;
                            } else
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (t == unitType.WHITEBISHOP || t == unitType.WHITEKING || t == unitType.WHITEKNIGHT || t == unitType.WHITEPAWN || t == unitType.WHITEQUEEN || t == unitType.WHITEROOK)
                            {
                                NewMove(xOffset, yOffset, prevX, prevY, previousBoard);
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        NewMove(xOffset, yOffset, prevX, prevY, previousBoard);
                    }
                }
                else
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }
    }

    //Determines legal moves for knight pieces in a given direction
    void IndividualKnightMove(int prevX, int prevY, int x, int y,Board previousBoard, int colToUse)
    {
        int xPos = prevX + x;
        int xPos2 = prevX - x;
        int yPos = prevY + y;
        if (yPos < boardLength && yPos >= 0)
        {
            if (xPos >= 0 && xPos < boardLength)
            {
                if (previousBoard.boardState[xPos, yPos] == unitType.NULL)
                {
                    NewMove(xPos, yPos, prevX, prevY, previousBoard);
                } else
                {
                    unitType t = previousBoard.boardState[xPos, yPos];
                    if (colToUse == 0)
                    {
                        if (t == unitType.BLACKBISHOP || t == unitType.BLACKKING || t == unitType.BLACKKNIGHT || t == unitType.BLACKPAWN || t == unitType.BLACKQUEEN || t == unitType.BLACKROOK)
                        {
                            NewMove(xPos, yPos, prevX, prevY, previousBoard);
                        }
                    }
                    else
                    {
                        if (t == unitType.WHITEBISHOP || t == unitType.WHITEKING || t == unitType.WHITEKNIGHT || t == unitType.WHITEPAWN || t == unitType.WHITEQUEEN || t == unitType.WHITEROOK)
                        {
                            NewMove(xPos, yPos, prevX, prevY, previousBoard);
                        }
                    }
                }
            }
            if (xPos2 < boardLength && xPos2 >= 0)
            {
                if (previousBoard.boardState[xPos2, yPos] == unitType.NULL)
                {
                    NewMove(xPos2, yPos, prevX, prevY, previousBoard);
                }
                else
                {
                    unitType t = previousBoard.boardState[xPos2, yPos];
                    if (currentTurn == 0)
                    {
                        if (t == unitType.BLACKBISHOP || t == unitType.BLACKKING || t == unitType.BLACKKNIGHT || t == unitType.BLACKPAWN || t == unitType.BLACKQUEEN || t == unitType.BLACKROOK)
                        {
                            NewMove(xPos2, yPos, prevX, prevY, previousBoard);
                        }
                    }
                    else
                    {
                        if (t == unitType.WHITEBISHOP || t == unitType.WHITEKING || t == unitType.WHITEKNIGHT || t == unitType.WHITEPAWN || t == unitType.WHITEQUEEN || t == unitType.WHITEROOK)
                        {
                            NewMove(xPos2, yPos, prevX, prevY, previousBoard);
                        }
                    }
                }
            }
        }
    }

    //Checks for legal knight moves in each direction available
    void KnightMove(int prevX, int prevY, Board previousBoard, int colToUse)
    {
        IndividualKnightMove(prevX, prevY, 1, 2,previousBoard,colToUse);
        IndividualKnightMove(prevX, prevY, 2, 1, previousBoard, colToUse);
        IndividualKnightMove(prevX, prevY, -2, -1, previousBoard, colToUse);
        IndividualKnightMove(prevX, prevY, -1, -2, previousBoard, colToUse);
    }

    //Determines legal pawn moves
    void PawnMove(int x, int y,int col,Board previousBoard)
    {
        int dir;
        int distance;
        //Checks if pawn has moved yet this game
        if (col == 0)
        {
            dir = 1;
            if(y == 1)
            {
                distance = 2;
            } else
            {
                distance = 1;
            }
        }
        else
        {
            dir = -1;
            if (y == boardLength-2)
            {
                distance = 2;
            }
            else
            {
                distance = 1;
            }
        }
        //Calculate move given distance and direction
        for (int i = 1; i <= distance; i++)
        {
            int xOffset = x + 0 * i;
            int yOffset = y + dir * i;
            if (yOffset < boardLength && yOffset >= 0)
            {
                if (previousBoard.boardState[xOffset, yOffset] != unitType.NULL)
                {
                    break;
                }
                else
                {
                    NewMove(xOffset, yOffset, x,y, previousBoard);
                }
            }
            else
            {
                break;
            }
        }
        //Checks diagonals for opponent pieces
        if (x + 1 < boardLength)
        {
            if (y + dir < boardLength && y + dir >= 0)
            {
                if (previousBoard.boardState[x + 1, y + dir] != unitType.NULL)
                {
                    unitType t = previousBoard.boardState[x + 1, y + dir];
                    if (currentTurn == 0)
                    {
                        if (t == unitType.BLACKBISHOP || t == unitType.BLACKKING || t == unitType.BLACKKNIGHT || t == unitType.BLACKPAWN || t == unitType.BLACKQUEEN || t == unitType.BLACKROOK)
                        {
                            NewMove(x + 1, y + dir, x, y, previousBoard);
                        }
                    }
                    else
                    {
                        if (t == unitType.WHITEBISHOP || t == unitType.WHITEKING || t == unitType.WHITEKNIGHT || t == unitType.WHITEPAWN || t == unitType.WHITEQUEEN || t == unitType.WHITEROOK)
                        {
                            NewMove(x + 1, y + dir, x, y, previousBoard);
                        }
                    }
                }
            }
        }
        if (x - 1 >= 0)
        {
            if (y + dir < boardLength && y + dir >= 0)
            {
                if (previousBoard.boardState[x - 1, y + dir] != unitType.NULL)
                {
                    unitType t = previousBoard.boardState[x - 1, y + dir];
                    if (currentTurn == 0)
                    {
                        if (t == unitType.BLACKBISHOP || t == unitType.BLACKKING || t == unitType.BLACKKNIGHT || t == unitType.BLACKPAWN || t == unitType.BLACKQUEEN || t == unitType.BLACKROOK)
                        {
                            NewMove(x-1, y+dir, x, y, previousBoard);
                        }
                    }
                    else
                    {
                        if (t == unitType.WHITEBISHOP || t == unitType.WHITEKING || t == unitType.WHITEKNIGHT || t == unitType.WHITEPAWN || t == unitType.WHITEQUEEN || t == unitType.WHITEROOK)
                        {
                            NewMove(x - 1, y + dir, x, y, previousBoard);
                        }
                    }
                }
            }
        }
    }

    bool CheckDiagonals(int x, int y, Board boardState, int colToUse)
    {
        //Looks at each diagonal piece from where the king will move to and determines if this will create a threat
        //Up and right
        for (int i = 1; i <= boardLength; i++)
        {
            if (x + i < boardLength && x + i >= 0)
            {
                if (y + i < boardLength && y + i >= 0)
                {
                    if (boardState.boardState[x + i, y + i] != unitType.NULL)
                    {
                        unitType t = boardState.boardState[x+i, y+i];
                        //If king is white
                        if (colToUse == 0)
                        {
                            if (boardState.boardState[x+i, y + i] != unitType.WHITEKING)
                            {
                                //If there is an enemy king that will take it
                                if (i == 1 && t == unitType.BLACKKING)
                                {
                                    return false;
                                }
                                //If there is an enemy pawn that will take it
                                if (i == 1 && t == unitType.BLACKPAWN)
                                {
                                    if (y + i > y)
                                    {
                                        return false;
                                    }
                                }
                                if (t == unitType.BLACKBISHOP || t == unitType.BLACKQUEEN)
                                {
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        //If king is black
                        else
                        {
                            if (boardState.boardState[x+i, y + i] != unitType.BLACKKING)
                            {
                                //If there is an enemy king that will take it
                                if (i == 1 && t == unitType.WHITEKING)
                                {
                                    return false;
                                }
                                //If there is an enemy pawn that will take it
                                if (i == 1 && t == unitType.WHITEPAWN)
                                {
                                    if (y + i < y)
                                    {
                                        return false;
                                    }
                                }
                                if (t == unitType.WHITEBISHOP || t == unitType.WHITEQUEEN)
                                {
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        //Down and Right
        for (int i = 1; i <= boardLength; i++)
        {
            if (x + i < boardLength && x + i >= 0)
            {
                if (y - i < boardLength && y - i >= 0)
                {
                    if (boardState.boardState[x + i, y - i] != unitType.NULL)
                    {
                        unitType t = boardState.boardState[x + i, y - i];
                        //If king is white
                        if (colToUse == 0)
                        {
                            if (boardState.boardState[x+i, y - i] != unitType.WHITEKING)
                            {
                                //If there is an enemy king that will take it
                                if (i == 1 && t == unitType.BLACKKING)
                                {
                                    return false;
                                }
                                //If there is an enemy pawn that will take it
                                if (i == 1 && t == unitType.BLACKPAWN)
                                {
                                    if (y + i < y)
                                    {
                                        return false;
                                    }
                                }
                                if (t == unitType.BLACKBISHOP || t == unitType.BLACKQUEEN)
                                {
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        //If king is black
                        else
                        {
                            if (boardState.boardState[x+i, y - i] != unitType.BLACKKING)
                            {
                                //If there is an enemy king that will take it
                                if (i == 1 && t == unitType.WHITEKING)
                                {
                                    return false;
                                }
                                //If there is an enemy pawn that will take it
                                if (i == 1 && t == unitType.WHITEPAWN)
                                {
                                    if (y + i > y)
                                    {
                                        return false;
                                    }
                                }
                                if (t == unitType.WHITEBISHOP || t == unitType.WHITEQUEEN)
                                {
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        //Up and left
        for (int i = 1; i <= boardLength; i++)
        {
            if (x - i < boardLength && x - i >= 0)
            {
                if (y + i < boardLength && y + i >= 0)
                {
                    if (boardState.boardState[x - i, y + i] != unitType.NULL)
                    {
                        unitType t = boardState.boardState[x - i, y + i];
                        //If king is white
                        if (colToUse == 0)
                        {
                            if (boardState.boardState[x-i, y + i] != unitType.WHITEKING)
                            {
                                //If there is an enemy king that will take it
                                if (i == 1 && t == unitType.BLACKKING)
                                {
                                    return false;
                                }
                                //If there is an enemy pawn that will take it
                                if (i == 1 && t == unitType.BLACKPAWN)
                                {
                                    if (y + i > y)
                                    {
                                        return false;
                                    }
                                }
                                if (t == unitType.BLACKBISHOP || t == unitType.BLACKQUEEN)
                                {
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        //If king is black
                        else
                        {
                            if (boardState.boardState[x-i, y + i] != unitType.BLACKKING)
                            {
                                //If there is an enemy king that will take it
                                if (i == 1 && t == unitType.WHITEKING)
                                {
                                    return false;
                                }
                                //If there is an enemy pawn that will take it
                                if (i == 1 && t == unitType.WHITEPAWN)
                                {
                                    if (y + i < y)
                                    {
                                        return false;
                                    }
                                }
                                if (t == unitType.WHITEBISHOP || t == unitType.WHITEQUEEN)
                                {
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        //Down and left
        for (int i = 1; i <= boardLength; i++)
        {
            if (x - i < boardLength && x - i >= 0)
            {
                if (y - i < boardLength && y - i >= 0)
                {
                    if (boardState.boardState[x - i, y - i] != unitType.NULL)
                    {
                        unitType t = boardState.boardState[x - i, y - i];
                        //If king is white
                        if (colToUse == 0)
                        {
                            if (boardState.boardState[x-i, y - i] != unitType.WHITEKING)
                            {
                                //If there is an enemy king that will take it
                                if (i == 1 && t == unitType.BLACKKING)
                                {
                                    return false;
                                }
                                //If there is an enemy pawn that will take it
                                if (i == 1 && t == unitType.BLACKPAWN)
                                {
                                    if (y + i < y)
                                    {
                                        return false;
                                    }
                                }
                                if (t == unitType.BLACKBISHOP || t == unitType.BLACKQUEEN)
                                {
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        //If king is black
                        else
                        {
                            if (boardState.boardState[x-i, y - i] != unitType.BLACKKING)
                            {
                                //If there is an enemy king that will take it
                                if (i == 1 && t == unitType.WHITEKING)
                                {
                                    return false;
                                }
                                //If there is an enemy pawn that will take it
                                if (i == 1 && t == unitType.WHITEPAWN)
                                {
                                    if (y + i > y)
                                    {
                                        return false;
                                    }
                                }
                                if (t == unitType.WHITEBISHOP || t == unitType.WHITEQUEEN)
                                {
                                    return false;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        return true;
    }

    bool CheckStraight(int x, int y, Board boardState, int colToUse)
    {
        //Looks at each straight piece from where the king will move to and determines if this will create a threat
        //Up
        for (int i = 1; i < boardLength; i++)
        {
            if (y + i < boardLength && y + i >= 0)
            {
                if (boardState.boardState[x, y + i] != unitType.NULL)
                {
                    unitType t = boardState.boardState[x, y + i];
                    //If king is white
                    if (colToUse == 0)
                    {
                        if (boardState.boardState[x, y + i] != unitType.WHITEKING)
                        {
                            //If there is an enemy king that will take it
                            if (i == 1 && t == unitType.BLACKKING)
                            {
                                return false;
                            }
                            if (t == unitType.BLACKROOK || t == unitType.BLACKQUEEN)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    //If king is black
                    else
                    {
                        if (boardState.boardState[x, y + i] != unitType.BLACKKING)
                        {
                            //If there is an enemy king that will take it
                            if (i == 1 && t == unitType.WHITEKING)
                            {
                                return false;
                            }
                            if (t == unitType.WHITEROOK || t == unitType.WHITEQUEEN)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        //Down
        for (int i = 1; i < boardLength; i++)
        {
            if (y - i < boardLength && y - i >= 0)
            {
                if (boardState.boardState[x, y - i] != unitType.NULL)
                {
                    unitType t = boardState.boardState[x, y - i];
                    //If king is white
                    if (colToUse == 0)
                    {
                        if (boardState.boardState[x, y - i] != unitType.WHITEKING)
                        {
                            //If there is an enemy king that will take it
                            if (i == 1 && t == unitType.BLACKKING)
                            {
                                return false;
                            }
                            if (t == unitType.BLACKROOK || t == unitType.BLACKQUEEN)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    //If king is black
                    else
                    {
                        if (boardState.boardState[x, y - i] != unitType.BLACKKING)
                        {
                            //If there is an enemy king that will take it
                            if (i == 1 && t == unitType.WHITEKING)
                            {
                                return false;
                            }
                            if (t == unitType.WHITEROOK || t == unitType.WHITEQUEEN)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        //Left
        for (int i = 1; i < boardLength; i++)
        {
            if (x - i < boardLength && x - i >= 0)
            {
                if (boardState.boardState[x-i, y] != unitType.NULL)
                {
                    unitType t = boardState.boardState[x-i, y];
                    //If king is white
                    if (colToUse == 0)
                    {
                        if (boardState.boardState[x - 1, y] != unitType.WHITEKING)
                        {
                            //If there is an enemy king that will take it
                            if (i == 1 && t == unitType.BLACKKING)
                            {
                                return false;
                            }
                            if (t == unitType.BLACKROOK || t == unitType.BLACKQUEEN)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    //If king is black
                    else
                    {
                        if (boardState.boardState[x - i, y] != unitType.BLACKKING)
                        {
                            //If there is an enemy king that will take it
                            if (i == 1 && t == unitType.WHITEKING)
                            {
                                return false;
                            }
                            if (t == unitType.WHITEROOK || t == unitType.WHITEQUEEN)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        //Right
        for (int i = 1; i < boardLength; i++)
        {
            if (x + i < boardLength && x + i >= 0)
            {
                if (boardState.boardState[x + i, y] != unitType.NULL)
                {
                    unitType t = boardState.boardState[x + i, y];
                    //If king is white
                    if (colToUse == 0)
                    {
                        if (boardState.boardState[x + i, y] != unitType.WHITEKING) {
                            //If there is an enemy king that will take it
                            if (i == 1 && t == unitType.BLACKKING)
                            {
                                return false;
                            }
                            if (t == unitType.BLACKROOK || t == unitType.BLACKQUEEN)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    //If king is black
                    else
                    {
                        if (boardState.boardState[x + i, y] != unitType.BLACKKING)
                        {
                            //If there is an enemy king that will take it
                            if (i == 1 && t == unitType.WHITEKING)
                            {
                                return false;
                            }
                            if (t == unitType.WHITEROOK || t == unitType.WHITEQUEEN)
                            {
                                return false;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
        return true;
    }

    bool CheckKnights(int x, int y, Board boardState, int colToUse)
    {
        //Checks if a move will result in opponent knights threatening the king
        if(x+1 < boardLength && y+2 < boardLength)
        {
            if(colToUse == 0 && boardState.boardState[x+1,y+2] == unitType.BLACKKNIGHT)
            {
                return false;
            } else if (colToUse == 1 && boardState.boardState[x + 1, y + 2] == unitType.WHITEKNIGHT)
            {
                return false;
            }
        }
        if (x + 2 < boardLength && y + 1 < boardLength)
        {
            if (colToUse == 0 && boardState.boardState[x + 2, y + 1] == unitType.BLACKKNIGHT)
            {
                return false;
            }
            else if (colToUse == 1 && boardState.boardState[x + 2, y + 1] == unitType.WHITEKNIGHT)
            {
                return false;
            }
        }

        if (x - 1 >= 0 && y - 2 >= 0)
        {
            if (colToUse == 0 && boardState.boardState[x - 1, y - 2] == unitType.BLACKKNIGHT)
            {
                return false;
            }
            else if (colToUse == 1 && boardState.boardState[x - 1, y - 2] == unitType.WHITEKNIGHT)
            {
                return false;
            }
        }
        if (x - 2 >= 0 && y - 1 >= 0)
        {
            if (colToUse == 0 && boardState.boardState[x - 2, y - 1] == unitType.BLACKKNIGHT)
            {
                return false;
            }
            else if (colToUse == 1 && boardState.boardState[x - 2, y - 1] == unitType.WHITEKNIGHT)
            {
                return false;
            }
        }

        if (x + 1 < boardLength && y - 2 >= 0)
        {
            if (colToUse == 0 && boardState.boardState[x + 1, y - 2] == unitType.BLACKKNIGHT)
            {
                return false;
            }
            else if (colToUse == 1 && boardState.boardState[x + 1, y - 2] == unitType.WHITEKNIGHT)
            {
                return false;
            }
        }
        if (x + 2 < boardLength && y - 1 >= 0)
        {
            if (colToUse == 0 && boardState.boardState[x + 2, y - 1] == unitType.BLACKKNIGHT)
            {
                return false;
            }
            else if (colToUse == 1 && boardState.boardState[x + 2, y - 1] == unitType.WHITEKNIGHT)
            {
                return false;
            }
        }

        if (x - 1 >= 0 && y + 2 < boardLength)
        {
            if (colToUse == 0 && boardState.boardState[x - 1, y + 2] == unitType.BLACKKNIGHT)
            {
                return false;
            }
            else if (colToUse == 1 && boardState.boardState[x - 1, y + 2] == unitType.WHITEKNIGHT)
            {
                return false;
            }
        }
        if (x - 2 >= 0 && y + 1 < boardLength)
        {
            if (colToUse == 0 && boardState.boardState[x - 2, y + 1] == unitType.BLACKKNIGHT)
            {
                return false;
            }
            else if (colToUse == 1 && boardState.boardState[x - 2, y + 1] == unitType.WHITEKNIGHT)
            {
                return false;
            }
        }

        return true;
    }

    //Kings can only move to a tile if it is still safe afterwards so extra checks are done for determining it a legal move
    void KingMove(int prevX, int prevY, int xDir, int yDir, Board previousBoard, int colToUse)
    {
        //Check move is valid
        bool validMove;
        int xOffset = prevX + xDir;
        int yOffset = prevY + yDir;
        if (xOffset < boardLength && xOffset >= 0)
        {
            if (yOffset >= 0 && yOffset < boardLength)
            {
                //If a piece exists on the tile the king is about to move to
                if(previousBoard.boardState[xOffset, yOffset] != unitType.NULL)
                {
                    unitType t = previousBoard.boardState[xOffset, yOffset];
                    if (colToUse == 0)
                    {
                        if (t == unitType.BLACKBISHOP || t == unitType.BLACKKING || t == unitType.BLACKKNIGHT || t == unitType.BLACKPAWN || t == unitType.BLACKQUEEN || t == unitType.BLACKROOK)
                        {
                            validMove = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        if (t == unitType.WHITEBISHOP || t == unitType.WHITEKING || t == unitType.WHITEKNIGHT || t == unitType.WHITEPAWN || t == unitType.WHITEQUEEN || t == unitType.WHITEROOK)
                        {
                            validMove = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                //If the piece is free
                else
                {
                    validMove = true;
                }
            } else
            {
                return;
            }
        } else
        {
            return;
        }

        if (validMove)
        {
            //If all return true
            //Create the move
            if (CheckDiagonals(xOffset,yOffset,previousBoard,colToUse) && CheckStraight(xOffset, yOffset, previousBoard, colToUse) && CheckKnights(xOffset, yOffset, previousBoard, colToUse))
            {
                NewMove(xOffset, yOffset, prevX, prevY, previousBoard);
            }
        }
    }

    void SwitchTurn()
    {
        //Tidies everything up and resets ready for next turn
        //Switches control to opponent
        if(gameType == GameType.AIONLY)
        {
            if(currentTurn == 0)
            {
                maxDepth = blackDepth;
            } else if(currentTurn == 1)
            {
                maxDepth = whiteDepth;
            }
        }
        bool blackFound = false;
        bool whiteFound = false;
        numberOfCalcs = 0;
        turnsSinceTake++;
        if(turnsSinceTake >= 100)
        {
            winText.SetActive(true);
            backPanel.SetActive(true);
            winText.GetComponent<Text>().text = "Draw!";
            gameOver = true;
        }
        if(turnCount > 6)
        {
            midGame = true;
        }

        for (int x = 0; x < boardLength; x++)
        {
            for (int y = 0; y < boardLength; y++)
            {
                if(gameBoard.boardState[x,y] == unitType.WHITEKING)
                {
                    whiteFound = true;
                } else if(gameBoard.boardState[x, y] == unitType.BLACKKING)
                {
                    blackFound = true;
                }
                if(blackFound && whiteFound)
                {
                    break;
                }
            }
            if (blackFound && whiteFound)
            {
                break;
            }
        }
        if (!blackFound)
        {
            gameOver = true;
            winText.SetActive(true);
            backPanel.SetActive(true);
            winText.GetComponent<Text>().text = "White win!";        
        } else if (!whiteFound)
        {
            gameOver = true;
            winText.SetActive(true);
            backPanel.SetActive(true);
            winText.GetComponent<Text>().text = "Black win!";
        }
        CheckPawn();
        turnCount++;
        currentlySelectedPiece = null;
        turnState = TurnState.PASSIVE;
        aiTurnState = AITurnState.PASSIVE;
    }

    [SerializeField] Tile currentlyHighlighted;
    [SerializeField] GameObject currentlySelectedPiece;

    void TileHighlight()
    {
        //Casts a ray from the cursor to worldpoint
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        LayerMask layer = LayerMask.GetMask("Board");
        if (Physics.Raycast(ray, out hit,100f,layer))
        {
            Vector3 point = hit.point;
            //Resulting worldpoint is converted to whole numbers
            Vector3Int intPoint = new Vector3Int((int)point.x, 0, (int)point.z);
            if (intPoint.x % 2 != 0)
            {
                intPoint.x++;
            }
            if (intPoint.z % 2 != 0)
            {
                intPoint.z++;
            }
            //Highlight is displayed on corresponding tile
            for(int x = 0; x < boardLength; x++)
            {
                for(int y=0; y < boardLength; y++)
                {
                    if(tiles[x,y].position == intPoint)
                    {
                        currentlyHighlighted = tiles[x,y];
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

    void CheckPawn()
    {
        //Checks either end of the board for pawns and converts them to queens
        for (int x = 0; x < boardLength; x++)
        {
            if (gameBoard.boardState[x,0] == unitType.BLACKPAWN)
            {
                UnitInfo info = GetPieceFromPosition(x, 0).GetComponent<UnitInfo>();
                info.type = unitType.BLACKQUEEN;
                info.basePower = 1000;
                gameBoard.boardState[x, 0] = unitType.BLACKQUEEN;
                info.gameObject.GetComponent<MeshFilter>().mesh = blackQueen;
            }
            if(gameBoard.boardState[x, boardLength - 1] == unitType.WHITEPAWN)
            {
                UnitInfo info = GetPieceFromPosition(x, boardLength-1).GetComponent<UnitInfo>();
                info.type = unitType.WHITEQUEEN;
                info.basePower = 1000;
                gameBoard.boardState[x, boardLength - 1] = unitType.WHITEQUEEN;
                info.gameObject.GetComponent<MeshFilter>().mesh = whiteQueen;
            }
        }
    }

    GameObject GetPieceFromPosition(int x, int y)
    {
        //Uses a raycast to find the piece currently on a position on the current game board
        Ray ray = new Ray(tiles[x, y].position, Vector3.up);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject.tag == "Piece")
            {
                return hit.collider.gameObject;
            }
        }
        else
        {
            print("Error");
            return null;
        }
        print("Error");
        return null;
    }

    void Deciding()
    {
        //Called while the player is in the 'deciding' state
        if (Input.GetMouseButtonDown(1))
        {
            //If the player right clicks return any selected piece to its previous position
            UnitInfo info = currentlySelectedPiece.GetComponent<UnitInfo>();
            info.targetPosition = info.returnPosition;
            currentlySelectedPiece = null;
            //Return the player to the passive state
            turnState = TurnState.PASSIVE;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            //If the player left clicks check if it is on a tile in the list of potential moves
            foreach (PotentialMove move in gameBoard.potentialMoves)
            {
                if (move.newTileX == currentlyHighlighted.xPos && move.newTileY == currentlyHighlighted.yPos)
                {
                    //If so then execute the move and remove any taken piece
                    if (move.pieceTaken == true)
                    {
                        Destroy(GetPieceFromPosition(move.newTileX, move.newTileY));
                        turnsSinceTake = 0;
                    }
                    gameBoard = move.ResultingBoard;
                    UnitInfo info = currentlySelectedPiece.GetComponent<UnitInfo>();
                    info.targetPosition = currentlyHighlighted.position;
                    info.moved = true;
                    //set player to the chosen state
                    turnState = TurnState.CHOSEN;
                    break;
                }
            }
        }
    }

    void SelectUnit()
    {
        //Called when in the passive state
        if (Input.GetMouseButtonDown(0))
        {
            //If they left click and they are highlighting a unit
            if (currentlyHighlighted != null)
            {
                if (gameBoard.boardState[currentlyHighlighted.xPos, currentlyHighlighted.yPos] != unitType.NULL)
                {
                    UnitInfo info = GetPieceFromPosition(currentlyHighlighted.xPos, currentlyHighlighted.yPos).GetComponent<UnitInfo>();
                    if (info.colour == currentTurn)
                    {
                        //Select that piece, calculate and display all available moves for that piece
                        currentlySelectedPiece = info.gameObject;
                        info.returnPosition = new Vector3(currentlySelectedPiece.transform.position.x, 0.2f, currentlySelectedPiece.transform.position.z);
                        info.targetPosition = new Vector3(currentlySelectedPiece.transform.position.x, currentlySelectedPiece.transform.position.y + 3, currentlySelectedPiece.transform.position.z);
                        switch (gameBoard.boardState[currentlyHighlighted.xPos, currentlyHighlighted.yPos])
                        {
                            case unitType.WHITEQUEEN:
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, 1, gameBoard,currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 0, -1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, 0, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, -1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 0, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, 0, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, -1, gameBoard, currentTurn);
                                break;
                            case unitType.BLACKQUEEN:
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 0, -1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, 0, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, -1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 0, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, 0, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, -1, gameBoard, currentTurn);
                                break;
                            case unitType.WHITEKING:
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 1, 1,gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 0, -1,gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 1, 0,gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, -1, -1,gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 0, 1,gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, -1, 0,gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, -1, 1,gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 1, -1,gameBoard, currentTurn);
                                break;
                            case unitType.BLACKKING:
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 1, 1, gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 0, -1, gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 1, 0, gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, -1, -1, gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 0, 1, gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, -1, 0, gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, -1, 1, gameBoard, currentTurn);
                                KingMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 1, -1, gameBoard, currentTurn);
                                break;
                            case unitType.WHITEBISHOP:
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, -1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, -1, gameBoard, currentTurn);
                                break;
                            case unitType.BLACKBISHOP:
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, -1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, -1, gameBoard, currentTurn);
                                break;
                            case unitType.WHITEROOK:
                                Move(currentlyHighlighted.xPos,currentlyHighlighted.yPos, boardLength, 0, 1,gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, 0, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, 0, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 0, -1, gameBoard, currentTurn);
                                break;
                            case unitType.BLACKROOK:
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 0, 1, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 1, 0, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, -1, 0, gameBoard, currentTurn);
                                Move(currentlyHighlighted.xPos, currentlyHighlighted.yPos, boardLength, 0, -1, gameBoard, currentTurn);
                                break;
                            case unitType.WHITEPAWN:
                                PawnMove(currentlyHighlighted.xPos,currentlyHighlighted.yPos, 0, gameBoard);
                                break;
                            case unitType.BLACKPAWN:
                                PawnMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, 1, gameBoard);
                                break;
                            case unitType.WHITEKNIGHT:
                                KnightMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, gameBoard,0);
                                break;
                            case unitType.BLACKKNIGHT:
                                KnightMove(currentlyHighlighted.xPos, currentlyHighlighted.yPos, gameBoard,1);
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
        //Create purple tiles to indicate a potential move
        foreach (PotentialMove move in gameBoard.potentialMoves)
        {
            GameObject newMoveTile = Instantiate(moveHighlight);
            newMoveTile.transform.position = tiles[move.newTileX,move.newTileY].position;
            movePrefabs.Add(newMoveTile);
        }
    }

    void ClearMoves()
    {
        gameBoard.potentialMoves.Clear();
        //Clears any purple tiles
        foreach (GameObject move in movePrefabs)
        {
            Destroy(move);
        }
        //Clears memory used when created the tree of data for minimax
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    //given a board state, find all the available moves for a given team
    void AIFindMoves(Board previousBoard, int colourToUse, int depth)
    {
        //Cycles through each tile on the board and if holds a piece for the given team, calculates its moves
        for (int x = 0; x < boardLength; x++)
        {
            for (int y = 0; y < boardLength; y++)
            {
                if(previousBoard.boardState[x,y] != unitType.NULL)
                {
                    unitType t = previousBoard.boardState[x, y];
                    if (colourToUse == 0)
                    {
                        if (t == unitType.WHITEBISHOP || t == unitType.WHITEKING || t == unitType.WHITEKNIGHT || t == unitType.WHITEPAWN || t == unitType.WHITEQUEEN || t == unitType.WHITEROOK)
                        {
                            switch (t)
                            {
                                case unitType.WHITEQUEEN:
                                    Move(x, y, boardLength, 1, 1, previousBoard,colourToUse);
                                    Move(x, y, boardLength, 0, -1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 1, 0, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, -1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 0, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, 0, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 1, -1, previousBoard, colourToUse);
                                    break;
                                case unitType.WHITEKING:
                                    KingMove(x, y, 1, 1, previousBoard, colourToUse);
                                    KingMove(x, y, 0, -1, previousBoard, colourToUse);
                                    KingMove(x, y, 1, 0, previousBoard, colourToUse);
                                    KingMove(x, y, -1, -1, previousBoard, colourToUse);
                                    KingMove(x, y, 0, 1, previousBoard, colourToUse);
                                    KingMove(x, y, -1, 0, previousBoard, colourToUse);
                                    KingMove(x, y, -1, 1, previousBoard, colourToUse);
                                    KingMove(x, y, 1, -1, previousBoard, colourToUse);
                                    break;
                                case unitType.WHITEBISHOP:
                                    Move(x, y, boardLength, 1, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, -1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 1, -1, previousBoard, colourToUse);
                                    break;
                                case unitType.WHITEROOK:
                                    Move(x, y, boardLength, 0, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 1, 0, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, 0, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 0, -1, previousBoard, colourToUse);
                                    break;
                                case unitType.WHITEPAWN:
                                    PawnMove(x, y, 0, previousBoard);
                                    break;
                                case unitType.WHITEKNIGHT:
                                    KnightMove(x, y, previousBoard,0);
                                    break;
                                default:
                                    print("Something is broken");
                                    break;
                            }
                        }
                    }
                    else
                    {
                        if (t == unitType.BLACKBISHOP || t == unitType.BLACKKING || t == unitType.BLACKKNIGHT || t == unitType.BLACKPAWN || t == unitType.BLACKQUEEN || t == unitType.BLACKROOK)
                        {
                            switch (t)
                            {
                                case unitType.BLACKQUEEN:
                                    Move(x, y, boardLength, 1, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 0, -1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 1, 0, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, -1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 0, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, 0, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 1, -1, previousBoard, colourToUse);
                                    break;
                                case unitType.BLACKKING:
                                    KingMove(x, y, 1, 1, previousBoard, colourToUse);
                                    KingMove(x, y, 0, -1, previousBoard, colourToUse);
                                    KingMove(x, y, 1, 0, previousBoard, colourToUse);
                                    KingMove(x, y, -1, -1, previousBoard, colourToUse);
                                    KingMove(x, y, 0, 1, previousBoard, colourToUse);
                                    KingMove(x, y, -1, 0, previousBoard, colourToUse);
                                    KingMove(x, y, -1, 1, previousBoard, colourToUse);
                                    KingMove(x, y, 1, -1, previousBoard, colourToUse);
                                    break;
                                case unitType.BLACKBISHOP:
                                    Move(x, y, boardLength, 1, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, -1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 1, -1, previousBoard, colourToUse);
                                    break;
                                case unitType.BLACKROOK:
                                    Move(x, y, boardLength, 0, 1, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 1, 0, previousBoard, colourToUse);
                                    Move(x, y, boardLength, -1, 0, previousBoard, colourToUse);
                                    Move(x, y, boardLength, 0, -1, previousBoard, colourToUse);
                                    break;
                                case unitType.BLACKPAWN:
                                    PawnMove(x, y, 1, previousBoard);
                                    break;
                                case unitType.BLACKKNIGHT:
                                    KnightMove(x, y, previousBoard,1);
                                    break;
                                default:
                                    print("Something is broken");
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }

    GameObject AIChosenPiece;

    void AIChooseMove(bool kingSafe)
    {
        //The resulting list of available moves after minimax can now be cycled through to find the highest
        int highest = -999999;
        List<PotentialMove> chosenMoves = new List<PotentialMove>();
        foreach (PotentialMove move in gameBoard.potentialMoves)
        {
            if (move.ResultingBoard.totalBoardValue > highest)
            {
                highest = move.ResultingBoard.totalBoardValue;
            }
        }
        foreach (PotentialMove move in gameBoard.potentialMoves)
        {
            if (move.ResultingBoard.totalBoardValue == highest)
            {
                if (kingSafe)
                {
                    chosenMoves.Add(move);
                } else
                {
                    for (int x = 0; x < boardLength; x++)
                    {
                        for (int y = 0; y < boardLength; y++)
                        {
                            //position of the king is stored to easily determine safety
                            if (move.ResultingBoard.boardState[x, y] == unitType.WHITEKING && currentTurn == 0)
                            {
                                AIKingX = x;
                                AIKingY = y;
                            }
                            else if (move.ResultingBoard.boardState[x, y] == unitType.BLACKKING && currentTurn == 1)
                            {
                                AIKingX = x;
                                AIKingY = y;
                            }
                        }
                    }
                    if (!CheckDiagonals(AIKingX, AIKingY, move.ResultingBoard, currentTurn) || !CheckStraight(AIKingX, AIKingY, move.ResultingBoard, currentTurn) || !CheckKnights(AIKingX, AIKingY, move.ResultingBoard, currentTurn))
                    {
                        //This move is not safe for the king in the resulting board state
                    } else
                    {
                        chosenMoves.Add(move);
                    }
                }
            }
        }
        //If there are multiple moves of the same value choose one at random
        if (chosenMoves.Count != 0)
        {
            int num = UnityEngine.Random.Range(0, chosenMoves.Count - 1);
            if (chosenMoves[num].pieceTaken == true)
            {
                Destroy(GetPieceFromPosition(chosenMoves[num].newTileX, chosenMoves[num].newTileY));
                turnsSinceTake = 0;
            }
            gameBoard = chosenMoves[num].ResultingBoard;
            valText.GetComponent<Text>().text = "Value chosen: " + chosenMoves[num].ResultingBoard.totalBoardValue;
            UnitInfo info = GetPieceFromPosition(chosenMoves[num].prevTileX, chosenMoves[num].prevTileY).GetComponent<UnitInfo>();
            info.targetPosition = tiles[chosenMoves[num].newTileX, chosenMoves[num].newTileY].position;
            info.moved = true;
            AIChosenPiece = info.gameObject;
            chosenMoves.Clear();
        }
        //If there are no legal moves then the game has ended
        else
        {
            winText.SetActive(true);
            backPanel.SetActive(true);
            winText.GetComponent<Text>().text = "CheckMate!";
            gameOver = true;
        }
        aiTurnState = AITurnState.CHOSEN;
    }

    //State machine controlling players turn
    void PlayerMoveSequence()
    {
        TileHighlight();
        switch (turnState)
        {
            case TurnState.PASSIVE:
                ClearMoves();
                for (int x = 0; x < boardLength; x++)
                {
                    for (int y = 0; y < boardLength; y++)
                    {
                        //Displays warning text if king is in check
                        if(gameBoard.boardState[x,y] == unitType.WHITEKING && currentTurn == 0)
                        {
                            if(!CheckDiagonals(x,y,gameBoard,currentTurn) || !CheckStraight(x, y, gameBoard, currentTurn) || !CheckKnights(x, y, gameBoard, currentTurn))
                            {
                                kingText.SetActive(true);
                            } else
                            {
                                kingText.SetActive(false);
                            }
                            break;
                        }
                        else if (gameBoard.boardState[x, y] == unitType.BLACKKING && currentTurn == 1)
                        {
                            if (!CheckDiagonals(x, y, gameBoard, currentTurn) || !CheckStraight(x, y, gameBoard, currentTurn) || !CheckKnights(x, y, gameBoard, currentTurn))
                            {
                                kingText.SetActive(true);
                            }
                            else
                            {
                                kingText.SetActive(false);
                            }
                            break;
                        }
                    }
                }
                SelectUnit();
                break;
            case TurnState.DECIDING:
                //Raises the currently selected piece
                currentlySelectedPiece.GetComponent<UnitInfo>().targetPosition = new Vector3Int(currentlyHighlighted.position.x, currentlyHighlighted.position.y + 3, currentlyHighlighted.position.z);
                Deciding();
                break;
            case TurnState.CHOSEN:
                //Moves piece to new position and switches turn
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

    //Builds a tree structure of all possible board states stemming from current board to a given depth
    void GenerateAllBoardStates(int depth, int maxDepth,int col, Board boardToGen)
    {
        if (depth != maxDepth)
        {
            int newCol = 0;
            if (col == 0)
            {
                newCol = 1;
            }
            //Find all moves possible with given boardstate
            AIFindMoves(boardToGen, col, depth);
            //If we havent reached max depth then recall the function for each potential move
            foreach (PotentialMove move in boardToGen.potentialMoves)
            {
                GenerateAllBoardStates(depth+1, maxDepth,newCol, move.ResultingBoard);
            }
        }   
    }

    //State machine for AI turn
    void AITurnSequence()
    {
        tileHighlight.SetActive(false);
        switch (aiTurnState)
        {
            case AITurnState.PASSIVE:
                if (midGame)
                {
                    //Find position of the king
                    for (int x = 0; x < boardLength; x++)
                    {
                        for (int y = 0; y < boardLength; y++)
                        {
                            if (gameBoard.boardState[x, y] == unitType.WHITEKING && currentTurn == 0)
                            {
                                AIKingX = x;
                                AIKingY = y;
                            }
                            else if (gameBoard.boardState[x, y] == unitType.BLACKKING && currentTurn == 1)
                            {
                                AIKingX = x;
                                AIKingY = y;
                            }
                        }
                    }
                    //Determine if king was put in danger last turn
                    if (!CheckDiagonals(AIKingX, AIKingY, gameBoard, currentTurn) || !CheckStraight(AIKingX, AIKingY, gameBoard, currentTurn) || !CheckKnights(AIKingX, AIKingY, gameBoard, currentTurn))
                    {
                        kingSafe = false;
                    } 
                    //If the king is currently safe then run the minimax algorithm
                    else
                    {
                        kingSafe = true;
                    }
                    if (kingSafe)
                    {
                        GenerateAllBoardStates(0, maxDepth, currentTurn, gameBoard);
                        FillBottomRow(gameBoard);
                        for (int i = 0; i < maxDepth; i++)
                        {
                            FillRestOfTree(gameBoard, true);
                        }
                    } 
                    //Otherwise find moves on current boardstate that protect the king
                    else
                    {
                        AIFindMoves(gameBoard, currentTurn, 0);
                    }
                } else
                {
                    AIFindMoves(gameBoard, currentTurn, 0);
                }
                calcText.GetComponent<Text>().text = "Moves Calculated: " + numberOfCalcs;
                aiTurnState = AITurnState.DECIDING;
                break;
            case AITurnState.DECIDING:
                AIChooseMove(kingSafe);
                break;
            case AITurnState.CHOSEN:
                ClearMoves();
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

    //Fills the bottom row of the tree with each boardstates board value for a given team
    public void FillBottomRow(Board prevBoard)
    {
        foreach(PotentialMove move in prevBoard.potentialMoves)
        {
            //If the move being looked at has no potential moves then it is at the bottom of the tree and should be evaluated
            if(move.ResultingBoard.potentialMoves.Count == 0)
            {
                move.ResultingBoard.GetBoardValue(currentTurn);
                move.ResultingBoard.evaluated = true;
            }
            //otherwise recall the function a level lower
            else
            {
                FillBottomRow(move.ResultingBoard);
            }
        }
    }

    //Returns either the highest or lowest value of a set of moves
    int HighLow(List<PotentialMove> moves, bool isMax)
    {
        int value = -99999;
        if (isMax == true)
        {
            int highest = -99999999;
            foreach(PotentialMove move in moves)
            {
                if(move.ResultingBoard.totalBoardValue > highest)
                {
                    highest = move.ResultingBoard.totalBoardValue;
                }
            }
            value = highest;
        } else if (isMax == false)
        {
            int lowest = 99999999;
            foreach (PotentialMove move in moves)
            {
                if (move.ResultingBoard.totalBoardValue < lowest)
                {
                    lowest = move.ResultingBoard.totalBoardValue;
                }
            }
            value = lowest;
        }

        return value;
    }

    //With the bottom layer evaluated the minimax algorithm can bring either the highest or lowest values up the tree
    //dependent on whether it would be black or whites turn
    public void FillRestOfTree(Board prevBoard, bool isMax) 
    { 
        //check if potential moves of passed in board state have been evaluated
        foreach(PotentialMove move in prevBoard.potentialMoves)
        {
            //if it has not been evaluated
            if (!move.ResultingBoard.evaluated)
            {
                if (move.ResultingBoard.potentialMoves[0].ResultingBoard.evaluated)
                {
                    move.ResultingBoard.totalBoardValue = HighLow(move.ResultingBoard.potentialMoves, move.ResultingBoard.isMax);
                    move.ResultingBoard.evaluated = true;
                } else if(move.ResultingBoard.potentialMoves[0].ResultingBoard.evaluated == false)
                {
                    FillRestOfTree(move.ResultingBoard, move.ResultingBoard.isMax);
                }
            }
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
                    if (currentTurn == 0)
                    {
                        if (whiteTurn == 0)
                        {
                            PlayerMoveSequence();
                        }
                        else
                        {
                            AITurnSequence();
                        }
                    }
                    else if (currentTurn == 1)
                    {
                        if (blackTurn == 0)
                        {
                            PlayerMoveSequence();
                        }
                        else
                        {
                            AITurnSequence();
                        }
                    }
                    break;
                default:
                    print("Something has broken");
                    break;
            }
        } else if (endlessDebug)
        {
            Destroy(GameObject.Find("MenuController"));
            SceneManager.UnloadScene("SampleScene");
            SceneManager.LoadScene("Menu");
        }
    }
    public void Quit()
    {
        Destroy(GameObject.Find("MenuController"));
        SceneManager.UnloadScene("SampleScene");
        SceneManager.LoadScene("Menu");
    }
}
