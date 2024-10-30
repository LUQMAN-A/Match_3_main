using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerMatch3 : MonoBehaviour
{
    public ArrayLayout boardLayout;

    [Header("UI Elements")]
    public List<Sprite> pieces;

    public RectTransform gameBoard;
    public RectTransform killedBoard;

    [Header("Power Up")]
    public Sprite freeze;
    public Sprite bomb;
    public int powerUpPercentChance;
    public float freezeTime;
    public int bombRange;
    public int powerUpScoreValue;

    [Header("Prefabs")]
    public GameObject nodePiece;
    public GameObject killedPiece;

    int width = 8;
    int height = 8;
    int[] fills;
    Node[,] board;
    Node[,] simuationBoard;

    List<NodePiece> update;
    List<FlippedPieces> flipped;
    List<NodePiece> dead;
    List<KilledPiece> killed;

    private PowerUp selectedPowerUp;
    private int powerUpVal;
    private GameController controller;

    System.Random random;

    void Start()
    {
        controller = FindObjectOfType<GameController>();
        if (controller == null) Debug.LogError("No Game Controller found!");
        StartGame();
    }

    void Update()
    {
        List<NodePiece> finishedUpdating = new List<NodePiece>();
        for (int i = 0; i < update.Count; i++)
        {
            NodePiece piece = update[i];
            if (!piece.UpdatePiece()) finishedUpdating.Add(piece);
        }
        for (int i = 0; i < finishedUpdating.Count; i++)
        {
            NodePiece piece = finishedUpdating[i];
            FlippedPieces flip = getFlipped(piece);
            NodePiece flippedPiece = null;

            int x = (int)piece.index.x;
            fills[x] = Mathf.Clamp(fills[x] - 1, 0, width);

            List<Point> connected = isConnected(piece.index, true);
            bool wasFlipped = (flip != null);

            if (wasFlipped) //If we flipped to make this update
            {
                flippedPiece = flip.getOtherPiece(piece);
                AddPoints(ref connected, isConnected(flippedPiece.index, true));
            }

            if (connected.Count == 0) //If we didn't make a match
            {
                if (wasFlipped) //If we flipped
                    FlipPieces(piece.index, flippedPiece.index, false); //Flip back
            }
            else //If we made a match
            {
                List<Point> usedConnected = new List<Point>();
                while (connected.Count > 0) //Remove the node pieces connected
                {
                    Point pnt = connected[0];
                    Node node = getNodeAtPoint(pnt);
                    NodePiece nodePiece = node.getPiece();

                    if (getValueAtPoint(pnt) == powerUpVal)
                    {
                        switch (selectedPowerUp)
                        {
                            case PowerUp.Freeze:
                                controller.FreezeTime(freezeTime);
                                break;
                            case PowerUp.Bomb:
                                controller.TriggerExplosionAtLocation(nodePiece.transform.position);
                                int minX = Mathf.Max(pnt.x - bombRange, 0); 
                                int minY = Mathf.Max(pnt.y - bombRange, 0);
                                int maxX = Mathf.Min(pnt.x + bombRange + 1, width);
                                int maxY = Mathf.Min(pnt.y + bombRange + 1, height);
                                int affectedPoints = 0;
                                for (; minX < maxX; ++minX)
                                {
                                    for (int yy = minY; yy < maxY; ++yy)
                                    {
                                        if (connected.Find(p => p.x == minX && p.y == yy) == null &&
                                            usedConnected.Find(p => p.x == minX && p.y == yy) == null)
                                        {
                                            connected.Add(new Point(minX, yy));
                                            affectedPoints++;
                                        }
                                    }
                                }
                                if (affectedPoints > 0)
                                    controller.AddScore(affectedPoints);
                                break;
                        }
                    }
                    usedConnected.Add(pnt);
                    connected.RemoveAt(0);
                    KillPiece(pnt);
                    if (nodePiece != null)
                    {
                        nodePiece.gameObject.SetActive(false);
                        dead.Add(nodePiece);
                    }
                    node.SetPiece(null);
                }

                ApplyGravityToBoard();

                if (!SimulatePossibleMatches()) controller.TriggerGameOver();
            }

            flipped.Remove(flip); //Remove the flip after update
            update.Remove(piece);
        }
    }

    public void ApplyGravityToBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = (height - 1); y >= 0; y--) //Start at the bottom and grab the next
            {
                Point p = new Point(x, y);
                Node node = getNodeAtPoint(p);
                int val = getValueAtPoint(p);
                if (val != 0) continue; //If not a hole, move to the next
                for (int ny = (y - 1); ny >= -1; ny--)
                {
                    Point next = new Point(x, ny);
                    int nextVal = getValueAtPoint(next);
                    if (nextVal == 0)
                        continue;
                    if (nextVal != -1)
                    {
                        Node gotten = getNodeAtPoint(next);
                        NodePiece piece = gotten.getPiece();

                        //Set the hole
                        node.SetPiece(piece);
                        update.Add(piece);

                        //Make a new hole
                        gotten.SetPiece(null);
                    }
                    else//Use dead ones or create new pieces to fill holes (hit a -1) only if we choose to
                    {
                        int newVal = fillPiece();
                        NodePiece piece;
                        Point fallPnt = new Point(x, (-1 - fills[x]));
                        if (dead.Count > 0)
                        {
                            NodePiece revived = dead[0];
                            revived.gameObject.SetActive(true);
                            piece = revived;

                            dead.RemoveAt(0);
                        }
                        else
                        {
                            GameObject obj = Instantiate(nodePiece, gameBoard);
                            NodePiece n = obj.GetComponent<NodePiece>();
                            piece = n;
                        }

                        piece.Initialize(newVal, p, pieces[newVal - 1]);
                        piece.rect.anchoredPosition = getPositionFromPoint(fallPnt);

                        Node hole = getNodeAtPoint(p);
                        hole.SetPiece(piece);
                        ResetPiece(piece);
                        fills[x]++;
                    }
                    break;
                }
            }
        }
    }

    bool SimulatePossibleMatches()
    {
        simuationBoard = new Node[width, height];
        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                simuationBoard[x, y] = new Node(board[x,y].value, new Point(x, y));
            }
        }
        Node tmpNode;
        bool tmpBool = false;
        for (int x = 0; x < width; ++x)
        {
            for (int y = 0; y < height; ++y)
            {
                tmpNode = simuationBoard[x, y];
                if (x+1 != width)
                {
                    simuationBoard[x, y] = simuationBoard[x + 1, y];
                    simuationBoard[x + 1, y] = tmpNode;
                    if (isConnected(new Point(x,y), false, true).Count > 0 || 
                        isConnected(new Point(x+1, y), false, true).Count > 0)
                    {
                        tmpBool = true;
                    }
                    simuationBoard[x + 1, y] = simuationBoard[x, y];
                    simuationBoard[x, y] = tmpNode;
                    if (tmpBool) return tmpBool;
                }
                if (y + 1 != height)
                {
                    simuationBoard[x, y] = simuationBoard[x, y + 1];
                    simuationBoard[x, y + 1] = tmpNode;
                    if (isConnected(new Point(x, y), false, true).Count > 0 ||
                        isConnected(new Point(x, y + 1), false, true).Count > 0)
                    {
                        tmpBool = true;
                    }
                    simuationBoard[x, y + 1] = simuationBoard[x, y];
                    simuationBoard[x, y] = tmpNode;
                    if (tmpBool) return tmpBool;
                }
            }
        }
        return false;
    }

    FlippedPieces getFlipped(NodePiece p)
    {
        FlippedPieces flip = null;
        for (int i = 0; i < flipped.Count; i++)
        {
            if (flipped[i].getOtherPiece(p) != null)
            {
                flip = flipped[i];
                break;
            }
        }
        return flip;
    }

    void StartGame()
    {
        fills = new int[width];
        string seed = getRandomSeed();
        random = new System.Random(seed.GetHashCode());
        update = new List<NodePiece>();
        flipped = new List<FlippedPieces>();
        dead = new List<NodePiece>();
        killed = new List<KilledPiece>();

        selectedPowerUp = PersistentData.PowerUp;
        pieces.Add(selectedPowerUp == PowerUp.Freeze ? freeze : bomb);
        powerUpVal = pieces.Count;

        InitializeBoard();
        VerifyBoard();
        InstantiateBoard();
    }

    void InitializeBoard()
    {
        board = new Node[width, height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                board[x, y] = new Node((boardLayout.rows[y].row[x]) ? -1 : fillPiece(false), new Point(x, y));
            }
        }
    }

    void VerifyBoard()
    {
        List<int> remove;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Point p = new Point(x, y);
                int val = getValueAtPoint(p);
                if (val <= 0) continue;

                remove = new List<int>();
                while (isConnected(p, true).Count > 0)
                {
                    val = getValueAtPoint(p);
                    if (!remove.Contains(val))
                        remove.Add(val);
                    setValueAtPoint(p, newValue(ref remove));
                }
            }
        }
    }

    void InstantiateBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = getNodeAtPoint(new Point(x, y));

                int val = node.value;
                if (val <= 0) continue;
                GameObject p = Instantiate(nodePiece, gameBoard);
                NodePiece piece = p.GetComponent<NodePiece>();
                RectTransform rect = p.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(32 + (64 * x), -32 - (64 * y));
                piece.Initialize(val, new Point(x, y), pieces[val - 1]);
                node.SetPiece(piece);
            }
        }
    }

    public void ResetPiece(NodePiece piece)
    {
        piece.ResetPosition();
        update.Add(piece);
    }

    public void FlipPieces(Point one, Point two, bool main)
    {
        if (getValueAtPoint(one) < 0) return;

        Node nodeOne = getNodeAtPoint(one);
        NodePiece pieceOne = nodeOne.getPiece();
        if (getValueAtPoint(two) > 0)
        {
            Node nodeTwo = getNodeAtPoint(two);
            NodePiece pieceTwo = nodeTwo.getPiece();
            nodeOne.SetPiece(pieceTwo);
            nodeTwo.SetPiece(pieceOne);

            if (main)
                flipped.Add(new FlippedPieces(pieceOne, pieceTwo));

            update.Add(pieceOne);
            update.Add(pieceTwo);
        }
        else
            ResetPiece(pieceOne);
    }

    void KillPiece(Point p)
    {
        List<KilledPiece> available = new List<KilledPiece>();
        for (int i = 0; i < killed.Count; i++)
            if (!killed[i].falling) available.Add(killed[i]);

        KilledPiece set = null;
        if (available.Count > 0)
            set = available[0];
        else
        {
            GameObject kill = GameObject.Instantiate(killedPiece, killedBoard);
            KilledPiece kPiece = kill.GetComponent<KilledPiece>();
            set = kPiece;
            killed.Add(kPiece);
        }

        int val = getValueAtPoint(p) - 1;
        if (set != null && val >= 0 && val < pieces.Count)
            set.Initialize(pieces[val], getPositionFromPoint(p));
    }

    List<Point> isConnected(Point p, bool main, bool useSimBoard = false)
    {
        List<Point> connected = new List<Point>();
        int val = getValueAtPoint(p, useSimBoard);
        if (val == -1) return connected;
        Point[] directions =
        {
            Point.up,
            Point.right,
            Point.down,
            Point.left
        };

        foreach (Point dir in directions) //Checking if there is 2 or more same shapes in the directions
        {
            List<Point> line = new List<Point>();

            int same = 0;
            int checkVal = -1;
            bool otherWasPowerUp = false;
            for (int i = 1; i < 3; i++)
            {
                Point check = Point.add(p, Point.mult(dir, i));
                checkVal = getValueAtPoint(check, useSimBoard);
                if (checkVal == val || val == powerUpVal || checkVal == powerUpVal)
                {
                    line.Add(check);
                    same++;
                    if (checkVal == powerUpVal) otherWasPowerUp = true;
                }
            }

            if (same > 1) //If there are more than 1 of the same shape in the direction then we know it is a match
            {
                if (val != powerUpVal)
                {
                    AddPoints(ref connected, line); //Add these points to the overarching connected list
                    if (main) controller.AddScore(3 + (otherWasPowerUp ? powerUpScoreValue : 0));
                }
                else
                {
                    int val1 = getValueAtPoint(line[0], useSimBoard);
                    int val2 = getValueAtPoint(line[1], useSimBoard);
                    if (val1 == val2 && val1 > 0) //make sure the rest of the line still is the same
                    {
                        AddPoints(ref connected, line);
                        if (main) controller.AddScore(3 + powerUpScoreValue);
                    }
                }
            }
        }

        for (int i = 0; i < 2; i++) //Checking if we are in the middle of two of the same shapes
        {
            List<Point> line = new List<Point>();

            int same = 0;
            int checkVal = -1;
            bool otherWasPowerUp = false;
            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[i + 2]) };
            foreach (Point next in check) //Check both sides of the piece, if they are the same value, add them to the list
            {
                checkVal = getValueAtPoint(next, useSimBoard);
                if (checkVal == val || val == powerUpVal || checkVal == powerUpVal)
                {
                    line.Add(next);
                    same++;
                    if (checkVal == powerUpVal) otherWasPowerUp = true;
                }
            }

            if (same > 1)
            {
                if (val != powerUpVal)
                {
                    AddPoints(ref connected, line);
                    if (main) controller.AddScore(3 + (otherWasPowerUp ? powerUpScoreValue : 0));
                }
                else
                {
                    int val1 = getValueAtPoint(line[0], useSimBoard);
                    int val2 = getValueAtPoint(line[1], useSimBoard);
                    if (val1 == val2 && val1 > 0)
                    {
                        AddPoints(ref connected, line);
                        if (main) controller.AddScore(3 + powerUpScoreValue);
                    }
                }
            }
        }

        for (int i = 0; i < 4; i++) //Check for a 2x2
        {
            List<Point> square = new List<Point>();

            int same = 0;
            int checkVal = -1;
            int next = i + 1;
            if (next >= 4)
                next -= 4;

            Point[] check = { Point.add(p, directions[i]), Point.add(p, directions[next]), Point.add(p, Point.add(directions[i], directions[next])) }; 
            bool otherWasPowerUp = false;

            foreach (Point pnt in check) //Check all sides of the piece, if they are the same value, add them to the list
            {
                checkVal = getValueAtPoint(pnt, useSimBoard);
                if (checkVal == val || val == powerUpVal || checkVal == powerUpVal)
                {
                    square.Add(pnt);
                    same++;
                }
            }

            if (same > 2)
            {
                if (val != powerUpVal)
                {
                    AddPoints(ref connected, square);
                    if (main) controller.AddScore(4 + (otherWasPowerUp ? powerUpScoreValue : 0));
                }
                else
                {
                    int val1 = getValueAtPoint(square[0], useSimBoard);
                    int val2 = getValueAtPoint(square[1], useSimBoard);
                    int val3 = getValueAtPoint(square[2], useSimBoard);
                    if (val1 == val2 && val2 == val3 && val1 > 0)
                    {
                        AddPoints(ref connected, square);
                        if (main) controller.AddScore(4 + powerUpScoreValue);
                    }
                }
            }
        }

        if (main) //Checks for other matches along the current match
        {
            for (int i = 0; i < connected.Count; i++)
                AddPoints(ref connected, isConnected(connected[i], false));
        }

        

        return connected;
    }

    void AddPoints(ref List<Point> points, List<Point> add)
    {
        foreach (Point p in add)
        {
            bool doAdd = true;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Equals(p))
                {
                    doAdd = false;
                    break;
                }
            }

            if (doAdd) points.Add(p);
        }
    }

    int fillPiece(bool allowPowerUp = true)
    {
        var rand = random.Next(0, 100);
        if (allowPowerUp && rand <= powerUpPercentChance)
            return powerUpVal;

        rand = random.Next(0, 100);
        var divider = Mathf.CeilToInt(100f / (pieces.Count-1));
        return (rand / divider) + 1;
    }

    int getValueAtPoint(Point p, bool useSimBoard = false)
    {
        if (p.x < 0 || p.x >= width || p.y < 0 || p.y >= height) return -1;
        return useSimBoard ? simuationBoard[p.x, p.y].value : board[p.x, p.y].value;
    }

    void setValueAtPoint(Point p, int v)
    {
        board[p.x, p.y].value = v;
    }

    Node getNodeAtPoint(Point p)
    {
        return board[p.x, p.y];
    }

    int newValue(ref List<int> remove)
    {
        List<int> available = new List<int>();
        for (int i = 0; i < pieces.Count; i++)
            available.Add(i + 1);
        foreach (int i in remove)
            available.Remove(i);

        if (available.Count <= 0) return 0;
        return available[random.Next(0, available.Count)];
    }

    string getRandomSeed()
    {
        string seed = "";
        string acceptableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdeghijklmnopqrstuvwxyz1234567890!@#$%^&*()";
        for (int i = 0; i < 20; i++)
            seed += acceptableChars[Random.Range(0, acceptableChars.Length)];
        return seed;
    }

    public Vector2 getPositionFromPoint(Point p)
    {
        return new Vector2(32 + (64 * p.x), -32 - (64 * p.y));
    }
    public int GetValPowerUp()
    {
        return powerUpVal;
    }
}

[System.Serializable]
public class Node
{
    public int value; //0 = blank, 1 = cat , 2 = , 3 = , 4 = , 5 = ,6 = , 7 = powerup -1 = hole
    public Point index;
    NodePiece piece;

    public Node(int v, Point i)
    {
        value = v;
        index = i;
    }

    public void SetPiece(NodePiece p)
    {
        piece = p;
        value = (piece == null) ? 0 : piece.value;
        if (piece == null) return;
        piece.SetIndex(index);
    }

    public NodePiece getPiece()
    {
        return piece;
    }
}

[System.Serializable]
public class FlippedPieces
{
    public NodePiece one;
    public NodePiece two;

    public FlippedPieces(NodePiece o, NodePiece t)
    {
        one = o; two = t;
    }

    public NodePiece getOtherPiece(NodePiece p)
    {
        if (p == one)
            return two;
        else if (p == two)
            return one;
        else
            return null;
    }
}
    