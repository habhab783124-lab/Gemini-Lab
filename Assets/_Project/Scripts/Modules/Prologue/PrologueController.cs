using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;



public class PrologueController : MonoBehaviour
{
    [System.Serializable]
    public class PrologueBeat
    {
        public VideoClip video;

        [TextArea(2, 5)]
        public string text;

        public Sprite illustration;

        [Header("Text Layout")]
        //public Vector2 textAnchor = new Vector2(0.5f, 0.2f);
        public Vector2 textOffset = Vector2.zero;
        public Vector2 textBoxSize = new Vector2(900f, 200f);

        public TextAlignmentOptions textAlignment =
            TextAlignmentOptions.Center;

        public Color textColor = Color.black;

    }

    [Header("Scene References")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image illustration;
    [SerializeField] private RectTransform dialoguePanel;
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private CanvasGroup textCanvasGroup;

    [Header("Text Animation")]
    [SerializeField] private float textFadeDuration = 0.4f;


    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.6f;



    [Header("Prologue Beats")]
    [SerializeField] private PrologueBeat[] beats;

    private int currentBeat = 0;
    private bool contentShown = false;
    private bool isTransitioning = false;



    private void Start()
    {
        if (beats == null || beats.Length == 0)
        {
            Debug.LogWarning("No prologue beats configured.");
            return;
        }

        StartCoroutine(StartPrologue());
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            NextBeat();
        }
    }

    private void NextBeat()
    {
        // 淡入淡出过程中禁止再次点击
        if (isTransitioning)
        {
            return;
        }
        // 当前 Beat 的文字还没出现
        if (!contentShown)
        {
            //ShowBeatContent();
            StartCoroutine(ShowBeatContent());
            return;
        }
        // 当前 Beat 的文字已经出现了
        // 下一次点击进入下一个 Beat
        int nextBeat = currentBeat + 1;

        if (nextBeat >= beats.Length)
        {
            Debug.Log("Prologue finished.");
            StartCoroutine(FinishPrologue());
            return;
        }

        StartCoroutine(TransitionToBeat(nextBeat));
    }

    private IEnumerator ShowBeatBackground(int index)
    {
        currentBeat = index;
        contentShown = false;

        PrologueBeat beat = beats[currentBeat];

        // 隐藏文字
        if (dialoguePanel != null)
        {
            dialoguePanel.gameObject.SetActive(false);
        }

        // 隐藏插图
        if (illustration != null)
        {
            illustration.gameObject.SetActive(false);
        }

        // 切换视频
        if (videoPlayer != null && beat.video != null)
        {
            videoPlayer.Stop();

            videoPlayer.clip = beat.video;
            videoPlayer.isLooping = true;

            videoPlayer.Prepare();

            // 等待视频准备完成
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            videoPlayer.Play();
        }
    }


    private IEnumerator ShowBeatContent()
    {
        PrologueBeat beat = beats[currentBeat];

        contentShown = true;
        isTransitioning = true;

        // 设置文字区域
        if (dialoguePanel != null)
        {
            dialoguePanel.gameObject.SetActive(true);

            dialoguePanel.anchorMin = new Vector2(0.5f, 0.5f);
            dialoguePanel.anchorMax = new Vector2(0.5f, 0.5f);
            dialoguePanel.pivot = new Vector2(0.5f, 0.5f);

            dialoguePanel.anchoredPosition = beat.textOffset;
            dialoguePanel.sizeDelta = beat.textBoxSize;
        }

        // 设置文字内容
        if (dialogueText != null)
        {
            dialogueText.text = beat.text;
            dialogueText.alignment = beat.textAlignment;
            dialogueText.color = beat.textColor;
        }

        // 文字从完全透明开始
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0f;

            float elapsed = 0f;

            while (elapsed < textFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsed / textFadeDuration
                );

                textCanvasGroup.alpha = t;

                yield return null;
            }

            textCanvasGroup.alpha = 1f;
        }

        // 插图暂时还是直接出现
        if (illustration != null)
        {
            if (beat.illustration != null)
            {
                illustration.sprite = beat.illustration;
                illustration.gameObject.SetActive(true);
            }
            else
            {
                illustration.gameObject.SetActive(false);
            }
        }

        isTransitioning = false;
    }


    private IEnumerator Fade(float from, float to)
    {
        if (fadeOverlay == null)
            yield break;

        float elapsed = 0f;

        fadeOverlay.alpha = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);

            fadeOverlay.alpha = Mathf.Lerp(from, to, t);

            yield return null;
        }

        fadeOverlay.alpha = to;
    }

    private IEnumerator TransitionToBeat(int index)
    {
        isTransitioning = true;

        // 当前 Beat 淡出到黑
        yield return StartCoroutine(Fade(0f, 1f));

        // 黑屏期间切换下一个 Beat
        yield return StartCoroutine(ShowBeatBackground(index));

        // 新 Beat 从黑屏淡入
        yield return StartCoroutine(Fade(1f, 0f));

        isTransitioning = false;
    }

    private IEnumerator StartPrologue()
    {
        isTransitioning = true;

        // 游戏刚开始时保持黑屏
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
        }

        // 准备第一个 Beat
        yield return StartCoroutine(ShowBeatBackground(0));

        // 第一幕从黑色慢慢出现
        yield return StartCoroutine(Fade(1f, 0f));

        isTransitioning = false;
    }

    private IEnumerator FinishPrologue()
    {
        isTransitioning = true;

        // 最后一幕慢慢淡出到黑
        yield return StartCoroutine(Fade(0f, 1f));

        // 进入室内场景
        SceneManager.LoadScene("Apartment_Main");
    }



}
