using UnityEngine;

using System.Collections;
using System.Collections.Generic;

using Chess;
using Chess.Game;
using TMPro;

// Handles all aspects related to the sending and recieving information to the Genvid stream.
public class GenvidChessManager : MonoBehaviour
{
    // We use a unique public static instance of this class to be able to access it from any script.
    public static GenvidChessManager Instance { get; private set; }
    
    public GameManager GameManager;
    public GenvidVoteEventHandler VoteEventHandler;
    public TextMeshProUGUI TimerText;
    public TextMeshProUGUI RestartText;
    
    // Duration of the voting period in seconds.
    public int VoteDuration;
    private float voteTimer;

    private bool startMoveVote;
    private bool closeMoveVote;
    
    private MoveGenerator moveGenerator;
    private string[] pieceNames = {
        "None",
        "King",
        "Pawn",
        "Knight",
        "",
        "Bishop",
        "Rook",
        "Queen"
    };

    void Awake()
    {
        Instance = this;

        moveGenerator = new MoveGenerator();
    }

    void Update()
    {
        if (voteTimer > 0)
        {
            voteTimer -= Time.deltaTime;
            TimerText.text = voteTimer.ToString("F");

            if (voteTimer < 0)
            {
                endVote();
            }
        }
    }

    public void StartVote()
    {
        startMoveVote = true;
        voteTimer = VoteDuration;
    }

    private void endVote()
    {
        closeMoveVote = true;
        
        string moveName = VoteEventHandler.GetMostVotedMove();
        
        // If no vote has been cast we restart the vote.
        if (moveName == "")
        {
            StartCoroutine(restartVote());
        }
        else
        {
            GameManager.WhitePlayer.ChooseMove(moveName);
            VoteEventHandler.ResetVoteCount();
        }
    }

    private IEnumerator restartVote()
    {
        RestartText.gameObject.SetActive(true);
        
        yield return new WaitForSeconds(5);

        StartVote();

        RestartText.gameObject.SetActive(false);
    }

    // Since we can't directly serialize a array of structs, we make make a wrapper to hold the array of moves. 
    [System.Serializable]
    public struct MoveList
    {
        [SerializeField] public MoveIdentifier[] LegalMoves;
    }

    [System.Serializable]
    public struct MoveIdentifier
    {
        public string Piece;       // Name of the piece.
        public string StartSquare; // Current square of the piece in x0 format.
        public string MoveName;    // Move in x0-x0 format.
    }
    
    // We open the vote whenever it's the white player's turn. 
    // By using an annotation we make sure the vote opens at the same time the turn starts on the stream.
    public void SubmitVoteStartAnnotation(string streamId)
    {
        // We only want to start a vote once at the beginning of a turn.
        if (!startMoveVote)
            return;
        
        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            List<Move> moves = moveGenerator.GenerateMoves(GameManager.board);
            MoveIdentifier[] moveIdentifiers = new MoveIdentifier[moves.Count];

            for (int i = 0; i < moves.Count; i++)
            {
                // probably want to extract this into a funciton
                int pieceType = Piece.PieceType(GameManager.board.Square[moves[i].StartSquare]);
                string startSquare = BoardRepresentation.SquareNameFromIndex(moves[i].StartSquare);

                moveIdentifiers[i] = new MoveIdentifier {
                    Piece = pieceNames[pieceType],
                    StartSquare = startSquare,
                    MoveName = moves[i].Name
                };
            }

            MoveList moveList = new MoveList { LegalMoves = moveIdentifiers };

            GenvidSessionManager.Instance.Session.Streams.SubmitAnnotationJSON(streamId, moveList);

            startMoveVote = false;
        }
    }

    // After the voting period has finished the vote will close and the most voted move will be executed.
    public void SubmitVoteClosedAnnotation(string streamId)
    {
        if (!closeMoveVote)
            return;

        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            // As we don't care about the content, just the existence of the annotation, we just send an empty move identifier.
            GenvidSessionManager.Instance.Session.Streams.SubmitAnnotationJSON(streamId, new MoveIdentifier());

            closeMoveVote = false;
        }
    }
}