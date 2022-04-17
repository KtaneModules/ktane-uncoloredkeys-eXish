using System.Collections;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;

public class UncoloredKeysScript : MonoBehaviour {

    public KMAudio audio;
    public KMSelectable[] buttons;
    public MeshRenderer[] buttonRends;
    public Material[] colorMats;
    public TextMesh displayText;
    public Color[] displayColors;

    private int stage;
    private int displayedWord;
    private List<int> correctButtons = new List<int>();
    private int[] btnColors = new int[4];
    private string[] alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
    private string[] words = { "Gray", "Red", "Yellow", "Blue", "Green", "White" };
    private string[] logPos = { "Top Left", "Top Right", "Bottom Left", "Bottom Right" };
    private bool unpressable = true;
    private bool activated = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += Activate;
    }

    void Start () {
        displayText.text = "";
        GenerateStage();
    }

    void Activate()
    {
        displayText.text = words[displayedWord].ToUpper();
        unpressable = false;
        activated = true;
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && unpressable != true)
        {
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
            int indexPressed = Array.IndexOf(buttons, pressed);
            Debug.LogFormat("[Uncolored Keys #{0}] <Stage {1}> Pressed: {2}", moduleId, stage, logPos[indexPressed]);
            if (correctButtons.Contains(indexPressed))
            {
                buttonRends[indexPressed].material = colorMats[displayedWord];
                btnColors[indexPressed] = displayedWord;
                if (stage == 4)
                {
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                    StartCoroutine(SolveAnim());
                    Debug.LogFormat("[Uncolored Keys #{0}] <Stage {1}> Correct. Module Solved!", moduleId, stage);
                    return;
                }
                Debug.LogFormat("[Uncolored Keys #{0}] <Stage {1}> Correct. Advancing to stage {2}", moduleId, stage, stage + 1);
                GenerateStage();
                StartCoroutine(SuccessAnim());
            }
            else
            {
                StartCoroutine(StrikeAnim());
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Uncolored Keys #{0}] <Stage {1}> Incorrect. Strike!", moduleId, stage);
            }
        }
    }

    void GenerateStage()
    {
        stage++;
        correctButtons.Clear();
        int genButton = UnityEngine.Random.Range(0, 4);
        redo:
        int[] scores = new int[4];
        for (int i = 0; i < 4; i++)
        {
            int letter = UnityEngine.Random.Range(0, alphabet.Length);
            buttons[i].GetComponentInChildren<TextMesh>().text = alphabet[letter];
            scores[i] = letter + 1;
            scores[i] *= btnColors[i] + 1;
        }
        displayedWord = UnityEngine.Random.Range(1, words.Length);
        if (Array.IndexOf(scores, scores.Max()) != genButton)
            goto redo;
        for (int i = 0; i < 4; i++)
        {
            if (scores[i] == scores.Max())
                correctButtons.Add(i);
        }
        Debug.LogFormat("[Uncolored Keys #{0}] <Stage {1}> Display: {2}", moduleId, stage, words[displayedWord]);
        Debug.LogFormat("[Uncolored Keys #{0}] <Stage {1}> Buttons:", moduleId, stage);
        for (int i = 0; i < 4; i++)
            Debug.LogFormat("[Uncolored Keys #{0}] <Stage {1}> {2} -> {3} {4} -> {5}", moduleId, stage, logPos[i], words[btnColors[i]], buttons[i].GetComponentInChildren<TextMesh>().text, scores[i]);
        Debug.LogFormat("[Uncolored Keys #{0}] <Stage {1}> Correct Button{3}: {2}", moduleId, stage, correctButtons.Select(x => logPos[x]).Join(", "), correctButtons.Count > 1 ? "s" : "");
    }

    IEnumerator SolveAnim()
    {
        audio.PlaySoundAtTransform("success", transform);
        int i = 0;
        int ranLet = 0;
        int ranCol = 0;
        displayText.text = "CORRECT";
        displayText.color = displayColors[5];
        yield return new WaitForSeconds(0.5f);
        while (i != 50)
        {
            ranLet = UnityEngine.Random.Range(0, 6);
            ranCol = UnityEngine.Random.Range(0, 6);
            displayText.text = words[ranLet];
            displayText.color = displayColors[ranCol];
            for (int j = 0; j < 4; j++)
                buttons[j].transform.localPosition = buttons[j].transform.localPosition + Vector3.up * -0.0005f;
            i++;
            yield return new WaitForSeconds(0.02f);
        }
        displayText.text = string.Empty;
    }

    IEnumerator SuccessAnim()
    {
        unpressable = true;
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        displayText.text = "CORRECT";
        displayText.color = displayColors[5];
        yield return new WaitForSeconds(1f);
        displayText.text = words[displayedWord].ToUpper();
        displayText.color = displayColors[stage - 1];
        unpressable = false;
    }

    IEnumerator StrikeAnim()
    {
        unpressable = true;
        displayText.text = "INCORRECT";
        displayText.color = displayColors[4];
        yield return new WaitForSeconds(1f);
        displayText.text = words[displayedWord].ToUpper();
        displayText.color = displayColors[stage - 1];
        unpressable = false;
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <TL/TR/BL/BR> [Presses the button in the specified position]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else if (parameters.Length == 2)
            {
                string[] positions = { "TL", "TR", "BL", "BR" };
                if (!positions.Contains(parameters[1].ToUpper()))
                {
                    yield return "sendtochaterror!f The specified position '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                buttons[Array.IndexOf(positions, parameters[1].ToUpper())].OnInteract();
            }
            else if (parameters.Length == 1)
                yield return "sendtochaterror Please specify the position of a button to press!";
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!activated) yield return true;
        int start = stage - 1;
        for (int i = start; i < 4; i++)
        {
            while (unpressable) yield return true;
            buttons[correctButtons.PickRandom()].OnInteract();
        }
    }
}