using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FD.Car
{
    /// <summary>
    /// Handles the car score system. 
    /// </summary>
    /// <remarks>Its called score cause the player will receive a highscore</remarks>
    public class CarScore
    {
        /* Things you can receive points for
         * cM = continues multiplier, cP = continues points, sP = single point
        #- cM drifting multiplier, every sec is a + x1
        - cP Staying above a certain speed 
        #- cP driving on the road cP (continues points for every second) 
        - cP sticking close to the edge of the road
        - cP air time
        #- sP breaking road signs
        - sP when finish not getting off road once
        - sP when finish driving time
        - sP not crashing entire race
        */
        /// <summary>
        /// The current points of the player
        /// </summary>
        public int CurrentPoints { get { return currentPoints; } }
        /// <summary>
        /// The points that are going to be added to the buffer if drift succeeds
        /// </summary>
        public int CurrentPointsToBeAddedToBuffer { get { return currentPointsToBeAddedToBuffer; } }
        public int CurrentPointsDrifting { get { return currentPointsDrifting; } }
        public int CurrentPointsDebris { get { return currentPointsDebris; } }
        public int CurrentPointsOnRoad { get { return currentPointsOnRoad; } } //TODO this can be set to get; private set;
        public float CurrentDriftMultiplier { get { return currentDriftMultiplier; } }
        public Stopwatch TrackTime { get { return stopwatch; } }

        /// <summary>
        /// Action called when PointsAddById is called
        /// </summary>
        public Action<int, int, string> OnPointsAddByID;
        /// <summary>
        /// Called when the points are added to the score
        /// </summary>
        public Action OnPointsAddToScore;

        /// <summary>
        /// Has the timer started?
        /// </summary>
        private bool startedTimer;
        /// <summary>
        /// The current points of the player
        /// </summary>
        private int currentPoints;
        /// <summary>
        /// The buffer used for current points to add it spread instead of instant
        /// </summary>
        private int currentPointsBuffer;
        /// <summary>
        /// The points that are going to be added to the buffer if drift succeeds
        /// </summary>
        private int currentPointsToBeAddedToBuffer;
        /// <summary>
        /// The current points for drifting to be added to currentPoints
        /// </summary>
        private int currentPointsDrifting;
        /// <summary>
        /// The current points for debris to be added to currentPointsBuffer
        /// </summary>
        private int currentPointsDebris;
        /// <summary>
        /// The current points for OnRoad to be added to currentPointsBuffer
        /// </summary>
        private int currentPointsOnRoad;
        /// <summary>
        /// The current drift multiplier (1x, 2x etc)
        /// </summary>
        private float currentDriftMultiplier;
        /// <summary>
        /// Used for when the currentPointsBuffer gets added to currentPoints
        /// </summary>
        private float currentPointBufferTimer;

        // The points receive values
        /// <summary>
        /// The points the player receives when entering a drift
        /// </summary>
        private readonly int singlePointsForDrifting = 10;
        /// <summary>
        /// Points received while on road when drifting
        /// </summary>
        private readonly int pointsForOnRoad = 40;

        private CarControllerV3 car;
        private Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// Flag used for drifting
        /// </summary>
        private bool isDriftingFlag;
        /// <summary>
        /// Used to check if a second passed. If a second passed this will reset to 0 in late update
        /// </summary>
        private float secondPassedTimer;

        public CarScore(CarControllerV3 car)
        {
            this.car = car;
        }

        /// <summary>
        /// Called from car.Update
        /// </summary>
        public void Update()
        {
            secondPassedTimer += Time.deltaTime;

            ScoreDrifting();
            ScoreOnRoad();
            UpdateTimer();
            UpdateCurrentPointsBuffer();
        }

        public void LateUpdate()
        {
            if(secondPassedTimer >= 1) secondPassedTimer = 0;
        }

        /// <summary>
        /// Add points to corresponding id.
        /// </summary>
        /// <param name="id">The id of the points</param>
        /// <param name="points">amount of points</param>
        /// <param name="name">Display name of points</param>
        /// <remarks>
        /// -1 instant points
        /// 0 drifting
        /// 1 debris
        /// 2 onRoad
        /// </remarks>
        public void PointsAddByID(int id, int points, string name)
        {
            // If car isnt drifting points are added instant
            if(!isDriftingFlag)
            {
                // Add instant
                currentPointsBuffer += points;
                OnPointsAddByID.Invoke(-1, points, name);
                return;
            }

            currentPointsToBeAddedToBuffer += points;
            switch(id)
            {
                // case -1, used for instant points when not drifting (see above)
                case 0: currentPointsDrifting += points; break;
                case 1: currentPointsDebris += points; break;
                case 2: currentPointsOnRoad += points; break;
                default: UnityEngine.Debug.LogError("[CarScore] points id not recognised!"); return;
            }

            // Invoke the action
            OnPointsAddByID.Invoke(id, points, name);
        }

        private void UpdateTimer()
        {
            if(GameManager.RoadGenerationState != 1) return;
            // Have to wait for CarUI if not started
            if(!startedTimer && GameManager.GameState == 1001)
            {
                startedTimer = true;
                stopwatch.Stop();
                stopwatch.Start();
            }
        }

        private void ScoreDrifting()
        {
            if(car.IsDrifting)
            {
                if(!isDriftingFlag)
                {
                    isDriftingFlag = true;
                    // Player starts drifting
                    PointsAddByID(0, singlePointsForDrifting, "Drifting");
                }
                currentDriftMultiplier += Time.deltaTime;
            }
            else
            {
                if(isDriftingFlag)
                {
                    isDriftingFlag = false;
                    PointsAddToScore();
                }
            }
        }

        /// <summary>
        /// While drifting if the car stays on the road add points
        /// </summary>
        private void ScoreOnRoad()
        {
            if(isDriftingFlag && car.AllWheelsOnRoad)
            {
                if(secondPassedTimer >= 1)
                {
                    // Add score
                    PointsAddByID(2, pointsForOnRoad, "Driving On Road");
                }
            }
        }

        /// <summary>
        /// This is called when drifting is over
        /// </summary>
        private void PointsAddToScore()
        {
            // Add all current points variants to currentPointsBuffer x currentDriftMultiplier
            currentPointsBuffer += currentPointsToBeAddedToBuffer * (int)currentDriftMultiplier;
            // reset
            currentDriftMultiplier = 0;
            currentPointsToBeAddedToBuffer = 0;
            currentPointsDrifting = 0;
            currentPointsDebris = 0;
            currentPointsOnRoad = 0;

            // Invoke
            OnPointsAddToScore.Invoke();
        }

        /// <summary>
        /// Transfers the buffer points to currentPoints
        /// </summary>
        private void UpdateCurrentPointsBuffer()
        {
            // Add the buffer points to currentPoints
            // If the buffer is high it adds points quicker then when it is low
            if(currentPointBufferTimer <= 0)
            {
                currentPointBufferTimer = 0.5f; // add every x sec
                int addRate;
                if(currentPointsBuffer > 1000) addRate = 1000; else if(currentPointsBuffer > 100) addRate = 100; else if(currentPointsBuffer > 10) addRate = 10; else addRate = 1; //IMPROVE
                int toAdd = currentPointsBuffer - addRate < 0 ? currentPointsBuffer : addRate; // check if buffer isnt empty
                
                currentPoints += toAdd;
                currentPointsBuffer -= toAdd;
            }
            else currentPointBufferTimer -= Time.deltaTime;
        }        
    }
}