using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Entity : MonoBehaviour
{

    public class Speech
    {
        public float Time;
        public float Duration;
        public string Content;
    }

    public Image Visual;
    public Text NameUi;
    public Text SpeechUi;
    public Image SpeechBackgroundUi;
    public Image SpeechQueueUi;
    public Image Lifebar;
    public float Speed;
    public string Name;
    public int Life;
    public bool Fixed;
    public bool Has8Directions;

    public bool isDead { get; protected set; }
    public Direction direction { get; protected set; }
    public int maxLife { get; protected set; }
    public List<long> path { get; protected set; }

    private readonly List<Speech> speechBuffer = new List<Speech>();
    private int currentSpeech;
    private float initialLifebarWidth;

    public int currentX
    {
        get { return Mathf.RoundToInt(transform.localPosition.x / GameConstants.CellSize); }
    }

    public int currentY
    {
        get { return Mathf.RoundToInt(transform.localPosition.y / GameConstants.CellSize); }
    }

    public float lifePercent
    {
        get { return (float) Life / maxLife; }
    }

    public bool hasPath
    {
        get { return (path != null && path.Count > 0); }
    }

    void Awake()
    {
        if (SpeechUi != null)
        {
            SpeechUi.text = "";
        }
        if (Lifebar != null)
        {
            initialLifebarWidth = Lifebar.rectTransform.rect.width;
        }
        maxLife = Life;

        if (SpeechUi != null)
        {
            StartCoroutine(SpeechBubble());
        }
    }

    void Update()
    {
        if (Lifebar != null)
        {
            Lifebar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, initialLifebarWidth * lifePercent);
            Lifebar.color = Color.Lerp(Color.red, Color.green, lifePercent);
        }

        if (hasPath && !isDead)
        {
            var step = path[0];
            var dx = step.GetX() * GameConstants.CellSize - transform.localPosition.x;
            var dy = step.GetY() * GameConstants.CellSize - transform.localPosition.y;
            var delta = new Vector3(dx, dy);
            var deltaMag = delta.magnitude;
            if (Mathf.Approximately(0, deltaMag))
            {
                path.RemoveAt(0);
                if (path.Count == 0)
                {
                    path = null;
                }
                OnMovedOneStep();
            }
            else
            {
                var move = Mathf.Min(deltaMag, Speed * Time.deltaTime);
                transform.Translate(delta.normalized * move);
                TurnToward(dx, dy);
            }
        }

        FrameUpdate();
    }

    public void TurnToward(Entity other)
    {
        var dx = other.currentX - currentX;
        var dy = other.currentY - currentY;
        TurnToward(dx, dy);
    }

    public void TurnToward(float dx, float dy)
    {
        if (Fixed)
        {
            return;
        }

        var angle = (Direction) (Mathf.RoundToInt(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg / 45) * 45);
        if (Has8Directions)
        {
            direction = angle;
        }
        else
        {
            // Direct angles: force direction
            if (angle == Direction.Right || angle == Direction.Left || angle == Direction.Up || angle == Direction.Down)
            {
                direction = angle;
            }
            else if (angle == Direction.UpRight)
            {
                if (direction == Direction.Left)
                {
                    direction = Direction.Up;
                }
                else if (direction == Direction.Down)
                {
                    direction = Direction.Right;
                }
                else
                {
                    // Do not change
                }
            }
            else if (angle == Direction.UpLeft)
            {
                if (direction == Direction.Down)
                {
                    direction = Direction.Left;
                }
                else if (direction == Direction.Right)
                {
                    direction = Direction.Up;
                }
                else
                {
                    // Do not change
                }
            }
            else if (angle == Direction.DownRight)
            {
                if (direction == Direction.Left)
                {
                    direction = Direction.Down;
                }
                else if (direction == Direction.Up)
                {
                    direction = Direction.Right;
                }
                else
                {
                    // Do not change
                }
            }
            else if (angle == Direction.DownLeft)
            {
                if (direction == Direction.Right)
                {
                    direction = Direction.Down;
                }
                else if (direction == Direction.Up)
                {
                    direction = Direction.Left;
                }
                else
                {
                    // Do not change
                }
            }
        }
    }

    public virtual void FrameUpdate()
    {
    }

    public virtual void OnMovedOneStep()
    {
    }

    public bool MoveTo(int x, int y, bool stopJustBefore)
    {
        if (x >= 1 && x <= GameConstants.Width && y >= 1 && y <= GameConstants.Height)
        {
            path = Field.instance.ComputePath(Position.Make(currentX, currentY), Position.Make(x, y));
            if (hasPath)
            {
                if (stopJustBefore)
                {
                    path.RemoveAt(path.Count - 1);
                }
                return true;
            }
        }
        return false;
    }

    public void MoveTo(Area area)
    {
        var pos = Field.instance.posPerArea[area].RandomElement();
        MoveTo(pos.GetX(), pos.GetY(), false);
    }

    public void Say(float delay, string speech)
    {
        EnqueueSpeech(delay, 3, speech);
    }

    public void SayQuickly(float delay, string speech)
    {
        EnqueueSpeech(delay, 1, speech);
    }

    private void EnqueueSpeech(float delay, float duration, string speech)
    {
        //Debug.LogWarning("SAY " + delay + ": " + speech);
        var at = Time.time + delay;
        if (speechBuffer.Count > 0 && speechBuffer[0].Time >= at)
        {
            //Debug.LogWarning("Empty speech buffer");
            speechBuffer.Clear();
        }
        speechBuffer.Add(new Speech
                         {
                             Content = speech,
                             Duration = duration,
                             Time = at
                         });
    }

    private IEnumerator SpeechBubble()
    {
        SpeechUi.text = "";
        SpeechBackgroundUi.gameObject.SetActive(false);
        SpeechQueueUi.gameObject.SetActive(false);

        while (true)
        {
            while (speechBuffer.Count == 0 || speechBuffer[0].Time > Time.time)
            {
                yield return null;
            }

            var speech = speechBuffer[0];
            var speakingAt = Time.time;
            speechBuffer.RemoveAt(0);

            SpeechUi.horizontalOverflow = HorizontalWrapMode.Overflow;
            SpeechUi.text = speech.Content;
            var bubbleWidth = Mathf.Min(300, SpeechUi.preferredWidth) + 20;
            SpeechUi.horizontalOverflow = HorizontalWrapMode.Wrap;
            SpeechUi.text = speech.Content;
            SpeechBackgroundUi.gameObject.SetActive(true);
            SpeechBackgroundUi.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bubbleWidth);
            SpeechQueueUi.gameObject.SetActive(true);

            while (Time.time - speakingAt < speech.Duration && (speechBuffer.Count == 0 || speechBuffer[0].Time > Time.time))
            {
                yield return null;
            }

            SpeechUi.text = "";
            SpeechBackgroundUi.gameObject.SetActive(false);
            SpeechQueueUi.gameObject.SetActive(false);
        }
    }

}
