using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

public enum TicTacToeState { none, cross, circle }

[Serializable]
public class WinnerEvent : UnityEvent<int>
{
}

public class TicTacToeAI : MonoBehaviour
{

	int _aiLevel;

	TicTacToeState[,] boardState;

	[SerializeField] public bool _isPlayerTurn;

	[SerializeField] private int _gridSize = 3;

	[SerializeField]
	private TicTacToeState playerState = TicTacToeState.cross;
	TicTacToeState aiState = TicTacToeState.circle;

	[SerializeField] private GameObject _xPrefab;

	[SerializeField]
	private GameObject _oPrefab;

	public UnityEvent onGameStarted;

	//Call This event with the player number to denote the winner
	public WinnerEvent onPlayerWin;

	ClickTrigger[,] _triggers;

	[SerializeField] private int _moveCount;

	float _delayTime = 0.3f;

	private void Awake()
	{
		if (onPlayerWin == null)
		{
			onPlayerWin = new WinnerEvent();
		}
	}

	public void StartAI(int AILevel)
	{
		_aiLevel = AILevel;
		StartGame();
	}

	private bool HasEmptyCells(TicTacToeState[,] boardCopy)
	{
		for (int i = 0; i < _gridSize; i++)
			for (int j = 0; j < _gridSize; j++)
				if (boardCopy[i, j] == TicTacToeState.none)
					return true;
		return false;
	}

	public void RegisterTransform(int myCoordX, int myCoordY, ClickTrigger clickTrigger)
	{
		_triggers[myCoordX, myCoordY] = clickTrigger;
	}

	private void StartGame()
	{
		_triggers = new ClickTrigger[3, 3];
		boardState = new[,]
		{
			{TicTacToeState.none,TicTacToeState.none,TicTacToeState.none},
			{TicTacToeState.none,TicTacToeState.none,TicTacToeState.none},
			{TicTacToeState.none,TicTacToeState.none,TicTacToeState.none}
		};
		onGameStarted.Invoke();
	}

	public void PlayerSelects(int coordX, int coordY)
	{
		if (!_isPlayerTurn) return;

		StartCoroutine(DrawMove(coordX, coordY, playerState, _delayTime));

		//switch turns
		_isPlayerTurn = false;

		if (HasEmptyCells(boardState) && GetBoardState(boardState) == 0) AiSelects();
	}

	private void AiSelects()
	{
		if (_aiLevel == 0)
		{
			// on easy mode we generate a random move
			EasyMode();
		}
		if (_aiLevel == 1)
		{
			HardMode();
		}

		// switch to players turn
		_isPlayerTurn = true;
	}

	void CheckResult()
	{
		int score = GetBoardState(boardState);

		// ai won the game
		if (score == +1)
			onPlayerWin.Invoke(1);

		// human won the game
		if (score == -1)
			onPlayerWin.Invoke(0);

		// no moves left and no winners, then it's a tie
		if (score == 0 && !HasEmptyCells(boardState))
			onPlayerWin.Invoke(-1);
	}

	private void EasyMode()
	{
		GenerateMove(out var coordX, out var coordY);
		StartCoroutine(DrawMove(coordX, coordY, aiState, _delayTime));
	}

	private void HardMode()
	{
		ClickTrigger maxMove = GetBestMove(boardState);
		StartCoroutine(DrawMove(maxMove._myCoordX, maxMove._myCoordY, aiState, _delayTime));
	}

