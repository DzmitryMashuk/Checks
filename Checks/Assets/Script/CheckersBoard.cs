using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CheckersBoard : MonoBehaviour
{
    public static CheckersBoard Instance { set; get; }

    public Piece[,] pieces = new Piece[8, 8];
    public GameObject whitePiecesPrefab;
    public GameObject blackPiecesPrefab;

    public GameObject alertCanvas1;
    public CanvasGroup alertCanvas;
    private float lastAlert;
    private bool alertActive;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0, 0.5f);

    public bool isWhite;
    private bool isWhiteTurn;
    private bool hasKilled;

    private Piece selectePiece;
    private List<Piece> forcedPieces;

    private Vector2 mouseOver;
    private Vector2 stareDrag;
    private Vector2 endDrag;

    private Client client;

    private void Start()
    {
        Instance = this;
        client = FindObjectOfType<Client>();

        if (client)
        {
            alertCanvas1.SetActive(false);
            isWhite = client.isHost;
            Alert(client.players[0].name + " vs " + client.players[1].name);
        }
        else
        {
            Alert("White player's turn");
        }

        //     isWhite = true;
        isWhiteTurn = true;
        forcedPieces = new List<Piece>();
        GenerateBoard();
    }

    private void Update()
    {

        UpdateMouseOver();
        UpdateAlert();

        if ((isWhite) ? isWhiteTurn : !isWhiteTurn)
        {
            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if (selectePiece != null)
                UpdatePieceDrag(selectePiece);

            if (Input.GetMouseButtonDown(0))
                SelectPiece(x, y);

            if (Input.GetMouseButtonUp(0))
                TryMove((int)stareDrag.x, (int)stareDrag.y, x, y);
        }
    }
    private void UpdateMouseOver()
    {
        if (!Camera.main)
        {
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            mouseOver.x = (int)(hit.point.x - boardOffset.x);
            mouseOver.y = (int)(hit.point.z - boardOffset.z);
        }
        else
        {
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }
    private void UpdatePieceDrag(Piece p)
    {
        //Тень
        if (!Camera.main)
        {
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            p.transform.position = hit.point + Vector3.up;
        }
    }

    private void SelectPiece(int x, int y)
    {
        // Out of bounds
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
            return;

        Piece p = pieces[x, y];
        if (p != null && p.isWhite == isWhite)
        {
            if (forcedPieces.Count == 0)
            {
                selectePiece = p;
                stareDrag = mouseOver;
            }
            else
            {
                if (forcedPieces.Find(fp => fp == p) == null)
                    return;

                selectePiece = p;
                stareDrag = mouseOver;
            }
        }
    }
    public void TryMove(int x1, int y1, int x2, int y2)
    {
        forcedPieces = ScanForPossibleMove();
        // Multiplayer Support
        stareDrag = new Vector2(x1, y1);
        endDrag = new Vector2(x2, y2);
        selectePiece = pieces[x1, y1];

        if (x2 < 0 || x2 >= 8 || y2 < 0 || y2 >= 8)
        {

            if (selectePiece != null)
                MovePiece(selectePiece, x1, y1);

            stareDrag = Vector2.zero;
            selectePiece = null;
            return;
        }

        if (selectePiece != null)
        {
            //Если он не сдвинутлся с места
            if (endDrag == stareDrag)
            {
                MovePiece(selectePiece, x1, y1);
                stareDrag = Vector2.zero;
                selectePiece = null;
                return;
            }

            //Проверка правильности перемещения
            if (selectePiece.ValidMove(pieces, x1, y1, x2, y2))
            {
                if (Mathf.Abs(x2 - x1) == 2)
                {
                    Piece p = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (p != null)
                    {
                        pieces[(x1 + x2) / 2, (y1 + y2) / 2] = null;
                        DestroyImmediate(p.gameObject);//Destroy
                        hasKilled = true;
                    }
                }

                if (forcedPieces.Count != 0 && !hasKilled)
                {
                    MovePiece(selectePiece, x1, y1);
                    stareDrag = Vector2.zero;
                    selectePiece = null;
                    return;
                }

                pieces[x2, y2] = selectePiece;
                pieces[x1, y1] = null;
                MovePiece(selectePiece, x2, y2);
                EndTurn();
            }
            else
            {
                MovePiece(selectePiece, x1, y1);
                stareDrag = Vector2.zero;
                selectePiece = null;
                return;
            }
        }
    } //Destroy (may be)
    private void EndTurn()
    {

        int x = (int)endDrag.x;
        int y = (int)endDrag.y;

        if (selectePiece != null)
        {
            if (selectePiece.isWhite && !selectePiece.isKing && y == 7)
            {
                selectePiece.isKing = true;
                selectePiece.transform.Rotate(Vector3.right * 180);
            }
            else if (!selectePiece.isWhite && !selectePiece.isKing && y == 0)
            {
                selectePiece.isKing = true;
                selectePiece.transform.Rotate(Vector3.right * 180);
            }
        }

        if (client)
        {
            string msg = "CMOV|";
            msg += stareDrag.x.ToString() + "|";
            msg += stareDrag.y.ToString() + "|";
            msg += endDrag.x.ToString() + "|";
            msg += endDrag.y.ToString();

            client.Send(msg);
        }

        selectePiece = null;
        stareDrag = Vector2.zero;

        if (ScanForPossibleMove(selectePiece, x, y).Count != 0 && hasKilled)
            return;

        isWhiteTurn = !isWhiteTurn;
        hasKilled = false;
        CheckVictory();

        if (!client)
        {
            alertCanvas1.SetActive(true);
            isWhite = !isWhite;
            if (isWhite)
                Alert("White player's turn");
            else
                Alert("Black player's turn");
        }
        //else
        //{
        //    //alertCanvas1.SetActive(false);
        //    //if (isWhite)
        //    //    Alert(client.players[0].name + "'s turn");
        //    //else
        //    //    Alert(client.players[1].name + "'s turn");
        //}
    }
    private void CheckVictory()
    {
        var ps = FindObjectsOfType<Piece>();
        bool hasWhite = false, hasBlack = false;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ps[i].isWhite)
                hasWhite = true;
            else
                hasBlack = true;
        }
        if (!hasWhite)
            Victory(false);
        if (!hasBlack)
            Victory(true);
    }
    private void Victory(bool isWhite)
    {
        if (isWhite)
            SceneManager.LoadScene("WhiteWin");// Debug.Log("White win");
        else
            SceneManager.LoadScene("BlackWin");
    }
    private List<Piece> ScanForPossibleMove(Piece p, int x, int y)
    {
        forcedPieces = new List<Piece>();

        if (pieces[x, y].IsForceToMove(pieces, x, y))
            forcedPieces.Add(pieces[x, y]);

        return forcedPieces;
    }
    private List<Piece> ScanForPossibleMove()
    {
        forcedPieces = new List<Piece>();

        for (int i = 0; i < 8; i++)
            for (int j = 0; j < 8; j++)
                if (pieces[i, j] != null && pieces[i, j].isWhite == isWhiteTurn)
                    if (pieces[i, j].IsForceToMove(pieces, i, j))
                        forcedPieces.Add(pieces[i, j]);
        return forcedPieces;
    }

    private void GenerateBoard()
    {
        // Генерация белых
        for (int y = 0; y < 3; y++)
        {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }

        // Генерация черных
        for (int y = 7; y > 4; y--)
        {
            bool oddRow = (y % 2 == 0);
            for (int x = 0; x < 8; x += 2)
            {
                GeneratePiece((oddRow) ? x : x + 1, y);
            }
        }
    }
    private void GeneratePiece(int x, int y)
    {
        bool isPieceWhite = (y > 3) ? false : true;
        GameObject go = Instantiate((isPieceWhite) ? whitePiecesPrefab : blackPiecesPrefab) as GameObject;
        go.transform.SetParent(transform);
        Piece p = go.GetComponent<Piece>();
        pieces[x, y] = p;
        MovePiece(p, x, y);
    }
    private void MovePiece(Piece p, int x, int y)
    {
        p.transform.position = (Vector3.right * x) + (Vector3.forward * y) + boardOffset + pieceOffset;
    }

    public void Alert(string text)
    {
        alertCanvas.GetComponentInChildren<Text>().text = text;
        alertCanvas.alpha = 1;
        lastAlert = Time.time;
        alertActive = true;
    }
    public void UpdateAlert()
    {
        if (alertActive)
        {
            if (Time.time - lastAlert > 1.5f)
            {
                alertCanvas.alpha = 1 - ((Time.time - lastAlert) - 1.5f);

                if (Time.time - lastAlert > 2.5f)
                {
                    alertActive = false;
                }
            }
        }
    }

    public void ChatMessage(string msg)
    {

    }
    public void SendMessage()
    {

    }
}
