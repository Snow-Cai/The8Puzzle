using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("GameBoard")]
    public int numTiles = 3;
    public RectTransform boardParent;
    public GridLayoutGroup gridLayout;
    public Tile tilePrefab;

    private List<Tile> tiles = new List<Tile>();
    private int[] state;
    private int[] goal;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BuildGoal();
        BuildBoard(); // sets up the tiles in a grid
        NewGame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void BuildGoal()
    {
        goal = new int[numTiles * numTiles];
        for (int i = 0; i < numTiles*numTiles; i++)
        {
            goal[i] = i + 1;
        }
        goal[numTiles*numTiles - 1] = 0; // last is blank
    }

    void BuildBoard()
    {
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);
        tiles.Clear();

        var size = Mathf.Min(boardParent.rect.width, boardParent.rect.height);
        var cell = Mathf.FloorToInt(size / numTiles);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = numTiles;
        gridLayout.cellSize = new Vector2(cell, cell);
        gridLayout.spacing = new Vector2(8, 8);
        gridLayout.padding.left = gridLayout.padding.right = gridLayout.padding.top = gridLayout.padding.bottom = 8;

        for (int i = 0; i < numTiles * numTiles; i++)
            tiles.Add(Instantiate(tilePrefab, boardParent));
    }

    public void NewGame() //make sure it's solvable
    {
        //if (tiles.Count != numTiles * numTiles)
        //    BuildBoard();

        state = (int[])goal.Clone();
        state = ShuffleByRandomMoves(state, numTiles, 50);
        ApplyToUI();
    }

    void ApplyToUI()
    {
        for (int i = 0; i < state.Length; i++)
            tiles[i].Init(state[i], OnTileClicked);
        if (IsGoal()) Debug.Log("Solved!");
    }

    void OnTileClicked(Tile tile)
    {
        int indx = tiles.IndexOf(tile);
        int blank = System.Array.IndexOf(state, 0);

        int br = blank / numTiles, bc = blank % numTiles, r = indx / numTiles, c = indx % numTiles;
        if (Mathf.Abs(br - r) + Mathf.Abs(bc - c) == 1) // Manhattan distance formula for adjacent
        {
            // swap tile with blank
            (state[blank], state[indx]) = (state[indx], state[blank]);
            ApplyToUI();
        }
    }

    bool IsGoal() // win check
    {
        for (int i = 0; i < state.Length; i++)
        {
            if (state[i] != goal[i]) return false;
        }
        return true;
    }

    // helpers
    struct Swap // swap a and b
    { 
        public int a, b; 
        public Swap(int a, int b) 
        { 
            this.a = a; 
            this.b = b; 
        } 
    } 

    List<Swap> MovableTiles(int[] s, int n)
    {
        var list = new List<Swap>();
        int blank = System.Array.IndexOf(s, 0);
        int r = blank / n, c = blank % n;
        if (r > 0) list.Add(new Swap(blank, blank - n));        // up
        if (r < n - 1) list.Add(new Swap(blank, blank + n));    // down
        if (c > 0) list.Add(new Swap(blank, blank - 1));        // left
        if (c < n - 1) list.Add(new Swap(blank, blank + 1));    // right

        return list;
    }

    int[] Apply(int[] s, Swap m)
    {
        var t = (int[])s.Clone();
        (t[m.a], t[m.b]) = (t[m.b], t[m.a]);
        return t;
    }

    int[] ShuffleByRandomMoves(int[] s0, int n, int k)
    {
        var s = (int[])s0.Clone();
        var rand = new System.Random();
        int lastBlank = System.Array.IndexOf(s, 0);

        for (int i = 0; i < k; i++)
        {
            var moves = MovableTiles(s, n);
            if (moves.Count > 0) moves.RemoveAll(st => st.b == lastBlank);
            var pick = moves[rand.Next(moves.Count)];
            lastBlank = pick.a;
            s = Apply(s, pick);
        }
        return s;
    }
}
