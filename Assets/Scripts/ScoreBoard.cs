using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreBoard : MonoBehaviour
{
    GameObject[] snowballers;

    public TextMeshProUGUI firstPlaceScore;
    public TextMeshProUGUI secondPlaceScore;
    public TextMeshProUGUI thirdPlaceScore;

    public TextMeshProUGUI firstPlaceName;
    public TextMeshProUGUI secondPlaceName;
    public TextMeshProUGUI thirdPlaceName;

    string firstName, secondName, thirdName;
    int firstScore, secondScore, thirdScore;
    
    private void Start()
    {
        snowballers = GameObject.FindGameObjectsWithTag("Snowballer");

        firstName = snowballers[0].name;
        secondName = snowballers[1].name;
        thirdName = snowballers[2].name;

        firstScore = GetScore(snowballers[0]);
        secondScore = GetScore(snowballers[1]);
        thirdScore = GetScore(snowballers[2]);

        TopThree();
    }

    // Update is called once per frame
    void Update()
    {
        PurgeList();

        firstPlaceScore.text = firstScore.ToString();
        secondPlaceScore.text = secondScore.ToString();
        thirdPlaceScore.text = thirdScore.ToString();

        firstPlaceName.text = firstName;
        secondPlaceName.text = secondName;
        thirdPlaceName.text = thirdName;
    }


    public void UpdateList()
    {
        TopThree();
    }

    void TopThree()
    {
        int currentScore = 0;

        for(int i = 0; i < snowballers.Length; i++)
        {
            currentScore = GetScore(snowballers[i]);

            if (firstScore < currentScore) 
            {
                firstScore = currentScore;
                firstName = snowballers[i].name;
            }
            else if (secondScore < currentScore && currentScore <= firstScore && !firstName.Equals(snowballers[i].name))
            {
                secondScore = currentScore;
                secondName = snowballers[i].name;
            }
            else if (thirdScore < currentScore && currentScore <= secondScore && currentScore <= firstScore && !firstName.Equals(snowballers[i].name) && !secondName.Equals(snowballers[i].name))
            {
                thirdScore = currentScore;
                thirdName = snowballers[i].name;
            }
        }
    }


    //Removes Scores that no longer exist
    void PurgeList()
    {
        foreach (GameObject snowballer in snowballers)
        {
            int score = GetScore(snowballer);

            if (firstName.Equals(snowballer.name))
            {
                if (score != firstScore)
                {
                    firstScore = 0;
                    secondScore = 0;
                    thirdScore = 0;
                    TopThree();
                }
            }

            if (secondName.Equals(snowballer.name))
            {
                if (score != secondScore)
                {
                    secondScore = 0;
                    thirdScore = 0;
                    TopThree();
                }
            }

            if (thirdName.Equals(snowballer.name))
            {
                if (score != thirdScore)
                {
                    thirdScore = 0;
                    TopThree();
                }
            }
        }
    }

    int GetScore(GameObject snowballer)
    {
        int score = 0;

        AISnowball aiScript = snowballer.GetComponent<AISnowball>();
        if (aiScript != null)
            score = aiScript.GetSize;

        SnowBallStats playerScript = snowballer.GetComponent<SnowBallStats>();
        if (playerScript != null)
            score = playerScript.GetSize;

        return score;
    }
}
