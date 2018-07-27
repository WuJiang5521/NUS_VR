﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class LaunchFood : MonoBehaviour {
    private FoodSet.Manual chosen = null;
    private int[] statisticsCount = null;
    private TextMesh[] statisticsCountText = null;
    private int statisticsMissCount;
    private int statisticsErrorCount;
    private enum Status
    {
        FREE, PREPARE, LAUNCH, PAUSE, END
    };
    private Status status = Status.FREE;

    public Transform foods;
    public Transform pieces;
    public Transform statisticsFoods;
    public Transform statisticsText;
    public TextMesh statisticsMiss;
    public TextMesh statisticsError;

    public int prepareCount = 3;
    public float launchInterval = 1f;
    private float bufferTime;
    private int second;

    private GameObject[] foodPool;
    private GameObject[] piecePool;
    private Vector3[] pieceVelocity;
    private Vector3 geo = new Vector3(0, -9.8f, 0);
    private int poolCap = 5;
    private int pieceEach = 3;

    public float statisticsHeight = 11;
    public float moveSpeed = 10;
    public float rotateSpeed = 180;
    public float trajectoryLength = 64;
    public float potHeight = 0;

    // Use this for initialization
    void Start () {
        if (prepareCount <= 0) prepareCount = 3;
        if (launchInterval <= 0) launchInterval = 0.5f;
        foodPool = new GameObject[FoodSet.foods.Length * poolCap];
        for (int i = 0; i < FoodSet.foods.Length; i++)
            for (int j = 0; j < poolCap; j++)
            {
                int k = i * poolCap + j;
                foodPool[k] = Instantiate(FoodSet.foods[i].obj, foods);
                foodPool[k].SetActive(false);
                foodPool[k].transform.localScale = new Vector3(5, 5, 5);
            }
        piecePool = new GameObject[FoodSet.foods.Length * pieceEach];
        pieceVelocity = new Vector3[FoodSet.foods.Length * pieceEach];
        for (int i = 0; i < FoodSet.foods.Length; i++)
            for (int j = 0; j < pieceEach; j++)
            {
                int k = i * pieceEach + j;
                piecePool[k] = Instantiate(FoodSet.foods[i].piece, pieces);
                piecePool[k].SetActive(false);
                piecePool[k].transform.localScale = new Vector3(3, 3, 3);
            }
    }
	
	// Update is called once per frame
	void Update () {
		switch(status)
        {
            case Status.FREE:
                bufferTime = 0;
                second = 0;
                break;
            case Status.PREPARE:
                {
                    int originsecond = second;
                    bufferTime += Time.deltaTime;
                    second = (int)bufferTime;
                    if (originsecond != second)
                    {
                        Debug.Log(second);
                        // show count
                    }
                    if (second == prepareCount)
                    {
                        status = Status.LAUNCH;
                        bufferTime = 0;
                        second = 0;
                    }
                }
                break;
            case Status.LAUNCH:
                if (applicationDown())
                {
                    status = Status.PAUSE;
                }
                else
                {
                    bufferTime += Time.deltaTime;
                    int originSecond = second;
                    second = (int)(bufferTime / launchInterval);
                    if (originSecond != second) launchFood();

                    for (int i = 0; i < foodPool.Length; i++)
                    {
                        GameObject food = foodPool[i];
                        if (food.activeSelf)
                        {
                            food.transform.position += new Vector3(0, 0, Time.deltaTime * moveSpeed);
                            food.transform.localEulerAngles += new Vector3(0, Time.deltaTime * rotateSpeed, 0);
                            if (food.transform.localPosition.z > trajectoryLength)
                            {
                                food.SetActive(false);
                                // some punish
                                if (inManual(i / poolCap) != -1)
                                {
                                    statisticsMissCount++;
                                    statisticsMiss.text = "Miss: " + statisticsMissCount;
                                }
                            }
                        }
                    }
                    for (int i = 0; i < piecePool.Length; i++)
                        if (piecePool[i].activeSelf)
                        {
                            GameObject piece = piecePool[i];
                            piece.transform.position += pieceVelocity[i] * Time.deltaTime;
                            piece.transform.Rotate(new Vector3(15, 30, 45) * Time.deltaTime);
                            pieceVelocity[i] += geo * Time.deltaTime;
                            if (piece.transform.position.y < potHeight)
                                piece.SetActive(false);
                        }
                }
                break;
            case Status.END:
                foreach (GameObject food in foodPool)
                    food.SetActive(false);
                clearChildren(statisticsFoods);
                clearChildren(statisticsText);
                statisticsFoods.gameObject.SetActive(false);
                statisticsText.gameObject.SetActive(false);
                statisticsMiss.gameObject.SetActive(false);
                statisticsError.gameObject.SetActive(false);
                status = Status.FREE;
                break;
            case Status.PAUSE:
                if (applicationDown())
                {
                    status = Status.LAUNCH;
                    // hide menu
                }
                break;
        }
	}

    private void clearChildren(Transform parent)
    {
        GameObject[] children = parent.gameObject.GetComponentsInChildren<GameObject>();
        foreach (GameObject child in children)
            Destroy(child);
    }

    private void launchFood()
    {
        int[] freeFoodIndex = new int[foodPool.Length];
        int freeFoodCount = 0;
        for (int i = 0; i < foodPool.Length; i++)
            if (!foodPool[i].activeSelf)
                freeFoodIndex[freeFoodCount++] = i;

        int choice = freeFoodIndex[UnityEngine.Random.Range(0, freeFoodCount)];
        foodPool[choice].transform.localPosition = new Vector3(UnityEngine.Random.Range(-3, 4), UnityEngine.Random.Range(-3, 4), 0);
        foodPool[choice].SetActive(true);
    }

    public void setManual(int manualIndex)
    {
        if (status == Status.FREE)
            if (manualIndex >= 0 && manualIndex < FoodSet.manuals.Length)
            {
                chosen = FoodSet.manuals[manualIndex];

                // create statistics
                int count = chosen.foods.Length;
                statisticsCount = new int[count];
                statisticsCountText = new TextMesh[count];
                for (int i = 0; i < count; i++)
                {
                    statisticsCount[i] = 0;
                    Vector3 offset = new Vector3(-statisticsHeight / 2 + (i + 1) * statisticsHeight / (count + 1), 0, 0);
                    GameObject tempFood;
                    tempFood = Instantiate(chosen.foods[i].obj, statisticsFoods);
                    tempFood.transform.localPosition = offset;
                    tempFood.transform.localScale = new Vector3(5, 5, 5);
                    GameObject tempText;
                    tempText = new GameObject();
                    tempText.transform.parent = statisticsText;
                    tempText.transform.localPosition = offset;
                    tempText.transform.localEulerAngles = new Vector3(0, 0, 0);
                    tempText.AddComponent<TextMesh>();
                    statisticsCountText[i] = tempText.GetComponent<TextMesh>();
                    statisticsCountText[i].text = 0 + "/" + chosen.nums[i];
                    statisticsCountText[i].anchor = TextAnchor.MiddleCenter;
                    statisticsCountText[i].characterSize = 0.2f;
                    statisticsCountText[i].fontSize = 60;
                }

                statisticsMissCount = 0;
                statisticsErrorCount = 0;
                statisticsMiss.text = "Miss: 0";
                statisticsError.text = "Error: 0";

                statisticsFoods.gameObject.SetActive(true);
                statisticsText.gameObject.SetActive(true);
                statisticsMiss.gameObject.SetActive(true);
                statisticsError.gameObject.SetActive(true);

                status = Status.PREPARE;
            }
    }

    public void cutFood(GameObject food)
    {
        if (status != Status.LAUNCH) return;
        for (int i = 0; i < foodPool.Length; i++)
            if (foodPool[i] == food)
            {
                food.SetActive(false);

                // show piece
                Vector3 pos = food.transform.position;
                int index = i / poolCap;
                for (int j = 0; j < pieceEach; j++)
                {
                    piecePool[index * pieceEach + j].transform.position = new Vector3(pos.x, pos.y, pos.z);
                    pieceVelocity[index * pieceEach + j] = new Vector3(UnityEngine.Random.Range(-1, 1), UnityEngine.Random.Range(-1, 1), 0);
                    piecePool[index * pieceEach + j].SetActive(true);
                }

                // statistics
                int indexInManual = inManual(index);
                if (indexInManual == -1)
                {
                    statisticsErrorCount++;
                    statisticsError.text = "Error: " + statisticsErrorCount;
                }
                else
                {
                    statisticsCount[indexInManual]++;
                    statisticsCountText[indexInManual].text = statisticsCount[indexInManual] + "/" + chosen.nums[indexInManual];
                }

                break;
            }
    }

    private int inManual(int index)
    {
        for (int i = 0; i < chosen.foods.Length; i++)
            if (chosen.foods[i].key == FoodSet.foods[index].key)
                return i;
        return -1;
    }

    bool applicationDown()
    {
        for (int i = 0; i < Player.instance.handCount; i++)
        {
            Hand hand = Player.instance.GetHand(i);
            if (hand.controller != null)
                if (hand.controller.GetPressDown(Valve.VR.EVRButtonId.k_EButton_ApplicationMenu))
                    return true;
        }
        return false;
    }
}