	private int GetBoardState(TicTacToeState[,] boardState)
	{

		// 1. check for a win on columns
		// x o o
		// x o o
		// x o o
		for (int col = 0; col < 3; col++)
		{
			if (boardState[0, col] == boardState[1, col] && boardState[1, col] == boardState[2, col])
			{
				if (boardState[0, col] == TicTacToeState.circle)
					return +1;
				else if (boardState[0, col] == TicTacToeState.cross)
					return -1;
			}
		}

		// 2. check for a win on rows
		// x x x
		// o o o
		// o o o

		for (int row = 0; row < 3; row++)
		{
			if (boardState[row, 0] == boardState[row, 1] && boardState[row, 1] == boardState[row, 2])
			{
				if (boardState[row, 0] == TicTacToeState.circle)
					return +1;
				else if (boardState[row, 0] == TicTacToeState.cross)
					return -1;
			}
		}

		// 2. check for a win on diagonals
		// x o o
		// o x o
		// o o x
		if (boardState[0, 0] == boardState[1, 1] && boardState[1, 1] == boardState[2, 2])
		{
			if (boardState[0, 0] == TicTacToeState.circle)
				return +1;
			else if (boardState[0, 0] == TicTacToeState.cross)
				return -1;
		}
		if (boardState[0, 2] == boardState[1, 1] && boardState[1, 1] == boardState[2, 0])
		{
			if (boardState[0, 2] == TicTacToeState.circle)
				return +1;
			else if (boardState[0, 2] == TicTacToeState.cross)
				return -1;
		}
		return 0;
	}

	// for more information about the minimax algorithm check this link
	// https://en.wikipedia.org/wiki/Minimax
	private int Minimax(TicTacToeState[,] boardState, int depth, bool isMax)
	{
		int score = GetBoardState(boardState);

		// if there's a winner then return
		if (score == 1 || score == -1)
			return score;

		// if there are no empty cells and there's no winner then its a tie
		if (HasEmptyCells(boardState) == false)
			return 0;

		if (isMax)
		{
			int best = -2;

			for (int i = 0; i < _gridSize; i++)
			{
				for (int j = 0; j < _gridSize; j++)
				{
					if (boardState[i, j] == TicTacToeState.none)
					{
						boardState[i, j] = aiState;
						best = Math.Max(best, Minimax(boardState, depth + 1, !isMax));
						boardState[i, j] = TicTacToeState.none;
					}
				}
			}
			return best;
		}
		else
		{
			int best = 2;
			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					if (boardState[i, j] == TicTacToeState.none)
					{
						boardState[i, j] = playerState;
						best = Math.Min(best, Minimax(boardState, depth + 1, !isMax));
						boardState[i, j] = TicTacToeState.none;
					}
				}
			}
			return best;
		}
	}

	private ClickTrigger GetBestMove(TicTacToeState[,] boardState)
	{
		int bestVal = -2;
		ClickTrigger bestMove = gameObject.AddComponent<ClickTrigger>();

		// traverse cells and evaluate minimax function
		for (int i = 0; i < 3; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				if (boardState[i, j] == TicTacToeState.none)
				{
					boardState[i, j] = aiState;

					int moveScore = Minimax(boardState, 0, false);
					boardState[i, j] = TicTacToeState.none;

					if (moveScore > bestVal)
					{
						bestMove._myCoordX = i;
						bestMove._myCoordY = j;
						bestVal = moveScore;
					}
				}
			}
		}

		return bestMove;
	}

	void GenerateMove(out int coordX, out int coordY)
	{

		List<ClickTrigger> possibleMoves = new List<ClickTrigger>();

		foreach (ClickTrigger move in _triggers)
		{
			if (move.canClick)
			{
				possibleMoves.Add(move);
			}
		}

		int randomMove = UnityEngine.Random.Range(0, possibleMoves.Count);

		coordX = possibleMoves[randomMove]._myCoordX;
		coordY = possibleMoves[randomMove]._myCoordY;

		if (possibleMoves.Count < 1) return;
	}

	private void SetVisual(int coordX, int coordY, TicTacToeState targetState)
	{
		Instantiate(
			targetState == aiState ? _oPrefab : _xPrefab,
			_triggers[coordX, coordY].transform.position,
			Quaternion.identity
		);
	}

	IEnumerator DrawMove(int coordX, int coordY, TicTacToeState player, float delayTime)
	{
		if (!_isPlayerTurn) yield return new WaitForSeconds(delayTime);

		if (boardState[coordX, coordY] == TicTacToeState.none)
		{
			_triggers[coordX, coordY].canClick = false;
			boardState[coordX, coordY] = player;
			SetVisual(coordX, coordY, player);
		}
		_moveCount++;
		CheckResult();
	}
}
