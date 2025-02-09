﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PathfindingTest : MonoBehaviour
{
    [Header ("Set Positions")]
    public int startPosX;
    public int startPosY;

    [Space(3)] 
    public int endPosX;
    public int endPosY;

    [Header("Automated Testing")]
    [SerializeField] private bool m_runAutomatedTesting;
    [SerializeField] private bool m_randomizePositions;
    [SerializeField] private bool m_createRandomWalls;
    [SerializeField] private int m_testingIterations;
    [SerializeField] private float m_testingDelay;
    private int m_testingCountIndex = 0;
    private int m_failedCount = 0;

    private List<Transform> m_path = new List<Transform>();
    private List<Node> exploredPositions = new List<Node>();

    private float m_optimalDistance;
    private float m_pathDistance;

    void Update()
    {
        DrawPath(m_path.ToArray());
    }

    [ContextMenu("Start Pathfinding Testing")]
    void StartTests()
    {
        if(m_runAutomatedTesting == true)
        {
            StartCoroutine(RunAutomatedTesting());
        }
        else
        {
            Debug.LogError("Automated Testing not enabled");
        }
    }

    [ContextMenu("Stop Pathfinding Testing")]
    void StopTests()
    {
        StopCoroutine(RunAutomatedTesting());
    }

    
    IEnumerator RunAutomatedTesting()
    {
        while (m_testingCountIndex < m_testingIterations)
        {
            if(m_randomizePositions == true)
            {
                startPosX = Random.Range(0, GameBoardGeneration.Instance.Graph.GetLength(0));
                startPosY = Random.Range(0, GameBoardGeneration.Instance.Graph.GetLength(1));
                endPosX = Random.Range(0, GameBoardGeneration.Instance.Graph.GetLength(0));
                endPosY = Random.Range(0, GameBoardGeneration.Instance.Graph.GetLength(1));
            }
            if (m_createRandomWalls)
            {
                for (int i = 0; i < GameBoardGeneration.Instance.Graph.GetLength(0); i++)
                {
                    for (int j = 0; j < GameBoardGeneration.Instance.Graph.GetLength(1); j++)
                    {
                        GameBoardGeneration.Instance.Graph[i, j].navigability =  nodeTypes.navigable;
                    }
                }
                for (int i = 0; i < Random.Range(0, GameBoardGeneration.Instance.Graph.Length); i++)
                {
                    int RandomX = Random.Range(0, GameBoardGeneration.Instance.Graph.GetLength(0));
                    int RandomY = Random.Range(0, GameBoardGeneration.Instance.Graph.GetLength(1));
                    GameBoardGeneration.Instance.Graph[RandomX, RandomY].navigability = nodeTypes.wall;
                }
            }

            RunPathfinding();
            yield return new WaitForSeconds(m_testingDelay);
            m_testingCountIndex++;
        }
        Debug.Log("Testing Complete");
    }
    void RunPathfinding()
    {
        m_path = new List<Transform>();
        var graph = GameBoardGeneration.Instance.Graph;
        var search = new Search();
        search.StartSearch(graph[startPosX, startPosY], graph[endPosX, endPosY],SearchTypes.Aggressive);


        Transform[] _pathPositions = new Transform[search.path.Count];
        for (int i = 0; i < search.path.Count; i++)
        {
            m_path.Add(search.path[i].hex.transform);
        }

        if (search.path.Count == 0)
        {
            Debug.Log("Search Failed");
            return;
        }

        Debug.Log("Search " + m_testingCountIndex + " done. Path length : " + search.path.Count);
    }
    void DrawPath(Transform[] _positions)
    {

        try
        {
            Vector3 _startingPoint = new Vector3(_positions[0].position.x, _positions[0].position.y + 1.5f, _positions[0].position.z);
            Vector3 _finishingPoint = new Vector3(_positions[_positions.Length - 1].position.x, _positions[_positions.Length - 1].position.y + 1.5f, _positions[_positions.Length - 1].position.z);

            Debug.DrawLine(_startingPoint, _finishingPoint, Color.green);

            for (int i = 1; i < _positions.Length; i++)
            {
                _startingPoint = new Vector3(_positions[i-1].position.x, _positions[i - 1].position.y + 1.5f, _positions[i - 1].position.z);
                _finishingPoint = new Vector3(_positions[i].position.x, _positions[i].position.y + 1.5f, _positions[i].position.z);

                Debug.DrawLine(_startingPoint, _finishingPoint, Color.blue);
            }
        }
        catch{

        }

    }
}
