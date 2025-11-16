using System;
using TMPro;
using Unity.InferenceEngine;
using Unity.Mathematics;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    float maxMoveSpeed = 5f;
    float degPerSec = 80;

    float rewardForTargetCollection = 10.0f;
    float punishmentForLose = -.6f;
    float shapingMultiplier = 1.0f;

    [Header("GameObjects: ")]
    [SerializeField] public GameObject bob;
    [SerializeField] public BalanceAgent agent;
    [SerializeField] public GameObject ball;
    [SerializeField] public GameObject target;
    [SerializeField] public GameObject ui;

    [HideInInspector] public float maxAngle = 20f;
    [HideInInspector] public float moveLimit = 2f;
    [HideInInspector] public float xDeg;
    [HideInInspector] public float zDeg;
    [HideInInspector] public bool inTarget = false;
    public bool main = false;
    [Header("UI: ")]
    [SerializeField] Slider stepSlider;
    [SerializeField] Slider targetSlider;
    [SerializeField] Scrollbar targetBar;
    [SerializeField] TextMeshProUGUI targetBarText;
    [SerializeField] Scrollbar modelBar;
    [SerializeField] TextMeshProUGUI modelBarText;
    public ModelAsset[] brains = new ModelAsset[3];
    int targetsCollected = 0;
    float tc = 0;
    float pdft = 0;

    private int m_currentDifficultyLevel = 1;
    private int m_maxDifficultyLevel = 10;
    private int m_stepsPerTarget = 500;
    private bool m_episodeWasWin = false;
    private bool m_episodeWasLoss = false;

    void Awake()
    {
        if (main)
        {
            ui.SetActive(true);
            m_currentDifficultyLevel = 3;
            m_maxDifficultyLevel = 100;
            stepSlider.maxValue = m_currentDifficultyLevel * m_stepsPerTarget;
            targetSlider.maxValue = m_currentDifficultyLevel;
        }
        if (ui != null)
        {
            ui.SetActive(false);
            targetBar.onValueChanged.AddListener(delegate { UpdateTargetCount(); });
            modelBar.onValueChanged.AddListener(delegate { UpdateModel(); });
        }
        UpdateSeed();
        SpawnTarget();
    }

    void UpdateModel()
    {
        bob.GetComponent<BehaviorParameters>().Model = brains[Mathf.RoundToInt(modelBar.value * 2)];
        ResetEnv();
        agent.EndEpisode();
        switch (modelBar.value * 2)
        {
            case 0:
                modelBarText.text = "v0";
                break;
            case 1:
                modelBarText.text = "v1";
                break;
            case 2:
                modelBarText.text = "v2";
                break;
        }
    }
    void UpdateTargetCount()
    {
        switch (targetBar.value)
        {
            case 0f:
                m_currentDifficultyLevel = 3;
                break;
            case 0.25f:
                m_currentDifficultyLevel = 5;
                break;
            case 0.5f:
                m_currentDifficultyLevel = 10;
                break;
            case 0.75f:
                m_currentDifficultyLevel = 20;
                break;
            case 1f:
                m_currentDifficultyLevel = 100;
                break;
        }
        targetBarText.text = m_currentDifficultyLevel.ToString();
        ResetEnv();
        agent.EndEpisode();
    }

    void Update()
    {
        if (main)
        {
            stepSlider.value = agent.StepCount;
            targetSlider.value = targetsCollected;
        }
        if (ui != null)
        {
            ui.SetActive(main);
        }
    }

    void FixedUpdate()
    {
        BallCheck();
    }

    void UpdateSeed()
    {
        UnityEngine.Random.InitState((int)(DateTime.Now.Ticks % int.MaxValue));
    }

    public void SetInputs(float rx, float rz, float mx)
    {
        ApplyInput(rx, rz, mx);
    }

    public void ApplyInput(float rx, float rz, float mx)
    {
        float dt = Time.fixedDeltaTime;

        xDeg += rx * degPerSec * dt;
        zDeg += rz * degPerSec * dt;
        xDeg = Mathf.Clamp(xDeg, -maxAngle, maxAngle);
        zDeg = Mathf.Clamp(zDeg, -maxAngle, maxAngle);
        bob.transform.localRotation = Quaternion.Euler(xDeg, 0f, zDeg);

        float moveDelta = mx * maxMoveSpeed * dt;
        Vector3 lp = bob.transform.localPosition;
        lp.x = Mathf.Clamp(lp.x + moveDelta, -moveLimit, moveLimit);
        bob.transform.localPosition = lp;
    }

    void ResetEnv()
    {
        if (main)
        {
            stepSlider.maxValue = m_currentDifficultyLevel * m_stepsPerTarget;
            targetSlider.maxValue = m_currentDifficultyLevel;
        }
        else
        {
            if (m_episodeWasWin)
            {
                m_currentDifficultyLevel = Mathf.Min(m_currentDifficultyLevel + 1, m_maxDifficultyLevel);
            }
            else if (m_episodeWasLoss)
            {
                m_currentDifficultyLevel = Mathf.Max(m_currentDifficultyLevel - 1, 1);
            }
        }

        if (agent != null)
        {
            agent.MaxStep = m_currentDifficultyLevel * m_stepsPerTarget;
        }

        m_episodeWasWin = false;
        m_episodeWasLoss = false;

        UpdateSeed();
        ball.transform.localPosition = new Vector3(0, .8f, 0);
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.angularVelocity = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
        bob.transform.localPosition = Vector3.zero;
        bob.transform.localRotation = quaternion.identity;
        ball.SetActive(true);
        xDeg = 0;
        zDeg = 0;
        targetsCollected = 0;
        SpawnTarget();

        Vector2 ballPos = new Vector2(ball.transform.localPosition.x, ball.transform.localPosition.z);
        Vector2 targetPos = new Vector2(target.transform.localPosition.x, target.transform.localPosition.z);
        pdft = Vector2.Distance(ballPos, targetPos);
    }

    void BallCheck()
    {
        Vector2 ballPos = new Vector2(ball.transform.localPosition.x, ball.transform.localPosition.z);
        Vector2 targetPos = new Vector2(target.transform.localPosition.x, target.transform.localPosition.z);

        if (ball.activeSelf)
        {
            if (targetsCollected == m_currentDifficultyLevel)
            {
                m_episodeWasWin = true;
                agent.EndEpisode();
                ResetEnv();
            }

            if (ball.transform.position.y <= 0.25f)
            {
                m_episodeWasLoss = true;
                ball.SetActive(false);
                agent.AddReward(punishmentForLose);
                agent.EndEpisode();
                ResetEnv();
            }
            else
            {
                float currentDistance = Vector2.Distance(ballPos, targetPos);
                float proximityReward = Mathf.Exp(-currentDistance * 0.5f);
                float deltaReward = (pdft - currentDistance) * 0.1f;
                pdft = currentDistance;
                float tiltPenalty = (Mathf.Abs(xDeg) + Mathf.Abs(zDeg)) / (2f * maxAngle);

                float shapingReward = (proximityReward + deltaReward - tiltPenalty * 0.2f) * (shapingMultiplier / agent.MaxStep);

                agent.AddReward(shapingReward);
            }

            if (inTarget)
            {
                tc += Time.fixedDeltaTime;
                if (tc > 1)
                {
                    TargetCollected();
                }
            }
            else
            {
                tc = 0;
            }
        }
        else
        {
            inTarget = false;
        }
    }

    void SpawnTarget()
    {
        tc = 0;
        if (target.activeSelf == true)
        {
            target.SetActive(false);
        }
        inTarget = false;
        Vector2 size = Vector2.one;
        float xPos = 0;
        if (MathF.Abs(bob.transform.localPosition.x) < 1.5f)
        {
            if (UnityEngine.Random.Range(1, 100) < 50f)
            {
                xPos = UnityEngine.Random.Range(bob.transform.localPosition.x + 1, 2.5f - size.x);
            }
            else
            {
                xPos = UnityEngine.Random.Range(-2.5f + size.x, bob.transform.localPosition.x - 1);
            }
        }
        else
        {
            if (bob.transform.localPosition.x < 0)
            {
                xPos = UnityEngine.Random.Range(bob.transform.localPosition.x + 1, 2.5f - size.x);
            }
            else
            {
                xPos = UnityEngine.Random.Range(-2.5f + size.x, bob.transform.localPosition.x - 1);
            }
        }


        target.transform.localPosition = new Vector3(xPos, .65f, 0f);

        target.SetActive(true);
    }

    void TargetCollected()
    {
        targetsCollected++;
        agent.AddReward(rewardForTargetCollection * (1f / m_currentDifficultyLevel));
        if (targetsCollected < m_currentDifficultyLevel)
        {
            SpawnTarget();
        }
        tc = 0;
    }
}