// Name: Snow Cai
// Email: snowc@unr.edu

using System;
using System.Collections;
using System.Collections.Concurrent;
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

    [Header("AI Solve")]
    [SerializeField] private int difficultyLevel = 0; // default 3x3
    [SerializeField] private TMPro.TMP_Text difficultyLabel;
    [SerializeField] private int shuffleMoves = 10;
    private bool isSolving = false;

    [Header("BFS Test Settings")]
    public int test3 = 12;
    public int test4 = 15;
    public int test5 = 20;
    public int test6 = 26;

    [Header("Game HUD")]
    public System.Action onPlayerMove;
    public System.Action onPlayerSolved;
    public System.Action onPlayerPressedSolve;
    public System.Action onPlayerHint;

    [Header("Visuals")]
    public bool useImage = true;               
    public bool overlayNumbersInImageMode = true; 
    public Texture2D puzzleImage;              
    public Color blankTint = new Color(1, 1, 1, 0);
    public Color tileTint = Color.white;        
    private Sprite[] slicedSprites;             // length = numTiles*numTiles (last = null for blank)
    public Texture2D[] puzzleImages;
    [SerializeField] Color imageBorderColor = new Color(0, 0, 0, 0.5f);
    [SerializeField] float imageBorderThickness = 2f;
    public UnityEngine.UI.Image boardBackground;

    public VerificationManager verificationManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int startSize = BoardSizeForDifficulty(difficultyLevel);
        numTiles = startSize; // ensure board is set to initial difficulty size

        BuildGoal();
        BuildBoard(); // sets up the tiles in a grid
        SliceImageForSize(numTiles);
        NewGame();

        if (difficultyLabel != null)
        {
            difficultyLabel.text = DifficultyName(difficultyLevel);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    void BuildGoal()
    {
        goal = new int[numTiles * numTiles];
        for (int i = 0; i < numTiles * numTiles; i++)
        {
            goal[i] = i + 1;
        }
        goal[numTiles * numTiles - 1] = 0; // last is blank
    }

    void BuildBoard()
    {
        // clear old
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);
        tiles.Clear();

        // available square inside boardParent
        int outerMargin = 5;
        float availW = boardParent.rect.width - 2 * outerMargin;
        float availH = boardParent.rect.height - 2 * outerMargin;
        float square = Mathf.Min(availW, availH);

        // seamless in image mode (no padding / spacing)
        int pad = 8;
        int gap = 8;

        // compute exact cell so: n*cell + (n-1)*gap + 2*pad == square (floored to int px)
        float cell = Mathf.Floor((square - 2 * pad - (numTiles - 1) * gap) / numTiles);

        // configure grid
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = numTiles;
        gridLayout.cellSize = new Vector2(cell, cell);
        gridLayout.spacing = new Vector2(gap, gap);
        gridLayout.padding.left = gridLayout.padding.right = gridLayout.padding.top = gridLayout.padding.bottom = pad;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // spawn tiles
        for (int i = 0; i < numTiles * numTiles; i++)
            tiles.Add(Instantiate(tilePrefab, boardParent));
    }

    public void NewGame() //make sure it's solvable
    {
        if (verificationManager != null)
            verificationManager.ResetSession();

        if (tiles.Count != numTiles * numTiles)
            BuildBoard();

        if (useImage && puzzleImages != null && puzzleImages.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, puzzleImages.Length);
            puzzleImage = puzzleImages[idx];
            SliceImageForSize(numTiles);

            if (boardBackground != null)
                boardBackground.sprite = Sprite.Create(
                    puzzleImage,
                    new Rect(0, 0, puzzleImage.width, puzzleImage.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
        }

        state = (int[])goal.Clone();

        shuffleMoves = GetBfsEasyShuffle(numTiles);
        Debug.Log($"Shuffling {numTiles}x{numTiles} board with {shuffleMoves} moves.");

        state = ShuffleByRandomMoves(state, numTiles, shuffleMoves);
        ApplyToUI();
    }

    void ApplyToUI()
    {
        for (int i = 0; i < state.Length; i++)
        {
            tiles[i].Init(state[i], OnTileClicked);

            // image vs number
            if (useImage && slicedSprites != null && i < slicedSprites.Length)
            {
                // state[i] is 1..N*N-1 or 0 for blank
                int v = state[i];
                if (v == 0)
                {
                    tiles[i].SetSprite(null);
                    tiles[i].SetImageTint(blankTint);
                    tiles[i].SetNumberVisible(false);
                    tiles[i].SetBorder(false, imageBorderColor, imageBorderThickness);
                }
                else
                {
                    // value v is at solved index (v-1)
                    var spr = slicedSprites[v - 1];
                    tiles[i].SetSprite(spr);
                    tiles[i].SetImageTint(tileTint);
                    tiles[i].SetNumberVisible(overlayNumbersInImageMode); // hide numbers in image mode (or true if you want tiny labels)
                    tiles[i].SetBorder(false, imageBorderColor, imageBorderThickness);
                }
            }
            else
            {
                // number mode
                tiles[i].SetSprite(null);
                tiles[i].SetImageTint(tileTint);
                tiles[i].SetNumberVisible(true);
                tiles[i].SetBorder(false, imageBorderColor, imageBorderThickness);
            }
        }
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
            onPlayerMove?.Invoke();

            if (IsGoal())
                onPlayerSolved?.Invoke();

            // remove hint border after *any* tile click
            if (activeHintTile != null)
            {
                activeHintTile.SetBorder(false, Color.yellow, 5f);
                activeHintTile = null;
            }
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
            if (moves.Count > 1) moves.RemoveAll(st => st.b == lastBlank);
            var pick = moves[rand.Next(moves.Count)];
            lastBlank = pick.a;
            s = Apply(s, pick);
        }
        return s;
    }

    // choose tiny shuffle depths so BFS is safe
    int GetBfsEasyShuffle(int n)
    {
        if (n == 3) return test3;
        if (n == 4) return test4;
        if (n == 5) return test5;
        if (n == 6) return test6;
        return 15;
    }

    void SliceImageForSize(int n)
    {
        if (!useImage || puzzleImage == null)
        {
            slicedSprites = null;
            return;
        }

        int dim = n;
        slicedSprites = new Sprite[dim * dim];

        float tileW = puzzleImage.width / (float)dim;
        float tileH = puzzleImage.height / (float)dim;

        int idx = 0;
        for (int r = 0; r < dim; r++)
        {
            for (int c = 0; c < dim; c++)
            {
                // last tile is the blank
                if (idx == dim * dim - 1)
                {
                    slicedSprites[idx] = null;
                    idx++;
                    continue;
                }

                var rect = new Rect(c * tileW, (dim - 1 - r) * tileH, tileW, tileH); // flip Y
                slicedSprites[idx] = Sprite.Create(
                    puzzleImage,
                    rect,
                    new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect
                );
                idx++;
            }
        }
    }

    // slider helper
    int BoardSizeForDifficulty(int d)
    {
        if (d == 0) return 3;   // Easy
        if (d == 1) return 4;   // Medium
        if (d == 2) return 5;   // Hard
        return 6;               // Extreme
    }

    string DifficultyName(int d)
    {
        if (d == 0) return "Difficulty Level: Easy (3×3)";
        if (d == 1) return "Difficulty Level: Medium (4×4)";
        if (d == 2) return "Difficulty Level: Hard (5×5)";
        return "Difficulty Level: Extreme (6×6)";
    }

    public void SetSize(int n)
    {
        n = Mathf.Clamp(n, 3, 6);
       if (n == numTiles) return;

        numTiles = n;

        if (verificationManager != null)
            verificationManager.ResetSession();

        BuildGoal();
        BuildBoard();
        SliceImageForSize(numTiles);
        NewGame();
    }

    public void OnDifficultySliderChanged(float v)
    {
        difficultyLevel = Mathf.Clamp((int)v, 0, 3);

        int newSize = BoardSizeForDifficulty(difficultyLevel);

        // Rebuild only if size actually changed
        if (newSize != numTiles)
        {
            SetSize(newSize);  // See method below
        }

        if (difficultyLabel != null)
        {
            difficultyLabel.text = DifficultyName(difficultyLevel);
        }        
    }

    private Tile activeHintTile = null;

    public void HintOneMove()
    {
        //if solved or solving, no hint and play a error sound
        if (IsGoal() || isSolving) 
        {
            AudioManager.Instance?.PlayError();
            return; 
        }

        onPlayerHint?.Invoke();

        // compute once after player clicked hint, then saves this path in case player clicks hint again.
        // If again, use this saved path else recompute.

        var startCopy = (int[])state.Clone();
        var path = BiBFS(startCopy, goal, numTiles);

        if (path != null && path.Count > 0)
        {
            var mv = path[0];
            int tileIndex = mv.b;
            var t = tiles[tileIndex];

            if (activeHintTile != null && activeHintTile != t)
                activeHintTile.SetBorder(false, Color.yellow, 5f);

            activeHintTile = t;
            t.SetBorder(true, Color.yellow, 5f);   // turn on border

            StartCoroutine(HintPulse(t));
        }
    }

    IEnumerator HintPulse(Tile t)
    {
        var rt = t.GetComponent<RectTransform>();
        Vector3 start = rt.localScale;

        // pulse strength & duration depend on board size
        float strength = (numTiles >= 5) ? 0.15f : 0.08f; // bigger boards pulse larger
        float duration = (numTiles >= 5) ? 1f : 0.4f;   // longer on 5x5 / 6x6

        for (float a = 0; a < duration; a += Time.deltaTime)
        {
            float k = 1f + strength * Mathf.Sin(a * Mathf.PI * 3f);
            rt.localScale = start * k;
            yield return null;
        }

        rt.localScale = start;
    }

    // AI solve
    public void Solve()
    {
        onPlayerPressedSolve?.Invoke();
        if (IsGoal() || isSolving)
        {
            AudioManager.Instance?.PlayError();
            return;
        }

        var startCopy = (int[])state.Clone();
        List<Swap> path = null;

        int shuffleDepth = GetBfsEasyShuffle(numTiles) + 5;

        path = BiBFS(startCopy, goal, numTiles, maxDepth: shuffleDepth);

        if (path == null)
        {
            Debug.LogWarning("[Solve] BFS failed. Falling back to reverse shuffle.");

            List<Swap> shufflePath;
            state = ShuffleAndRecordMoves(goal, numTiles, shuffleDepth, out shufflePath);

            path = new List<Swap>();
            for (int i = shufflePath.Count - 1; i >= 0; i--)
            {
                path.Add(new Swap(shufflePath[i].b, shufflePath[i].a));
            }
        }

        if (path.Count == 0)
        {
            Debug.Log("Already solved.");
            return;
        }

        StartCoroutine(PathAnimation(path, 0.2f));
    }

    List<Swap> BiBFS(int[] start, int[] goal, int n, int maxDepth = 30)
    {
        string GetKey(int[] board) => string.Join(",", board);

        bool AreEqual(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var frontierF = new Queue<string>();
        var frontierB = new Queue<string>();

        var visitedF = new ConcurrentDictionary<string, (string prevKey, Swap move)>();
        var visitedB = new ConcurrentDictionary<string, (string prevKey, Swap move)>();
        var boardMap = new ConcurrentDictionary<string, int[]>();

        string startKey = GetKey(start);
        string goalKey = GetKey(goal);

        frontierF.Enqueue(startKey);
        visitedF[startKey] = (null, new Swap(-1, -1));
        boardMap[startKey] = (int[])start.Clone();

        frontierB.Enqueue(goalKey);
        visitedB[goalKey] = (null, new Swap(-1, -1));
        boardMap[goalKey] = (int[])goal.Clone();

        if (AreEqual(start, goal))
            return new List<Swap>();

        string meetingKey = null;
        for (int depth = 0; depth <= maxDepth; depth++)
        {
            if (frontierF.Count <= frontierB.Count)
            {
                ExpandLayerBalanced(frontierF, visitedF, visitedB, boardMap, n, ref meetingKey);
            }
            else
            {
                ExpandLayerBalanced(frontierB, visitedB, visitedF, boardMap, n, ref meetingKey);
            }

            if (meetingKey != null)
            {
                var path = ReconstructPath(meetingKey, visitedF, visitedB, boardMap, start, goal);
                if (path != null)
                {
                    Debug.Log($"[BiBFS] 🎯 Found in {path.Count} steps");
                    Debug.Log($"[BiBFS] ⏱ Time taken: {stopwatch.Elapsed.TotalSeconds:F3} seconds");
                    return path;
                }
            }
        }

        Debug.LogWarning("[BiBFS] ❌ No path found within depth limit");
        Debug.Log($"[BiBFS] ⏱ Time taken: {stopwatch.Elapsed.TotalSeconds:F3} seconds");
        return null;
    }

    void ExpandLayerBalanced(Queue<string> frontier,
                             ConcurrentDictionary<string, (string prevKey, Swap move)> visitedThis,
                             ConcurrentDictionary<string, (string prevKey, Swap move)> visitedOther,
                             ConcurrentDictionary<string, int[]> boardMap,
                             int n,
                             ref string meetingKey)
    {
        int count;
        lock (frontier) count = frontier.Count;
        for (int i = 0; i < count; i++)
        {
            string currentKey;
            lock (frontier)
            {
                if (frontier.Count == 0) return;
                currentKey = frontier.Dequeue();
            }

            var current = boardMap[currentKey];

            foreach (var move in MovableTiles(current, n))
            {
                var next = Apply(current, move);
                string nextKey = string.Join(",", next);

                if (visitedThis.ContainsKey(nextKey))
                {
                    if (AreEqual(boardMap[nextKey], next)) continue;
                }

                visitedThis[nextKey] = (currentKey, move);
                boardMap[nextKey] = (int[])next.Clone();

                if (visitedOther.ContainsKey(nextKey))
                {
                    lock (boardMap)
                    {
                        if (meetingKey == null)
                            meetingKey = nextKey;
                    }
                }

                lock (frontier) frontier.Enqueue(nextKey);
            }
        }
    }

    bool AreEqual(int[] a, int[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    List<Swap> ReconstructPath(
        string meetKey,
        IDictionary<string, (string prevKey, Swap move)> visitedF,
        IDictionary<string, (string prevKey, Swap move)> visitedB,
        IDictionary<string, int[]> boardMap,
        int[] start,
        int[] goal)
    {
        var path = new List<Swap>();

        string h = meetKey;
        while (visitedF.ContainsKey(h) && visitedF[h].prevKey != null)
        {
            var (prev, m) = visitedF[h];
            path.Add(m);
            h = prev;
        }
        path.Reverse();

        h = meetKey;
        while (visitedB.ContainsKey(h) && visitedB[h].prevKey != null)
        {
            var (prev, m) = visitedB[h];
            path.Add(new Swap(m.b, m.a));
            h = prev;
        }

        var testBoard = (int[])start.Clone();
        foreach (var m in path)
            testBoard = Apply(testBoard, m);

        return AreEqual(testBoard, goal) ? path : null;
    }


    IEnumerator PathAnimation(List<Swap> path, float delay = 0.2f)
    {
        if (path == null || path.Count == 0) yield break;
        isSolving = true;

        foreach (var tile in tiles)
            tile.SetIneractable(false);

        foreach (var move in path)
        {
            state = Apply(state, move);
            ApplyToUI();
            yield return new WaitForSeconds(delay);
        }

        foreach (var tile in tiles)
            tile.SetIneractable(true);

        isSolving = false;

        if (IsGoal())
            onPlayerSolved?.Invoke();
    }

    // last resort

    int[] ShuffleAndRecordMoves(int[] goal, int n, int moves, out List<Swap> shufflePath)
    {
        var current = (int[])goal.Clone();
        shufflePath = new List<Swap>();
        var rand = new System.Random();

        int blank = Array.IndexOf(current, 0);

        for (int i = 0; i < moves; i++)
        {
            var possible = MovableTiles(current, n);
            if (shufflePath.Count > 0)
            {
                // Prevent reversing previous move
                var last = shufflePath[shufflePath.Count - 1];
                possible.RemoveAll(m => m.a == last.b && m.b == last.a);
            }

            if (possible.Count == 0) break;

            var move = possible[rand.Next(possible.Count)];
            current = Apply(current, move);
            shufflePath.Add(move);
        }

        return current;
    }

}